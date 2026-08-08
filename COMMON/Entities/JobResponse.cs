namespace FieldOps.COMMON.Entities;

public class JobResponse
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid TemplateFieldId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public bool? ValueBool { get; set; }
    public string? PhotoUrl { get; set; }

    public Job Job { get; set; } = null!;
    public TemplateField TemplateField { get; set; } = null!;
}
