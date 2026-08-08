using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<ApplicationUser>> GetCompanyUsersAsync(Guid companyId, PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<bool> ExistsInCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db) => _db = db;

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task<PagedResult<ApplicationUser>> GetCompanyUsersAsync(Guid companyId, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var query = _db.Users
            .AsNoTracking()
            .Where(u => u.CompanyId == companyId)
            .OrderBy(u => u.FullName);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ApplicationUser>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task<bool> ExistsInCompanyAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default)
        => await _db.Users.AnyAsync(u => u.Id == userId && u.CompanyId == companyId, cancellationToken);
}
