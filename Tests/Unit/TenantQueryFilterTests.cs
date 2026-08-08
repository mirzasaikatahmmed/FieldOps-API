using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using FieldOps.DAL;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FieldOps.Tests.Unit;

public class TenantQueryFilterTests
{
    private sealed class FakeTenantProvider : ITenantProvider
    {
        public Guid? CompanyId { get; set; }
        public Guid? UserId { get; set; }
        public string? Role { get; set; }
        public bool IsSuperAdmin => Role == "SuperAdmin";
    }

    [Fact]
    public async Task QueryFilter_RestrictsCustomers_ToCurrentTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var provider = new FakeTenantProvider { CompanyId = tenantA };

        await using var db = CreateDb(provider);
        db.Companies.AddRange(
            new Company { Id = tenantA, Name = "A" },
            new Company { Id = tenantB, Name = "B" });
        db.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), CompanyId = tenantA, Name = "Cust A" },
            new Customer { Id = Guid.NewGuid(), CompanyId = tenantB, Name = "Cust B" });
        await db.SaveChangesAsync();

        var visible = await db.Customers.AsNoTracking().ToListAsync();
        visible.Should().HaveCount(1);
        visible[0].Name.Should().Be("Cust A");
    }

    [Fact]
    public async Task QueryFilter_Bypassed_WhenCompanyIdIsNull()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var provider = new FakeTenantProvider { CompanyId = null, Role = "SuperAdmin" };

        await using var db = CreateDb(provider);
        db.Companies.AddRange(
            new Company { Id = tenantA, Name = "A" },
            new Company { Id = tenantB, Name = "B" });
        db.Customers.AddRange(
            new Customer { Id = Guid.NewGuid(), CompanyId = tenantA, Name = "Cust A" },
            new Customer { Id = Guid.NewGuid(), CompanyId = tenantB, Name = "Cust B" });
        await db.SaveChangesAsync();

        var visible = await db.Customers.AsNoTracking().ToListAsync();
        visible.Should().HaveCount(2);
    }

    private static AppDbContext CreateDb(ITenantProvider tenantProvider)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options, tenantProvider);
    }
}
