using FieldOps.BLL.DTOs.JobTemplates;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface IJobTemplateService
{
    Task<Result<PagedResult<JobTemplateDto>>> GetAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<JobTemplateDto>> CreateAsync(CreateJobTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result<JobTemplateDto>> UpdateAsync(Guid id, UpdateJobTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public class JobTemplateService : IJobTemplateService
{
    private readonly IJobTemplateRepository _repository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public JobTemplateService(
        IJobTemplateRepository repository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<JobTemplateDto>>> GetAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await _repository.GetPagedAsync(pagination, cancellationToken);
        return Result<PagedResult<JobTemplateDto>>.Success(new PagedResult<JobTemplateDto>
        {
            Items = page.Items.Select(Map).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<JobTemplateDto>> CreateAsync(CreateJobTemplateRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result<JobTemplateDto>.Forbidden("Company context required.");

        var template = new JobTemplate
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            TemplateFields = request.Fields.Select(f => new TemplateField
            {
                Id = Guid.NewGuid(),
                Label = f.Label.Trim(),
                FieldType = f.FieldType,
                Options = f.Options,
                SortOrder = f.SortOrder,
                IsRequired = f.IsRequired
            }).ToList()
        };

        await _repository.AddAsync(template, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<JobTemplateDto>.Success(Map(template), 201);
    }

    public async Task<Result<JobTemplateDto>> UpdateAsync(Guid id, UpdateJobTemplateRequest request, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(id, includeFields: true, cancellationToken);
        if (template is null)
            return Result<JobTemplateDto>.NotFound();

        template.Name = request.Name.Trim();
        template.IsActive = request.IsActive;
        template.TemplateFields.Clear();
        foreach (var f in request.Fields)
        {
            template.TemplateFields.Add(new TemplateField
            {
                Id = Guid.NewGuid(),
                JobTemplateId = template.Id,
                Label = f.Label.Trim(),
                FieldType = f.FieldType,
                Options = f.Options,
                SortOrder = f.SortOrder,
                IsRequired = f.IsRequired
            });
        }

        _repository.Update(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<JobTemplateDto>.Success(Map(template));
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await _repository.GetByIdAsync(id, includeFields: false, cancellationToken);
        if (template is null)
            return Result.NotFound();

        _repository.Remove(template);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static JobTemplateDto Map(JobTemplate t) => new(
        t.Id,
        t.Name,
        t.IsActive,
        t.CreatedAt,
        t.TemplateFields
            .OrderBy(f => f.SortOrder)
            .Select(f => new TemplateFieldDto(f.Id, f.Label, f.FieldType, f.Options, f.SortOrder, f.IsRequired))
            .ToList());
}
