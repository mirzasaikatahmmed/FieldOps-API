using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FieldOps.BLL.Services;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FluentAssertions;
using Xunit;

namespace FieldOps.Tests.Unit;

public class AuthClaimsTests
{
    [Fact]
    public void BuildClaims_IncludesExpectedClaims_ForCompanyUser()
    {
        var companyId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin@acme.test",
            Role = Roles.CompanyAdmin,
            CompanyId = companyId,
            FullName = "Admin"
        };

        var claims = JwtClaimFactory.BuildClaims(user).ToList();

        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == Roles.CompanyAdmin);
        claims.Should().Contain(c => c.Type == AppClaimTypes.CompanyId && c.Value == companyId.ToString());
        claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == user.Email);
    }

    [Fact]
    public void BuildClaims_OmitsCompanyId_ForSuperAdmin()
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "super@fieldops.local",
            Role = Roles.SuperAdmin,
            CompanyId = null,
            FullName = "Super"
        };

        var claims = JwtClaimFactory.BuildClaims(user).ToList();
        claims.Should().NotContain(c => c.Type == AppClaimTypes.CompanyId);
    }
}
