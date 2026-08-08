using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Models;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<Customer>> GetPagedAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    void Update(Customer customer);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;

    public CustomerRepository(AppDbContext db) => _db = db;

    public async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _db.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<PagedResult<Customer>> GetPagedAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Customers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, term) ||
                (c.Email != null && EF.Functions.ILike(c.Email, term)) ||
                (c.Phone != null && EF.Functions.ILike(c.Phone, term)) ||
                (c.Address != null && EF.Functions.ILike(c.Address, term)));
        }

        query = query.OrderBy(c => c.Name);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pagination.Page - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Customer>
        {
            Items = items,
            TotalCount = total,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        => await _db.Customers.AddAsync(customer, cancellationToken);

    public void Update(Customer customer) => _db.Customers.Update(customer);
}
