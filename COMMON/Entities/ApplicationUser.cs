using Microsoft.AspNetCore.Identity;

namespace FieldOps.COMMON.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? CompanyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Company? Company { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
