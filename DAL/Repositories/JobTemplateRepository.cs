using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface IJobTemplateRepository
{
    Task<JobTemplate?> GetByIdAsync(Guid id, bool includeFields = true, CancellationToken cancellationToken = default);
    Task<PagedResult<JobTemplate>> GetPagedAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task AddAsync(JobTemplate template, CancellationToken cancellationToken = default);
    void Update(JobTemplate template);
    void Remove(JobTemplate template);
}

public class JobTemplateRepository : IJobTemplateRepository
{
    private readonly AppDbContext _db;

    public JobTemplateRepository(AppDbContext db) => _db = db;

    public async Task<JobTemplate?> GetByIdAsync(Guid id, bool includeFields = true, CancellationToken cancellationToken = default)
    {
        IQueryable<JobTemplate> query = _db.JobTemplates;
        if (includeFields)
            query = query.Include(t => t.TemplateFields.OrderBy(f => f.SortOrder));

        return await query.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PagedResult<JobTemplate>> GetPagedAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _db.JobTemplates
            .AsNoTracking()
            .Include(t => t.TemplateFields.OrderBy(f => f.SortOrder))
            .OrderBy(t => t.Name);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobTemplate>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task AddAsync(JobTemplate template, CancellationToken cancellationToken = default)
        => await _db.JobTemplates.AddAsync(template, cancellationToken);

    public void Update(JobTemplate template) => _db.JobTemplates.Update(template);

    public void Remove(JobTemplate template) => _db.JobTemplates.Remove(template);
}
