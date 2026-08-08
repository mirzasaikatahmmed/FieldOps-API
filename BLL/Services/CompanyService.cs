using FieldOps.BLL.DTOs.Companies;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface ICompanyService
{
    Task<Result<PagedResult<CompanyDto>>> GetAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}

public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CompanyService(
        ICompanyRepository companyRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _companyRepository = companyRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<CompanyDto>>> GetAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsSuperAdmin)
            return Result<PagedResult<CompanyDto>>.Forbidden();

        var page = await _companyRepository.GetPagedAsync(pagination, search, cancellationToken);
        return Result<PagedResult<CompanyDto>>.Success(new PagedResult<CompanyDto>
        {
            Items = page.Items.Select(Map).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<CompanyDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsSuperAdmin)
            return Result<CompanyDto>.Forbidden();

        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        return company is null
            ? Result<CompanyDto>.NotFound()
            : Result<CompanyDto>.Success(Map(company));
    }

    public async Task<Result<CompanyDto>> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsSuperAdmin)
            return Result<CompanyDto>.Forbidden();

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _companyRepository.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Success(Map(company), 201);
    }

    public async Task<Result<CompanyDto>> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        if (!_tenantProvider.IsSuperAdmin)
            return Result<CompanyDto>.Forbidden();

        var company = await _companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
            return Result<CompanyDto>.NotFound();

        company.IsActive = isActive;
        _companyRepository.Update(company);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CompanyDto>.Success(Map(company));
    }

    private static CompanyDto Map(Company c) => new(c.Id, c.Name, c.IsActive, c.CreatedAt);
}
