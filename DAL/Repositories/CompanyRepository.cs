using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    void Update(Company company);
    Task<PagedResult<Company>> GetPagedAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default);
}

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
        => await _db.Companies.AddAsync(company, cancellationToken);

    public void Update(Company company) => _db.Companies.Update(company);

    public async Task<PagedResult<Company>> GetPagedAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Companies.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c => EF.Functions.ILike(c.Name, term));
        }

        query = query.OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Company>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }
}
