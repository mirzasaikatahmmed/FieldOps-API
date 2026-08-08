namespace FieldOps.COMMON.Entities;

public class JobTemplate
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public ICollection<TemplateField> TemplateFields { get; set; } = new List<TemplateField>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
