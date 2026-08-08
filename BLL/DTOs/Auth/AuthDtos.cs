namespace FieldOps.BLL.DTOs.Auth;

public record RegisterCompanyRequest(
    string CompanyName,
    string AdminFullName,
    string AdminEmail,
    string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId);
