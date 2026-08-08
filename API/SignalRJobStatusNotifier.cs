using FieldOps.API.Hubs;
using FieldOps.COMMON.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace FieldOps.API;

public class SignalRJobStatusNotifier : IJobStatusNotifier
{
    private readonly IHubContext<JobStatusHub> _hubContext;

    public SignalRJobStatusNotifier(IHubContext<JobStatusHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyJobStatusChangedAsync(
        Guid companyId,
        Guid jobId,
        string newStatus,
        string? technicianName,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group($"company-{companyId}")
            .SendAsync("JobStatusChanged", new
            {
                jobId,
                newStatus,
                technicianName,
                updatedAt
            }, cancellationToken);
    }
}
