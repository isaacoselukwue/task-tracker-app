using TaskTracker.Service.Jobs;

namespace TaskTracker.Service;

public class Worker(ILogger<Worker> logger, INotificationJob notificationJob) : BackgroundService
{
    private readonly ILogger<Worker> _logger = logger;
    private readonly INotificationJob _notificationJob = notificationJob;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("TaskReminderWorker started at: {time}", DateTimeOffset.UtcNow);
                await _notificationJob.ProcessNotifications(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error occurred while processing reminders.");
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}
