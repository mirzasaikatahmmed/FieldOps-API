namespace FieldOps.COMMON.Entities;

public class JobPhoto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public string? Caption { get; set; }

    public Job Job { get; set; } = null!;
}
