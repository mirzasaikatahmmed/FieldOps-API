using FieldOps.COMMON.Interfaces;
using FieldOps.COMMON.Enums;
using FieldOps.DAL.Repositories;

namespace FieldOps.API.BackgroundServices;

public class SlaBreachChecker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SlaBreachChecker> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan BreachGrace = TimeSpan.FromMinutes(30);

    public SlaBreachChecker(IServiceScopeFactory scopeFactory, ILogger<SlaBreachChecker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "SLA breach checker failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobs = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var threshold = DateTime.UtcNow - BreachGrace;
        var breached = await jobs.GetSlaBreachedScheduledJobsAsync(threshold, cancellationToken);

        foreach (var job in breached)
        {
            var message =
                $"SLA breach: Job {job.Id} ('{job.Title}') was scheduled at {job.ScheduledAt:u} and is still {JobStatus.Scheduled}.";
            _logger.LogWarning("{Message}", message);
            await notifications.NotifyAdminAsync(message, cancellationToken);
        }
    }
}
