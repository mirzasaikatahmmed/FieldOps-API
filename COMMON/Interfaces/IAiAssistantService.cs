using FieldOps.COMMON.Models;

namespace FieldOps.COMMON.Interfaces;

public interface IAiAssistantService
{
    Task<Result<JobAiSummaryDto>> GenerateJobSummaryAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<Result<AiAskResponseDto>> AskAsync(string question, CancellationToken cancellationToken = default);
    Task<Result<AiRiskHintsResponseDto>> GetRiskHintsAsync(int limit = 20, CancellationToken cancellationToken = default);
}

public record JobAiSummaryDto(
    Guid JobId,
    string Summary,
    DateTime GeneratedAt,
    bool UsedStub,
    string Model);

public record AiAskResponseDto(
    string Answer,
    bool UsedStub,
    string Model);

public record AiRiskHintDto(
    Guid JobId,
    string Title,
    int Score,
    string Level,
    string Reason,
    string Recommendation);

public record AiRiskHintsResponseDto(
    IReadOnlyList<AiRiskHintDto> Items,
    bool UsedStub,
    string Model);
