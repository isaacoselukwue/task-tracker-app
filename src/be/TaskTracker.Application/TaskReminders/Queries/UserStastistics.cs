
namespace TaskTracker.Application.TaskReminders.Queries;
public class UserStastisticsQuery : IRequest<Result<UserStastisticsDto>>
{
    public bool RefreshData { get; set; }
}

public class UserStastisticsQueryHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<UserStastisticsQuery, Result<UserStastisticsDto>>
{
    public async Task<Result<UserStastisticsDto>> Handle(UserStastisticsQuery request, CancellationToken cancellationToken)
    {
        var result = await taskTrackerService.GetUserStastisticsAsync(request.RefreshData, cancellationToken);
        return Result<UserStastisticsDto>.Success("User stastistics returned", result);
    }
}

public class UserStastisticsDto
{
    public long UsersCount { get; set; }
    public long ActiveUsersCount { get; set; }
    public long DeactivatedUsersCount { get; set; }
    public long DeletedUsersCount { get; set; }
    public long PendingUsersCount { get; set; }

}