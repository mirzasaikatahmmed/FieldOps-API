using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface IJobCommentRepository
{
    Task AddAsync(JobComment comment, CancellationToken cancellationToken = default);
    Task<PagedResult<JobComment>> GetByJobIdAsync(Guid jobId, PaginationQuery pagination, CancellationToken cancellationToken = default);
}

public class JobCommentRepository : IJobCommentRepository
{
    private readonly AppDbContext _db;

    public JobCommentRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(JobComment comment, CancellationToken cancellationToken = default)
        => await _db.JobComments.AddAsync(comment, cancellationToken);

    public async Task<PagedResult<JobComment>> GetByJobIdAsync(Guid jobId, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _db.JobComments
            .AsNoTracking()
            .Include(c => c.Author)
            .Where(c => c.JobId == jobId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobComment>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
