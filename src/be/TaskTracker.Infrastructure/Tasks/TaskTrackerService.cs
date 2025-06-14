using Microsoft.Extensions.Caching.Hybrid;
using TaskTracker.Application.TaskReminders;
using TaskTracker.Application.TaskReminders.Commands;
using TaskTracker.Application.TaskReminders.Queries;

namespace TaskTracker.Infrastructure.Tasks;
public class TaskTrackerService (HybridCache hybridCache, ICurrentUser currentUser, ITaskDbContext taskDbContext)
    : ITaskTrackerService
{
    public async Task<List<TaskReminderDto>> GetPendingRemindersAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = DateTime.UtcNow;

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

    public async Task<Result> CreateTaskAsync(CreateTaskCommand task, CancellationToken cancellationToken)
    {
        Domain.Entities.Tasks tasks = new()
        {
            Description = task.Description,
            Reminders = [.. task.ReminderOffsets.Select(offsetType => new TasksReminder
            {
                OffsetFromTaskTime = offsetType switch
                {
                    ReminderOffsetEnum.AtTime => TimeSpan.Zero,
                    ReminderOffsetEnum.OneHourBefore => TimeSpan.FromHours(-1),
                    ReminderOffsetEnum.OneDayBefore => TimeSpan.FromDays(-1),
                    _ => TimeSpan.Zero
                },
                Sent = false
            })],
            UserId = currentUser.UserId,
            ScheduledFor = task.ScheduledFor.ToUniversalTime(),
            Status = StatusEnum.Active,
            Title = task.Title!
        };
        await taskDbContext.Tasks.AddAsync(tasks, cancellationToken);
        int result = await taskDbContext.SaveChangesAsync(cancellationToken);
        return result > 0 ? Result.Success("Task successfully created") 
            : Result.Failure("We could not create task at this time", ["Tasks not saved"]);
    }

    public IQueryable<UpcomingTasksResult> GetTasks(StatusEnum status)
    {
        return taskDbContext.Tasks.Where(x => x.UserId == currentUser.UserId && x.Status == status).Select(query => new UpcomingTasksResult
        {
            Description = query.Description,
            Id = query.Id,
            ScheduledFor = query.ScheduledFor,
            Title = query.Title,
            Status = query.Status,
            UserId = query.UserId
        }).AsQueryable();
    }

    public IQueryable<UpcomingTasksResult> GetTasks()
    {
        return taskDbContext.Tasks.Where(x => x.UserId == currentUser.UserId).Select(query => new UpcomingTasksResult
        {
            Description = query.Description,
            Id = query.Id,
            ScheduledFor = query.ScheduledFor,
            Title = query.Title,
            Status = query.Status,
            UserId = query.UserId
        }).AsQueryable();
    }

    public IQueryable<UpcomingTasksResult> GetTasksNoId() =>
        taskDbContext.Tasks.Select(query => new UpcomingTasksResult
        {
            Description = query.Description,
            Id = query.Id,
            ScheduledFor = query.ScheduledFor,
            Title = query.Title,
            Status = query.Status,
            UserId = query.UserId
        }).AsQueryable();

    private async Task<UserStastisticsDto> GetFreshStatisticsAsync(CancellationToken cancellationToken)
    {
        var query = await taskDbContext.Users
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Active = g.Count(x => x.UsersStatus == StatusEnum.Active),
                Inactive = g.Count(x => x.UsersStatus == StatusEnum.InActive),
                Deleted = g.Count(x => x.UsersStatus == StatusEnum.Deleted),
                Pending = g.Count(x => x.UsersStatus == StatusEnum.Pending)
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new UserStastisticsDto
        {
            UsersCount = query?.Total ?? 0,
            ActiveUsersCount = query?.Active ?? 0,
            DeactivatedUsersCount = query?.Inactive ?? 0,
            DeletedUsersCount = query?.Deleted ?? 0,
            PendingUsersCount = query?.Pending ?? 0
        };
    }


    public async Task<UserStastisticsDto> GetUserStastisticsAsync(bool refresh, CancellationToken cancellationToken)
    {
        const string cacheKey = $"{nameof(UserStastisticsDto)}";
        if (refresh)
        {
            UserStastisticsDto freshData = await GetFreshStatisticsAsync(cancellationToken);
            await hybridCache.SetAsync(cacheKey, freshData, cancellationToken: cancellationToken);
            return freshData;
        }
        else
        {
            return await hybridCache.GetOrCreateAsync(cacheKey, async entry =>
            {
                return await GetFreshStatisticsAsync(cancellationToken);
            }, cancellationToken: cancellationToken);
        }
    }

    public async Task<Result> MarkTaskAsDoneAsync(Guid taskId, CancellationToken cancellationToken)
    {
        int update = await taskDbContext.Tasks.Where(x => x.Id == taskId && x.UserId == currentUser.UserId)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Status, StatusEnum.InActive)
            .SetProperty(r => r.LastModified, DateTimeOffset.UtcNow)
            .SetProperty(r => r.LastModifiedBy, currentUser.Email),
            cancellationToken);
        return update > 0 ? Result.Success("Task marked as completed")
            : Result.Failure("Task could not be marked as completed", ["Please try later"]);
    }

    public async Task<Result> UpdateTaskAsync(Guid taskId, StatusEnum status, CancellationToken cancellationToken)
    {
        int update = await taskDbContext.Tasks.Where(x => x.Id == taskId && x.UserId == currentUser.UserId)
            .ExecuteUpdateAsync(setters => setters
            .SetProperty(r => r.Status, status)
            .SetProperty(r => r.LastModified, DateTimeOffset.UtcNow)
            .SetProperty(r => r.LastModifiedBy, currentUser.Email),
            cancellationToken);
        return update > 0 ? Result.Success("Task updated successsfully")
            : Result.Failure("Task could not be updated", ["Please try later"]);
    }
}
