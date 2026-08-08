using FieldOps.BLL.DTOs.Jobs;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface IJobCommentService
{
    Task<Result<PagedResult<JobCommentDto>>> GetAsync(Guid jobId, PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<JobCommentDto>> CreateAsync(Guid jobId, CreateJobCommentRequest request, CancellationToken cancellationToken = default);
}

public class JobCommentService : IJobCommentService
{
    private readonly IJobRepository _jobRepository;
    private readonly IJobCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public JobCommentService(
        IJobRepository jobRepository,
        IJobCommentRepository commentRepository,
        IUserRepository userRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _jobRepository = jobRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<JobCommentDto>>> GetAsync(Guid jobId, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(jobId, cancellationToken);
        if (!access.IsSuccess)
            return Result<PagedResult<JobCommentDto>>.Failure(access.Error!, access.StatusCode);

        var page = await _commentRepository.GetByJobIdAsync(jobId, pagination, cancellationToken);
        return Result<PagedResult<JobCommentDto>>.Success(new PagedResult<JobCommentDto>
        {
            Items = page.Items.Select(Map).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<JobCommentDto>> CreateAsync(Guid jobId, CreateJobCommentRequest request, CancellationToken cancellationToken = default)
    {
        var access = await EnsureJobAccessAsync(jobId, cancellationToken);
        if (!access.IsSuccess)
            return Result<JobCommentDto>.Failure(access.Error!, access.StatusCode);

        if (_tenantProvider.UserId is not Guid userId || _tenantProvider.CompanyId is not Guid companyId)
            return Result<JobCommentDto>.Forbidden();

        var comment = new JobComment
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            CompanyId = companyId,
            AuthorUserId = userId,
            Body = request.Body.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _commentRepository.AddAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var author = await _userRepository.GetByIdAsync(userId, cancellationToken);
        return Result<JobCommentDto>.Success(new JobCommentDto(
            comment.Id,
            comment.JobId,
            comment.AuthorUserId,
            author?.FullName ?? string.Empty,
            comment.Body,
            comment.CreatedAt), 201);
    }

    private async Task<Result<Job>> EnsureJobAccessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await _jobRepository.GetByIdAsync(jobId, includeDetails: false, cancellationToken);
        if (job is null)
            return Result<Job>.NotFound();

        if (IsTechnician() && job.AssignedTechnicianId != _tenantProvider.UserId)
            return Result<Job>.NotFound();

        return Result<Job>.Success(job);
    }

    private bool IsTechnician() =>
        string.Equals(_tenantProvider.Role, Roles.Technician, StringComparison.OrdinalIgnoreCase);

    private static JobCommentDto Map(JobComment c) => new(
        c.Id,
        c.JobId,
        c.AuthorUserId,
        c.Author?.FullName ?? string.Empty,
        c.Body,
        c.CreatedAt);
}
