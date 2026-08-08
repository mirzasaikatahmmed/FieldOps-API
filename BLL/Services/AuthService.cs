using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FieldOps.BLL.DTOs.Auth;
using FieldOps.BLL.Options;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FieldOps.BLL.Services;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterCompanyAsync(RegisterCompanyRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    IEnumerable<Claim> BuildClaims(ApplicationUser user);
}

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyRepository _companyRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly INotificationService _notificationService;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companyRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        INotificationService notificationService,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _companyRepository = companyRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _notificationService = notificationService;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<Result<AuthResponse>> RegisterCompanyAsync(RegisterCompanyRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(request.AdminEmail);
        if (existing is not null)
            return Result<AuthResponse>.Failure("Email is already registered.");

        var company = new Company
        {
            Id = Guid.NewGuid(),
            Name = request.CompanyName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _companyRepository.AddAsync(company, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.AdminEmail,
            Email = request.AdminEmail,
            EmailConfirmed = true,
            FullName = request.AdminFullName.Trim(),
            Role = Roles.CompanyAdmin,
            CompanyId = company.Id,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result<AuthResponse>.Failure(string.Join("; ", createResult.Errors.Select(e => e.Description)));

        await _userManager.AddToRoleAsync(user, Roles.CompanyAdmin);

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, cancellationToken), 201);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Result<AuthResponse>.Unauthorized("Invalid email or password.");

        if (user.CompanyId.HasValue)
        {
            var company = await _companyRepository.GetByIdAsync(user.CompanyId.Value, cancellationToken);
            if (company is null || !company.IsActive)
                return Result<AuthResponse>.Forbidden("Company account is inactive.");
        }

        return Result<AuthResponse>.Success(await IssueTokensAsync(user, cancellationToken));
    }

    public async Task<Result<AuthResponse>> RefreshAsync(RefreshRequest request, CancellationToken cancellationToken = default)
    {
        var hash = HashToken(request.RefreshToken);
        var stored = await _refreshTokenRepository.GetByHashAsync(hash, cancellationToken);
        if (stored is null || !stored.IsActive)
            return Result<AuthResponse>.Unauthorized("Invalid refresh token.");

        if (stored.User.CompanyId.HasValue)
        {
            var company = await _companyRepository.GetByIdAsync(stored.User.CompanyId.Value, cancellationToken);
            if (company is null || !company.IsActive)
                return Result<AuthResponse>.Forbidden("Company account is inactive.");
        }

        stored.RevokedAt = DateTime.UtcNow;
        var response = await IssueTokensAsync(stored.User, cancellationToken);
        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.UserId is not Guid userId)
            return Result.Unauthorized();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is not null)
        {
            await _passwordResetTokenRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            await _passwordResetTokenRepository.AddAsync(new PasswordResetToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = HashToken(rawToken),
                ExpiresAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _notificationService.NotifyAdminAsync(
                $"Password reset token for {user.Email}: {rawToken}",
                cancellationToken);
        }

        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
            return Result.Failure("Invalid reset token.");

        var stored = await _passwordResetTokenRepository.GetActiveByHashAsync(HashToken(request.Token), cancellationToken);
        if (stored is null || stored.UserId != user.Id)
            return Result.Failure("Invalid or expired reset token.");

        var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!result.Succeeded)
            return Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));

        stored.UsedAt = DateTime.UtcNow;
        await _passwordResetTokenRepository.InvalidateUserTokensAsync(user.Id, cancellationToken);
        await _refreshTokenRepository.RevokeUserTokensAsync(user.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public IEnumerable<Claim> BuildClaims(ApplicationUser user) => JwtClaimFactory.BuildClaims(user);

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var expires = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes);
        var accessToken = CreateAccessToken(user, expires);
        var refreshToken = GenerateRefreshToken();

        await _refreshTokenRepository.AddAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = HashToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AuthResponse(
            accessToken,
            refreshToken,
            expires,
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            user.Role,
            user.CompanyId);
    }

    private string CreateAccessToken(ApplicationUser user, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: JwtClaimFactory.BuildClaims(user),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public static string HashToken(string token)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
