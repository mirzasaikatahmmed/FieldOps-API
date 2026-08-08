namespace FieldOps.COMMON.Entities;

public class Customer
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
