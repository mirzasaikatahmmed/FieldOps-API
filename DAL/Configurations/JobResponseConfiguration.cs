using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class JobResponseConfiguration : IEntityTypeConfiguration<JobResponse>
{
    public void Configure(EntityTypeBuilder<JobResponse> builder)
    {
        builder.ToTable("JobResponses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValueText).HasMaxLength(4000);
        builder.Property(x => x.ValueNumber).HasPrecision(18, 4);
        builder.Property(x => x.PhotoUrl).HasMaxLength(1000);

        builder.HasOne(x => x.Job)
            .WithMany(x => x.Responses)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.TemplateField)
            .WithMany(x => x.JobResponses)
            .HasForeignKey(x => x.TemplateFieldId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.JobId, x.TemplateFieldId }).IsUnique();
    }
}
