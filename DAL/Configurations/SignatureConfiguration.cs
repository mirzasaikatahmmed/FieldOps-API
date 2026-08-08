using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class SignatureConfiguration : IEntityTypeConfiguration<Signature>
{
    public void Configure(EntityTypeBuilder<Signature> builder)
    {
        builder.ToTable("Signatures");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StorageKey).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.SignedByName).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.Job)
            .WithOne(x => x.Signature)
            .HasForeignKey<Signature>(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.JobId).IsUnique();
    }
}
