using FieldOps.COMMON.Entities;

namespace FieldOps.COMMON.Interfaces;

public interface IPdfService
{
    Task<(string StorageKey, string Url)> GenerateJobReportAsync(Job job, CancellationToken cancellationToken = default);
}
