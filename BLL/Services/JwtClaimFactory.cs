using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;

namespace FieldOps.BLL.Services;

public static class JwtClaimFactory
{
    public static IEnumerable<Claim> BuildClaims(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role)
        };

        if (user.CompanyId.HasValue)
            claims.Add(new Claim(AppClaimTypes.CompanyId, user.CompanyId.Value.ToString()));

        return claims;
    }
}
