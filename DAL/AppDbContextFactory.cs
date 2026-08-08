using FieldOps.COMMON.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FieldOps.DAL;

internal sealed class DesignTimeTenantProvider : ITenantProvider
{
    public Guid? CompanyId => null;
    public Guid? UserId => null;
    public string? Role => null;
    public bool IsSuperAdmin => true;
}

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=fieldops;Username=fieldops;Password=fieldops";

        optionsBuilder.UseNpgsql(connectionString);
        return new AppDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }
}
