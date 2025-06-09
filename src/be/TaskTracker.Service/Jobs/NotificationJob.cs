global using System.Collections.Concurrent;
global using TaskTracker.Application.TaskReminders;
global using TaskTracker.Domain.Enums;

namespace TaskTracker.Service.Jobs;
public interface INotificationJob
{
    Task ProcessNotifications(CancellationToken cancellationToken);
}
public class NotificationJob(ILogger<NotificationJob> logger, IBus bus, IServiceScopeFactory scopeFactory) : INotificationJob
{
    private readonly ILogger<NotificationJob> _logger = logger;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    public async Task ProcessNotifications(CancellationToken cancellationToken)
    {
        using IServiceScope scope = _scopeFactory.CreateScope();
        ITaskTrackerService taskService = scope.ServiceProvider.GetRequiredService<ITaskTrackerService>();
        IEmailService emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        List<TaskReminderDto> pendingReminders = await taskService.GetPendingRemindersAsync(cancellationToken);
        _logger.LogInformation("Found {count} pending reminders to send.", pendingReminders.Count);

        ConcurrentBag<Task> bag = [];

        foreach (TaskReminderDto reminder in pendingReminders)
        {
            bag.Add(Task.Run(async () =>
            {
                try
                {
                    Dictionary<string, string> replacements = new(){
                        {"{{TaskName}}", reminder.TaskName },
                        {"{{TaskDescription}}", reminder.TaskDescription },
                        {"DueDate", reminder.DueDate.ToString("dd-MMM-yyyy hh:mm") }
                    };
                    await bus.Publish(new NotificationEvent(reminder.UsersEmail, "You have an upcoming Task", NotificationTypeEnum.UpcomingTaskReminder, replacements), cancellationToken);
                    await taskService.MarkReminderAsSentAsync(reminder.Id, cancellationToken);
                    _logger.LogInformation("Sent reminder for TaskId: {TaskId}", reminder.TaskId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send reminder for TaskId: {TaskId}", reminder.TaskId);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(bag);
    }

}
