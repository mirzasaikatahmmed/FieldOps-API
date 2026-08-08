namespace FieldOps.COMMON.Entities;

public class JobComment
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Job Job { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}
