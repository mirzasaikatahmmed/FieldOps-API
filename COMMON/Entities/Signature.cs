namespace FieldOps.COMMON.Entities;

public class Signature
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string SignedByName { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;

    public Job Job { get; set; } = null!;
}
