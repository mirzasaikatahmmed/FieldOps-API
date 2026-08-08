using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public class JobFilter
{
    public JobStatus? Status { get; set; }
    public Guid? TechnicianId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id, bool includeDetails = false, CancellationToken cancellationToken = default);
    Task<Job?> GetByIdForReportAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Job>> GetPagedAsync(JobFilter filter, PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task AddAsync(Job job, CancellationToken cancellationToken = default);
    void Update(Job job);
    Task AddResponsesAsync(IEnumerable<JobResponse> responses, CancellationToken cancellationToken = default);
    Task<List<JobResponse>> GetResponsesAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task AddPhotoAsync(JobPhoto photo, CancellationToken cancellationToken = default);
    Task UpsertSignatureAsync(Signature signature, CancellationToken cancellationToken = default);
    Task AddReportAsync(Report report, CancellationToken cancellationToken = default);
    Task<Report?> GetReportByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<List<Job>> GetSlaBreachedScheduledJobsAsync(DateTime threshold, CancellationToken cancellationToken = default);
}

public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db) => _db = db;

    public async Task<Job?> GetByIdAsync(Guid id, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Job> query = _db.Jobs;
        if (includeDetails)
        {
            query = query
                .Include(j => j.Customer)
                .Include(j => j.AssignedTechnician)
                .Include(j => j.JobTemplate).ThenInclude(t => t.TemplateFields)
                .Include(j => j.Responses)
                .Include(j => j.Photos)
                .Include(j => j.Signature)
                .Include(j => j.Report);
        }

        return await query.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<Job?> GetByIdForReportAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Jobs
            .Include(j => j.Company)
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .Include(j => j.JobTemplate).ThenInclude(t => t.TemplateFields.OrderBy(f => f.SortOrder))
            .Include(j => j.Responses).ThenInclude(r => r.TemplateField)
            .Include(j => j.Photos)
            .Include(j => j.Signature)
            .Include(j => j.Report)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<PagedResult<Job>> GetPagedAsync(JobFilter filter, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _db.Jobs
            .AsNoTracking()
            .Include(j => j.Customer)
            .Include(j => j.AssignedTechnician)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(j => j.Status == filter.Status);
        if (filter.TechnicianId.HasValue)
            query = query.Where(j => j.AssignedTechnicianId == filter.TechnicianId);
        if (filter.From.HasValue)
            query = query.Where(j => j.ScheduledAt >= filter.From);
        if (filter.To.HasValue)
            query = query.Where(j => j.ScheduledAt <= filter.To);

        query = query.OrderByDescending(j => j.ScheduledAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Job>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task AddAsync(Job job, CancellationToken cancellationToken = default)
        => await _db.Jobs.AddAsync(job, cancellationToken);

    public void Update(Job job) => _db.Jobs.Update(job);

    public async Task AddResponsesAsync(IEnumerable<JobResponse> responses, CancellationToken cancellationToken = default)
        => await _db.JobResponses.AddRangeAsync(responses, cancellationToken);

    public async Task<List<JobResponse>> GetResponsesAsync(Guid jobId, CancellationToken cancellationToken = default)
        => await _db.JobResponses.Where(r => r.JobId == jobId).ToListAsync(cancellationToken);

    public async Task AddPhotoAsync(JobPhoto photo, CancellationToken cancellationToken = default)
        => await _db.JobPhotos.AddAsync(photo, cancellationToken);

    public async Task UpsertSignatureAsync(Signature signature, CancellationToken cancellationToken = default)
    {
        var existing = await _db.Signatures.FirstOrDefaultAsync(s => s.JobId == signature.JobId, cancellationToken);
        if (existing is null)
        {
            await _db.Signatures.AddAsync(signature, cancellationToken);
            return;
        }

        existing.StorageKey = signature.StorageKey;
        existing.Url = signature.Url;
        existing.SignedByName = signature.SignedByName;
        existing.SignedAt = signature.SignedAt;
    }

    public async Task AddReportAsync(Report report, CancellationToken cancellationToken = default)
        => await _db.Reports.AddAsync(report, cancellationToken);

    public async Task<Report?> GetReportByJobIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        => await _db.Reports.FirstOrDefaultAsync(r => r.JobId == jobId, cancellationToken);

    public async Task<List<Job>> GetSlaBreachedScheduledJobsAsync(DateTime threshold, CancellationToken cancellationToken = default)
    {
        return await _db.Jobs
            .IgnoreQueryFilters()
            .Include(j => j.AssignedTechnician)
            .Where(j => j.Status == JobStatus.Scheduled && j.ScheduledAt < threshold)
            .ToListAsync(cancellationToken);
    }
}
