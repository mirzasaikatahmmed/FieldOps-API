using FieldOps.COMMON.Enums;

namespace FieldOps.BLL.Services;

public static class JobStatusTransitions
{
    private static readonly Dictionary<JobStatus, HashSet<JobStatus>> Allowed = new()
    {
        [JobStatus.Scheduled] = [JobStatus.InProgress, JobStatus.Cancelled],
        [JobStatus.InProgress] = [JobStatus.Completed, JobStatus.Cancelled],
        [JobStatus.Completed] = [],
        [JobStatus.Cancelled] = []
    };

    public static bool CanTransition(JobStatus from, JobStatus to)
    {
        if (from == to)
            return true;

        return Allowed.TryGetValue(from, out var next) && next.Contains(to);
    }
}
