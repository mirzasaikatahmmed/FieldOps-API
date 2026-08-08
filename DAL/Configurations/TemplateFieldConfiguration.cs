using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class TemplateFieldConfiguration : IEntityTypeConfiguration<TemplateField>
{
    public void Configure(EntityTypeBuilder<TemplateField> builder)
    {
        builder.ToTable("TemplateFields");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Label).IsRequired().HasMaxLength(300);
        builder.Property(x => x.FieldType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Options).HasMaxLength(2000);
        builder.HasOne(x => x.JobTemplate)
            .WithMany(x => x.TemplateFields)
            .HasForeignKey(x => x.JobTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
