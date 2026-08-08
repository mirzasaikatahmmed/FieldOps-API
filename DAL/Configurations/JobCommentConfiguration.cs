using FieldOps.COMMON.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldOps.DAL.Configurations;

public class JobCommentConfiguration : IEntityTypeConfiguration<JobComment>
{
    public void Configure(EntityTypeBuilder<JobComment> builder)
    {
        builder.ToTable("JobComments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Body).IsRequired().HasMaxLength(4000);
        builder.HasOne(x => x.Job)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.JobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Company)
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.CompanyId);
    }
}
