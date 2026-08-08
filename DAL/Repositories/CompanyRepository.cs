using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    Task<PagedResult<Company>> GetPagedAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
}

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;

    public CompanyRepository(AppDbContext db) => _db = db;

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Companies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
        => await _db.Companies.AddAsync(company, cancellationToken);

    public async Task<PagedResult<Company>> GetPagedAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _db.Companies.AsNoTracking().OrderBy(c => c.Name);
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
