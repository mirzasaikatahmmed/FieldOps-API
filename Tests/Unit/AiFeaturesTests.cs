using FieldOps.BLL.Services;
using FieldOps.COMMON.Enums;
using FluentAssertions;
using Xunit;

namespace FieldOps.Tests.Unit;

public class JobRiskScorerTests
{
    private static readonly DateTime Now = new(2026, 8, 8, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Scheduled_unassigned_and_overdue_is_high()
    {
        var result = JobRiskScorer.Assess(
            JobStatus.Scheduled,
            scheduledAt: Now.AddHours(-2),
            startedAt: null,
            assignedTechnicianId: null,
            utcNow: Now);

        result.Level.Should().Be("High");
        result.Score.Should().BeGreaterThanOrEqualTo(70);
        result.Reason.Should().Contain("No technician assigned");
    }

    [Fact]
    public void Scheduled_within_hour_is_low_or_medium()
    {
        var result = JobRiskScorer.Assess(
            JobStatus.Scheduled,
            scheduledAt: Now.AddMinutes(30),
            startedAt: null,
            assignedTechnicianId: Guid.NewGuid(),
            utcNow: Now);

        result.Score.Should().Be(20);
        result.Level.Should().Be("Low");
    }

    [Fact]
    public void InProgress_long_open_increases_score()
    {
        var result = JobRiskScorer.Assess(
            JobStatus.InProgress,
            scheduledAt: Now.AddHours(-6),
            startedAt: Now.AddHours(-5),
            assignedTechnicianId: Guid.NewGuid(),
            utcNow: Now);

        result.Level.Should().Be("Medium");
        result.Reason.Should().Contain("InProgress");
    }
}

public class StubLlmClientTests
{
    [Fact]
    public async Task Stub_returns_summary_style_for_default_prompt()
    {
        var client = new StubLlmClient();
        var text = await client.CompleteAsync("You are a report writer.", "Summarize this job: {...}");
        client.IsStub.Should().BeTrue();
        client.ModelName.Should().Be("stub-local");
        text.Should().Contain("Findings");
    }

    [Fact]
    public async Task Stub_returns_recommendation_for_risk_prompt()
    {
        var client = new StubLlmClient();
        var text = await client.CompleteAsync("coach", "risk recommend for overdue job");
        text.Should().Contain("technician");
    }
}
