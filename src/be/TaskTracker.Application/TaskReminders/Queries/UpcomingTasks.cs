namespace TaskTracker.Application.TaskReminders.Queries;
public record GetUpcomingTasksQuery(int PageCount, int PageNumber) : IRequest<Result<UpcomingTasksDto>>;

public class GetUpcomingTasksValidator : AbstractValidator<GetUpcomingTasksQuery>
{
    public GetUpcomingTasksValidator()
    {
        RuleFor(x => x.PageCount).GreaterThan(0);
        RuleFor(x => x.PageNumber).GreaterThan(0);
    }
}

public class GetUpcomingTasksQueryHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<GetUpcomingTasksQuery, Result<UpcomingTasksDto>>
{
    public async Task<Result<UpcomingTasksDto>> Handle(GetUpcomingTasksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UpcomingTasksResult> upcomingTasks = taskTrackerService.GetTasks(StatusEnum.Active);
        int totalResults = await upcomingTasks.CountAsync(cancellationToken: cancellationToken);
        int totalPages = (int)Math.Ceiling((double)totalResults / request.PageCount);

        List<UpcomingTasksResult> result = await upcomingTasks.Select(tasks => new UpcomingTasksResult
        {
            Description = tasks.Description,
            Id = tasks.Id,
            ScheduledFor = tasks.ScheduledFor,
            Title = tasks.Title
        }).Skip((request.PageNumber - 1) * request.PageCount)
                .Take(request.PageCount)
                .ToListAsync(cancellationToken: cancellationToken);

        UpcomingTasksDto upcomingTasksResult = new()
        {
            Page = request.PageNumber,
            Size = request.PageCount,
            TotalPages = totalPages,
            TotalResults = totalResults,
            Results = result
        };
        return Result<UpcomingTasksDto>.Success("Upcoming tasks retrieved successfully.", upcomingTasksResult);
    }
}

public class UpcomingTasksDto
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<UpcomingTasksResult> Results { get; set; } = [];
}

public class UpcomingTasksResult
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public StatusEnum Status { get; set; }
}