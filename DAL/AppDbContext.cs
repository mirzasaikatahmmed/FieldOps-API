using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.DAL;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    /// <summary>
    /// Evaluated per query by EF Core global filters. Null means no tenant restriction (e.g. SuperAdmin / seeding).
    /// </summary>
    public Guid? CurrentCompanyId => _tenantProvider.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<JobTemplate> JobTemplates => Set<JobTemplate>();
    public DbSet<TemplateField> TemplateFields => Set<TemplateField>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobResponse> JobResponses => Set<JobResponse>();
    public DbSet<JobPhoto> JobPhotos => Set<JobPhoto>();
    public DbSet<JobComment> JobComments => Set<JobComment>();
    public DbSet<Signature> Signatures => Set<Signature>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        builder.Entity<Customer>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.CompanyId == CurrentCompanyId);
        builder.Entity<JobTemplate>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.CompanyId == CurrentCompanyId);
        builder.Entity<Job>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.CompanyId == CurrentCompanyId);
        builder.Entity<ApplicationUser>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.CompanyId == null || e.CompanyId == CurrentCompanyId);
        builder.Entity<JobResponse>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.Job.CompanyId == CurrentCompanyId);
        builder.Entity<JobPhoto>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.Job.CompanyId == CurrentCompanyId);
        builder.Entity<JobComment>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.CompanyId == CurrentCompanyId);
        builder.Entity<Signature>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.Job.CompanyId == CurrentCompanyId);
        builder.Entity<Report>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.Job.CompanyId == CurrentCompanyId);
        builder.Entity<TemplateField>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.JobTemplate.CompanyId == CurrentCompanyId);
        builder.Entity<RefreshToken>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.User.CompanyId == null || e.User.CompanyId == CurrentCompanyId);
        builder.Entity<PasswordResetToken>().HasQueryFilter(e =>
            CurrentCompanyId == null || e.User.CompanyId == null || e.User.CompanyId == CurrentCompanyId);
    }
}
