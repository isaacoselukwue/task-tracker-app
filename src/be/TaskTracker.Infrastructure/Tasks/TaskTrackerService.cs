using TaskTracker.Application.Tasks;

namespace TaskTracker.Infrastructure.Tasks;
internal class TaskTrackerService : ITaskTrackerService
{
    public Task<List<TaskReminderDto>> GetPendingRemindersAsync()
    {
        throw new NotImplementedException();
    }

    public Task MarkReminderAsSentAsync(Guid reminderId)
    {
        throw new NotImplementedException();
    }
}
