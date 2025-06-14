namespace TaskTracker.Application.TaskReminders.Queries;
public record UserTasksQuery : IRequest<Result<UserTasksDto>>
{
    public int PageCount { get; set; }
    public int PageNumber { get; set; }
    public string? SearchString { get; set; }
    public StatusEnum? Status { get; set; }
    public DateTimeOffset? UpcomingStartDate { get; set; }
    public DateTimeOffset? UpcomingEndDate { get; set; }
}

public class UserTasksValidator : AbstractValidator<UserTasksQuery>
{
    public UserTasksValidator()
    {
        RuleFor(x => x.PageCount).GreaterThan(0);
        RuleFor(x => x.PageNumber).GreaterThan(0);
        When(x => x.UpcomingStartDate.HasValue, () =>
        {
            RuleFor(x => x.UpcomingEndDate)
                .NotNull().WithMessage("Upcoming end date is required when start date is provided.");

            RuleFor(x => x)
                .Must(x => x.UpcomingEndDate >= x.UpcomingStartDate)
                .WithMessage("Upcoming end date must be after start date.")
                .When(x => x.UpcomingEndDate.HasValue);
        });
        RuleFor(x => x.UpcomingStartDate)
            .NotNull()
            .When(x => x.UpcomingEndDate.HasValue)
            .WithMessage("Upcoming start date is required when end date is provided.");
        RuleFor(x => x.Status)
            .Must(status => status != StatusEnum.Pending)
            .When(x => x.Status.HasValue)
            .WithMessage("Filtering by 'Pending' status is not allowed.")
            .IsInEnum();
    }
}

public class UserTasksQueryHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<UserTasksQuery, Result<UserTasksDto>>
{
    public async Task<Result<UserTasksDto>> Handle(UserTasksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UpcomingTasksResult> userTasks = taskTrackerService.GetTasks();

        if (!string.IsNullOrEmpty(request.SearchString))
        {
            userTasks = userTasks.Where(x => EF.Functions.Like(x.Title, $"%{request.SearchString}%"));
        }

        if (request.UpcomingStartDate.HasValue && request.UpcomingEndDate.HasValue)
        {
            DateTime start = request.UpcomingStartDate.Value.UtcDateTime.Date;
            DateTimeOffset startUtc = new(start, TimeSpan.Zero);
            DateTime end = request.UpcomingEndDate.Value.UtcDateTime.Date.AddDays(1).AddTicks(-1);
            DateTimeOffset endUtc = new(end, TimeSpan.Zero);

            userTasks = userTasks.Where(x => x.ScheduledFor >= startUtc && x.ScheduledFor <= endUtc);
        }

        if (request.Status.HasValue)
        {
            userTasks = userTasks.Where(x => x.Status == request.Status.Value);
        }

        int totalResults = await userTasks.CountAsync(cancellationToken: cancellationToken);
        int totalPages = (int)Math.Ceiling((double)totalResults / request.PageCount);

        List<UpcomingTasksResult> result = await userTasks.Select(tasks => new UpcomingTasksResult
        {
            Description = tasks.Description,
            Id = tasks.Id,
            ScheduledFor = tasks.ScheduledFor,
            Status = tasks.Status,
            Title = tasks.Title
        }).Skip((request.PageNumber - 1) * request.PageCount)
                .Take(request.PageCount)
                .ToListAsync(cancellationToken: cancellationToken);

        UserTasksDto userTasksResult = new()
        {
            Page = request.PageNumber,
            Size = request.PageCount,
            TotalPages = totalPages,
            TotalResults = totalResults,
            Results = result
        };
        return Result<UserTasksDto>.Success("Users tasks retrieved successfully.", userTasksResult);
    }
}

public class UserTasksDto
{
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalPages { get; set; }
    public int TotalResults { get; set; }
    public List<UpcomingTasksResult> Results { get; set; } = [];
}