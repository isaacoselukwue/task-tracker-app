using TaskTracker.Application.TaskReminders;

namespace TaskTracker.Infrastructure.Tasks;
public class TaskTrackerNService(ITaskDbContext taskDbContext, ILogger<TaskTrackerNService> logger) : ITaskTrackerNService
{
    public async Task<List<TaskReminderDto>> GetPendingRemindersAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = DateTime.UtcNow;
        logger.LogInformation("GetPendingRemindersAsync: Current App Server UTC time is {ServerUtcNow}", utcNow);
        return await taskDbContext.TaskReminders
            .AsNoTracking()
            .Where(r =>
                !r.Sent && r.Task != null &&
                r.Task.Status == StatusEnum.Active &&
                r.Task.ScheduledFor + r.OffsetFromTaskTime <= utcNow)
            .Select(r => new TaskReminderDto
            {
                Id = r.Id,
                TaskId = r.TaskId,
                TaskName = r.Task != null ? r.Task.Title : string.Empty,
                TaskDescription = r.Task != null ? r.Task.Description ?? string.Empty : string.Empty,
                DueDate = r.Task != null ? r.Task.ScheduledFor : DateTimeOffset.UtcNow,
                UsersEmail = r.Task != null && r.Task.User != null ? r.Task.User.Email ?? string.Empty : string.Empty,
            })
            .ToListAsync(cancellationToken);
    }

    public async Task MarkReminderAsSentAsync(Guid reminderId, CancellationToken cancellationToken)
    {
        await taskDbContext.TaskReminders.Where(r => r.Id == reminderId).ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Sent, true)
            .SetProperty(r => r.SentAt, DateTimeOffset.UtcNow)
            .SetProperty(r => r.LastModified, DateTimeOffset.UtcNow)
            .SetProperty(r => r.LastModifiedBy, "TaskTracker.Worker"), cancellationToken: cancellationToken);
    }
}
