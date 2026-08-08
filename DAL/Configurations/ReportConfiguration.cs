using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(1000);
        builder.HasOne(x => x.Job)
            .WithOne(x => x.Report)
            .HasForeignKey<Report>(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.JobId).IsUnique();
    }
}
