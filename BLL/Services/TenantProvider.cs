using System.Security.Claims;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FieldOps.BLL.Services;

public class TenantProvider : ITenantProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public Guid? CompanyId
    {
        get
        {
            var value = User?.FindFirstValue(AppClaimTypes.CompanyId);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? User?.FindFirstValue("sub");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Role =>
        User?.FindFirstValue(ClaimTypes.Role)
        ?? User?.FindFirstValue("role");

    public bool IsSuperAdmin =>
        string.Equals(Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase);
}
