namespace FieldOps.COMMON.Interfaces;

public interface ITenantProvider
{
    Guid? CompanyId { get; }
    Guid? UserId { get; }
    string? Role { get; }
    bool IsSuperAdmin { get; }
}
