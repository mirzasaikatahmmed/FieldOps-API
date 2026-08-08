namespace FieldOps.BLL.DTOs.Jobs;

public record CreateJobCommentRequest(string Body);

public record JobCommentDto(
    Guid Id,
    Guid JobId,
    Guid AuthorUserId,
    string AuthorName,
    string Body,
    DateTime CreatedAt);
