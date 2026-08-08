namespace FieldOps.BLL.DTOs.Users;

public record CreateUserRequest(
    string FullName,
    string Email,
    string Password,
    string Role);

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid? CompanyId,
    DateTime CreatedAt);
