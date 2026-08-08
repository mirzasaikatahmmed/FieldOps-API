using FieldOps.COMMON.Enums;

namespace FieldOps.BLL.DTOs.Jobs;

public record CreateJobRequest(
    Guid CustomerId,
    Guid JobTemplateId,
    Guid? AssignedTechnicianId,
    string Title,
    DateTime ScheduledAt,
    string? Notes);

public record AssignJobRequest(Guid TechnicianId);

public record UpdateJobStatusRequest(JobStatus Status);

public record JobResponseItemRequest(
    Guid TemplateFieldId,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBool,
    string? PhotoUrl);

public record SubmitJobResponsesRequest(IReadOnlyList<JobResponseItemRequest> Responses);

public record PresignUploadRequest(string FileName, string ContentType);

public record PresignUploadResponse(string UploadUrl, string StorageKey, string PublicUrl);

public record ConfirmPhotoRequest(string StorageKey, string? Caption);

public record ConfirmSignatureRequest(string StorageKey, string SignedByName);

public record JobDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedTechnicianId,
    string? TechnicianName,
    Guid JobTemplateId,
    string Title,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    JobStatus Status,
    string? Notes,
    DateTime CreatedAt);

public record JobDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedTechnicianId,
    string? TechnicianName,
    Guid JobTemplateId,
    string TemplateName,
    string Title,
    DateTime ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    JobStatus Status,
    string? Notes,
    DateTime CreatedAt,
    IReadOnlyList<JobResponseDto> Responses,
    IReadOnlyList<JobPhotoDto> Photos,
    SignatureDto? Signature,
    ReportDto? Report);

public record JobResponseDto(
    Guid Id,
    Guid TemplateFieldId,
    string FieldLabel,
    string? ValueText,
    decimal? ValueNumber,
    bool? ValueBool,
    string? PhotoUrl);

public record JobPhotoDto(Guid Id, string Url, string? Caption, DateTime UploadedAt);

public record SignatureDto(Guid Id, string Url, string SignedByName, DateTime SignedAt);

public record ReportDto(Guid Id, string Url, DateTime GeneratedAt);
