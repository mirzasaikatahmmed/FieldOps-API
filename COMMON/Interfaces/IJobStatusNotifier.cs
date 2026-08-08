namespace FieldOps.COMMON.Interfaces;

public interface IJobStatusNotifier
{
    Task NotifyJobStatusChangedAsync(
        Guid companyId,
        Guid jobId,
        string newStatus,
        string? technicianName,
        DateTime updatedAt,
        CancellationToken cancellationToken = default);
}
