using FieldOps.COMMON.Enums;

namespace FieldOps.BLL.DTOs.JobTemplates;

public record TemplateFieldRequest(
    string Label,
    FieldType FieldType,
    string? Options,
    int SortOrder,
    bool IsRequired);

public record CreateJobTemplateRequest(
    string Name,
    IReadOnlyList<TemplateFieldRequest> Fields);

public record UpdateJobTemplateRequest(
    string Name,
    bool IsActive,
    IReadOnlyList<TemplateFieldRequest> Fields);

public record TemplateFieldDto(
    Guid Id,
    string Label,
    FieldType FieldType,
    string? Options,
    int SortOrder,
    bool IsRequired);

public record JobTemplateDto(
    Guid Id,
    string Name,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyList<TemplateFieldDto> Fields);
