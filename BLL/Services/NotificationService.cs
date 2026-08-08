using FieldOps.COMMON.Interfaces;
using Microsoft.Extensions.Logging;

namespace FieldOps.BLL.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyAdminAsync(string message, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("ADMIN NOTIFICATION: {Message}", message);
        return Task.CompletedTask;
    }
}
