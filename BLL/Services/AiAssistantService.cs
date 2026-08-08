using System.Text.Json;
using FieldOps.COMMON.Constants;
using FieldOps.COMMON.Entities;
using FieldOps.COMMON.Enums;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace FieldOps.BLL.Services;

public class AiAssistantService : IAiAssistantService
{
    private readonly IJobRepository _jobRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILlmClient _llm;
    private readonly ILogger<AiAssistantService> _logger;

    public AiAssistantService(
        IJobRepository jobRepository,
        IUnitOfWork unitOfWork,
        ITenantProvider tenantProvider,
        ILlmClient llm,
        ILogger<AiAssistantService> logger)
    {
        _jobRepository = jobRepository;
        _unitOfWork = unitOfWork;
        _tenantProvider = tenantProvider;
        _llm = llm;
        _logger = logger;
    }

    public async Task<Result<JobAiSummaryDto>> GenerateJobSummaryAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _jobRepository.GetByIdForReportAsync(jobId, cancellationToken);
        if (job is null)
            return Result<JobAiSummaryDto>.NotFound();

        if (!CanAccessJob(job))
            return Result<JobAiSummaryDto>.NotFound();

        var checklist = job.JobTemplate.TemplateFields
            .OrderBy(f => f.SortOrder)
            .Select(f =>
            {
                var response = job.Responses.FirstOrDefault(r => r.TemplateFieldId == f.Id);
                return new
                {
                    f.Label,
                    FieldType = f.FieldType.ToString(),
                    Answer = FormatAnswer(f.FieldType, response)
                };
            });

        var payload = new
        {
            job.Title,
            Status = job.Status.ToString(),
            Customer = job.Customer?.Name,
            Technician = job.AssignedTechnician?.FullName,
            job.ScheduledAt,
            job.StartedAt,
            job.CompletedAt,
            job.Notes,
            Checklist = checklist
        };

        var system = """
            You are a field-service report writer. Write a concise professional inspection summary (3-6 sentences)
            followed by a short "Findings:" bullet list. Use only the provided JSON. Do not invent facts.
            """;
        var user = "Summarize this job:\n" + JsonSerializer.Serialize(payload);

        _logger.LogDebug("Generating AI summary for job {JobId}", jobId);
        var summary = await _llm.CompleteAsync(system, user, cancellationToken);

        job.AiSummary = summary;
        job.AiSummaryGeneratedAt = DateTime.UtcNow;
        _jobRepository.Update(job);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<JobAiSummaryDto>.Success(new JobAiSummaryDto(
            job.Id,
            summary,
            job.AiSummaryGeneratedAt.Value,
            _llm.IsStub,
            _llm.ModelName));
    }

    public async Task<Result<AiAskResponseDto>> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is null)
            return Result<AiAskResponseDto>.Forbidden("Company context required.");

        if (string.IsNullOrWhiteSpace(question))
            return Result<AiAskResponseDto>.Failure("Question is required.");

        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var snapshot = await _jobRepository.GetDashboardSnapshotAsync(
            todayStart, todayStart.AddDays(1), now.AddMinutes(-30), cancellationToken);

        var recent = await _jobRepository.GetPagedAsync(
            new JobFilter { From = now.AddDays(-7), To = now.AddDays(7) },
            new PaginationQuery { Page = 1, PageSize = 30 },
            cancellationToken);

        var context = new
        {
            GeneratedAtUtc = now,
            snapshot.CountsByStatus,
            snapshot.JobsScheduledToday,
            snapshot.SlaBreachedCount,
            Jobs = recent.Items.Select(j => new
            {
                j.Id,
                j.Title,
                Status = j.Status.ToString(),
                j.ScheduledAt,
                Customer = j.Customer?.Name,
                Technician = j.AssignedTechnician?.FullName,
                j.AssignedTechnicianId
            })
        };

        var system = """
            You are a FieldOps dispatcher assistant. Answer ONLY using the provided JSON context for this company.
            If the answer is not in the context, say you do not have that data. Do not invent job IDs.
            Be concise and actionable.
            """;
        var user = "Question: " + question.Trim() + "\n\nContext JSON:\n" + JsonSerializer.Serialize(context);

        _logger.LogDebug("Dispatcher AI ask for company {CompanyId}", _tenantProvider.CompanyId);
        var answer = await _llm.CompleteAsync(system, user, cancellationToken);

        return Result<AiAskResponseDto>.Success(new AiAskResponseDto(answer, _llm.IsStub, _llm.ModelName));
    }

    public async Task<Result<AiRiskHintsResponseDto>> GetRiskHintsAsync(int limit = 20, CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is null)
            return Result<AiRiskHintsResponseDto>.Forbidden("Company context required.");

        limit = Math.Clamp(limit, 1, 50);
        var now = DateTime.UtcNow;

        var openJobs = await _jobRepository.GetPagedAsync(
            new JobFilter(),
            new PaginationQuery { Page = 1, PageSize = 100 },
            cancellationToken);

        var scored = openJobs.Items
            .Where(j => j.Status is JobStatus.Scheduled or JobStatus.InProgress)
            .Select(j =>
            {
                var assessment = JobRiskScorer.Assess(
                    j.Status, j.ScheduledAt, j.StartedAt, j.AssignedTechnicianId, now);
                return new { Job = j, assessment.Score, assessment.Level, assessment.Reason };
            })
            .Where(x => x.Score >= 20)
            .OrderByDescending(x => x.Score)
            .Take(limit)
            .ToList();

        var items = new List<AiRiskHintDto>();
        foreach (var row in scored)
        {
            var system = "You are a field-ops dispatcher coach. Reply with ONE short actionable recommendation sentence only.";
            var user =
                $"Job '{row.Job.Title}' status={row.Job.Status}, scheduled={row.Job.ScheduledAt:u}, " +
                $"assigned={(row.Job.AssignedTechnicianId.HasValue ? "yes" : "no")}, risk={row.Level}, reason={row.Reason}. Recommend:";

            string recommendation;
            try
            {
                recommendation = await _llm.CompleteAsync(system, user, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "LLM recommendation failed for job {JobId}", row.Job.Id);
                recommendation = "Review assignment and schedule; follow up with the technician.";
            }

            items.Add(new AiRiskHintDto(
                row.Job.Id,
                row.Job.Title,
                row.Score,
                row.Level,
                row.Reason,
                recommendation.Trim()));
        }

        return Result<AiRiskHintsResponseDto>.Success(
            new AiRiskHintsResponseDto(items, _llm.IsStub, _llm.ModelName));
    }

    private bool CanAccessJob(Job job)
    {
        if (!string.Equals(_tenantProvider.Role, Roles.Technician, StringComparison.OrdinalIgnoreCase))
            return true;
        return job.AssignedTechnicianId == _tenantProvider.UserId;
    }

    private static string FormatAnswer(FieldType type, JobResponse? response)
    {
        if (response is null) return "—";
        return type switch
        {
            FieldType.Number => response.ValueNumber?.ToString() ?? "—",
            FieldType.Boolean => response.ValueBool switch { true => "Yes", false => "No", _ => "—" },
            FieldType.Photo => response.PhotoUrl ?? "—",
            _ => response.ValueText ?? "—"
        };
    }
}
