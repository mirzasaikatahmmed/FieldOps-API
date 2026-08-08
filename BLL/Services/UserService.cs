using FieldOps.BLL.DTOs.Users;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;
using Microsoft.AspNetCore.Identity;

namespace FieldOps.BLL.Services;

public interface IUserService
{
    Task<Result<PagedResult<UserDto>>> GetUsersAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        UserManager<ApplicationUser> userManager,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userManager = userManager;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result<PagedResult<UserDto>>.Forbidden();

        var page = await _userRepository.GetCompanyUsersAsync(companyId, pagination, cancellationToken);
        return Result<PagedResult<UserDto>>.Success(new PagedResult<UserDto>
        {
            Items = page.Items.Select(Map).ToList(),
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        });
    }

    public async Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result<UserDto>.Forbidden();

        if (await _userManager.FindByEmailAsync(request.Email) is not null)
            return Result<UserDto>.Failure("Email is already registered.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true,
            FullName = request.FullName.Trim(),
            Role = request.Role,
            CompanyId = companyId,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result<UserDto>.Failure(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, request.Role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<UserDto>.Success(Map(user), 201);
    }

    public async Task<Result> DeleteUserAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is not Guid companyId)
            return Result.Forbidden();

        var user = await _userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null || user.CompanyId != companyId)
            return Result.NotFound();

        if (user.Id == _tenantProvider.UserId)
            return Result.Failure("Cannot delete your own account.");

        if (user.Role == Roles.SuperAdmin)
            return Result.Forbidden();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        return Result.Success();
    }

    private static UserDto Map(ApplicationUser user) => new(
        user.Id,
        user.FullName,
        user.Email ?? string.Empty,
        user.Role,
        user.CompanyId,
        user.CreatedAt);
}
