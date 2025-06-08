global using TaskTracker.Application.Tasks;

namespace TaskTracker.Application.Common.Interfaces;
public interface ITaskTrackerService
{
    Task<List<TaskReminderDto>> GetPendingRemindersAsync();
    Task MarkReminderAsSentAsync(Guid reminderId);
}
