namespace FieldOps.BLL.DTOs.Customers;

public record CreateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address);

public record UpdateCustomerRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address);

public record CustomerDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address);
