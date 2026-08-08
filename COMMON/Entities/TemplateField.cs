using FieldOps.COMMON.Enums;

namespace FieldOps.COMMON.Entities;

public class TemplateField
{
    public Guid Id { get; set; }
    public Guid JobTemplateId { get; set; }
    public string Label { get; set; } = string.Empty;
    public FieldType FieldType { get; set; }
    public string? Options { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }

    public JobTemplate JobTemplate { get; set; } = null!;
    public ICollection<JobResponse> JobResponses { get; set; } = new List<JobResponse>();
}
