namespace FieldOps.BLL.DTOs.Auth;

public record RegisterCompanyRequest(
    string CompanyName,
    string AdminFullName,
    string AdminEmail,
    string Password);

public record LoginRequest(string Email, string Password);

public record RefreshRequest(string RefreshToken);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    Guid? CompanyId);
