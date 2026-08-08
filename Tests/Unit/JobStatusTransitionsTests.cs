using FieldOps.BLL.Services;
using FieldOps.COMMON.Enums;
using FluentAssertions;
using Xunit;

namespace FieldOps.Tests.Unit;

public class JobStatusTransitionsTests
{
    [Theory]
    [InlineData(JobStatus.Scheduled, JobStatus.InProgress, true)]
    [InlineData(JobStatus.Scheduled, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.Scheduled, JobStatus.Completed, false)]
    [InlineData(JobStatus.InProgress, JobStatus.Completed, true)]
    [InlineData(JobStatus.InProgress, JobStatus.Cancelled, true)]
    [InlineData(JobStatus.InProgress, JobStatus.Scheduled, false)]
    [InlineData(JobStatus.Completed, JobStatus.Scheduled, false)]
    [InlineData(JobStatus.Completed, JobStatus.InProgress, false)]
    [InlineData(JobStatus.Cancelled, JobStatus.Scheduled, false)]
    [InlineData(JobStatus.Completed, JobStatus.Completed, true)]
    public void CanTransition_EnforcesRules(JobStatus from, JobStatus to, bool expected)
    {
        JobStatusTransitions.CanTransition(from, to).Should().Be(expected);
    }
}
