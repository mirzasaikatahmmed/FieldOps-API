namespace FieldOps.COMMON.Entities;

public class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
    public ICollection<JobTemplate> JobTemplates { get; set; } = new List<JobTemplate>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
