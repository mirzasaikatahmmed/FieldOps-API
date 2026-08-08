namespace FieldOps.BLL.DTOs.Companies;

public record CreateCompanyRequest(string Name);

public record CompanyDto(Guid Id, string Name, bool IsActive, DateTime CreatedAt);
