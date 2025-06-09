global using TaskTracker.Application.TaskReminders;
global using TaskTracker.Application.TaskReminders.Commands;
using TaskTracker.Application.TaskReminders.Queries;

namespace TaskTracker.Application.Common.Interfaces;
public interface ITaskTrackerService
{
    Task<Result> CreateTaskAsync(CreateTaskCommand task, CancellationToken cancellationToken);
    IQueryable<UpcomingTasksResult> GetTasks();
    IQueryable<UpcomingTasksResult> GetTasks(StatusEnum status);
    IQueryable<UpcomingTasksResult> GetTasksNoId();
    Task<List<TaskReminderDto>> GetPendingRemindersAsync(CancellationToken cancellationToken);
    Task MarkReminderAsSentAsync(Guid reminderId, CancellationToken cancellationToken);
    Task<Result> MarkTaskAsDoneAsync(Guid taskId, CancellationToken cancellationToken);
    Task<Result> UpdateTaskAsync(Guid taskId, StatusEnum status, CancellationToken cancellationToken);
}
