using FieldOps.COMMON.Enums;

namespace FieldOps.BLL.DTOs.Dashboard;

public record TechnicianWorkloadDto(Guid TechnicianId, string FullName, int OpenJobCount);

public record DashboardDto(
    IReadOnlyDictionary<string, int> CountsByStatus,
    int JobsScheduledToday,
    int SlaBreachedCount,
    IReadOnlyList<TechnicianWorkloadDto> TechnicianWorkload);
