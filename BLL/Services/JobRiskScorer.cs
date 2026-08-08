using FieldOps.COMMON.Enums;

namespace FieldOps.BLL.Services;

public static class JobRiskScorer
{
    public sealed record RiskAssessment(int Score, string Level, string Reason);

    public static RiskAssessment Assess(
        JobStatus status,
        DateTime scheduledAt,
        DateTime? startedAt,
        Guid? assignedTechnicianId,
        DateTime utcNow)
    {
        var score = 0;
        var reasons = new List<string>();

        if (status == JobStatus.Scheduled)
        {
            var minutesLate = (utcNow - scheduledAt).TotalMinutes;
            if (minutesLate >= 30)
            {
                score += 50;
                reasons.Add($"Scheduled job is {Math.Floor(minutesLate)} minutes past ScheduledAt (SLA breach window).");
            }
            else if (minutesLate >= 0)
            {
                score += 35;
                reasons.Add("Scheduled job is overdue but within the first 30 minutes.");
            }
            else if (minutesLate >= -60)
            {
                score += 20;
                reasons.Add("Job starts within the next hour.");
            }

            if (assignedTechnicianId is null)
            {
                score += 25;
                reasons.Add("No technician assigned.");
            }
        }
        else if (status == JobStatus.InProgress)
        {
            var started = startedAt ?? scheduledAt;
            var minutesOpen = (utcNow - started).TotalMinutes;
            if (minutesOpen >= 240)
            {
                score += 45;
                reasons.Add($"InProgress for {Math.Floor(minutesOpen / 60)}+ hours.");
            }
            else if (minutesOpen >= 120)
            {
                score += 25;
                reasons.Add("InProgress for over 2 hours.");
            }

            if (assignedTechnicianId is null)
            {
                score += 20;
                reasons.Add("InProgress job has no assigned technician.");
            }
        }

        score = Math.Clamp(score, 0, 100);
        var level = score switch
        {
            >= 70 => "High",
            >= 40 => "Medium",
            _ => "Low"
        };

        var reason = reasons.Count == 0
            ? "No elevated risk signals."
            : string.Join(" ", reasons);

        return new RiskAssessment(score, level, reason);
    }
}
