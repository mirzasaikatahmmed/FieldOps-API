using FieldOps.COMMON.Enums;

namespace FieldOps.COMMON.Entities;

public class Job
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? AssignedTechnicianId { get; set; }
    public Guid JobTemplateId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Scheduled;
    public string? Notes { get; set; }
    public string? AiSummary { get; set; }
    public DateTime? AiSummaryGeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company Company { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public ApplicationUser? AssignedTechnician { get; set; }
    public JobTemplate JobTemplate { get; set; } = null!;
    public ICollection<JobResponse> Responses { get; set; } = new List<JobResponse>();
    public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();
    public ICollection<JobComment> Comments { get; set; } = new List<JobComment>();
    public Signature? Signature { get; set; }
    public Report? Report { get; set; }
}
