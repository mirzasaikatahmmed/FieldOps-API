using FieldOps.BLL.DTOs.Customers;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface ICustomerService
{
    Task<Result<PagedResult<CustomerDto>>> GetAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);
    Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default);
}

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        ICustomerRepository customerRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<CustomerDto>>> GetAsync(PaginationQuery pagination, string? search = null, CancellationToken cancellationToken = default)
    {
        var page = await _customerRepository.GetPagedAsync(pagination, search, cancellationToken);
        return Result<PagedResult<CustomerDto>>.Success(new PagedResult<CustomerDto>
        {
            Items = page.Items.Select(Map).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<CustomerDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        return customer is null
            ? Result<CustomerDto>.NotFound()
            : Result<CustomerDto>.Success(Map(customer));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result<CustomerDto>.Forbidden("Company context required.");

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = request.Name.Trim(),
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address
        };

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CustomerDto>.Success(Map(customer), 201);
    }

    public async Task<Result<CustomerDto>> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
        if (customer is null)
            return Result<CustomerDto>.NotFound();

        customer.Name = request.Name.Trim();
        customer.Phone = request.Phone;
        customer.Email = request.Email;
        customer.Address = request.Address;

        _customerRepository.Update(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<CustomerDto>.Success(Map(customer));
    }

    private static CustomerDto Map(Customer c) => new(c.Id, c.Name, c.Phone, c.Email, c.Address);
}
