using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class JobTemplateConfiguration : IEntityTypeConfiguration<JobTemplate>
{
    public void Configure(EntityTypeBuilder<JobTemplate> builder)
    {
        builder.ToTable("JobTemplates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.Company)
            .WithMany(x => x.JobTemplates)
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.CompanyId);
    }
}
