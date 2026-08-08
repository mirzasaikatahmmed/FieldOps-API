namespace FieldOps.COMMON.Interfaces;

public interface INotificationService
{
    Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default);
}
