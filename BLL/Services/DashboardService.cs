using FieldOps.BLL.DTOs.Dashboard;
using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Models;
using FieldOps.DAL.Repositories;

namespace FieldOps.BLL.Services;

public interface IDashboardService
{
    Task<Result<DashboardDto>> GetAsync(CancellationToken cancellationToken = default);
}

public class DashboardService : IDashboardService
{
    private readonly IJobRepository _jobRepository;
    private readonly ITenantProvider _tenantProvider;

    public DashboardService(IJobRepository jobRepository, ITenantProvider tenantProvider)
    {
        _jobRepository = jobRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<DashboardDto>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (_tenantProvider.CompanyId is null)
            return Result<DashboardDto>.Forbidden("Company context required.");

        var now = DateTime.UtcNow;
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);
        var todayEnd = todayStart.AddDays(1);
        var slaThreshold = now.AddMinutes(-30);

        var snapshot = await _jobRepository.GetDashboardSnapshotAsync(todayStart, todayEnd, slaThreshold, cancellationToken);

        return Result<DashboardDto>.Success(new DashboardDto(
            snapshot.CountsByStatus.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value),
            snapshot.JobsScheduledToday,
            snapshot.SlaBreachedCount,
            snapshot.TechnicianWorkload
                .Select(w => new TechnicianWorkloadDto(w.TechnicianId, w.FullName, w.OpenJobCount))
                .ToList()));
    }
}
