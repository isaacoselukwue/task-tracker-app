namespace TaskTracker.Application.TaskReminders.Queries;
public record AdminTasksQuery : IRequest<Result<AdminTasksDto>>
{
    public int PageCount { get; set; }
    public int PageNumber { get; set; }
    public string? SearchString { get; set; }
    public StatusEnum? Status { get; set; }
    public DateTimeOffset? UpcomingStartDate { get; set; }
    public DateTimeOffset? UpcomingEndDate { get; set; }
    public Guid? UserId { get; set; }
}

public class AdminTasksValidator : AbstractValidator<AdminTasksQuery>
{
    public AdminTasksValidator()
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

public class AdminTasksQueryHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<AdminTasksQuery, Result<AdminTasksDto>>
{
    public async Task<Result<AdminTasksDto>> Handle(AdminTasksQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UpcomingTasksResult> tasks = taskTrackerService.GetTasksNoId();

        if (!string.IsNullOrEmpty(request.SearchString))
        {
            tasks = tasks.Where(x => EF.Functions.Like(x.Title, $"%{request.SearchString}%"));
        }

        if (request.UpcomingStartDate.HasValue && request.UpcomingEndDate.HasValue)
        {
            DateTime start = request.UpcomingStartDate.Value.UtcDateTime.Date;
            DateTimeOffset startUtc = new(start, TimeSpan.Zero);
            DateTime end = request.UpcomingEndDate.Value.UtcDateTime.Date.AddDays(1).AddTicks(-1);
            DateTimeOffset endUtc = new(end, TimeSpan.Zero);

            tasks = tasks.Where(x => x.ScheduledFor >= startUtc && x.ScheduledFor <= endUtc);
        }

        if (request.Status.HasValue)
        {
            tasks = tasks.Where(x => x.Status == request.Status.Value);
        }

        if (request.UserId is { } userId)
        {
            tasks = tasks.Where(x => x.UserId == userId);
        }

        int totalResults = await tasks.CountAsync(cancellationToken: cancellationToken);
        int totalPages = (int)Math.Ceiling((double)totalResults / request.PageCount);

        List<UpcomingTasksResult> result = await tasks.OrderBy(x => x.ScheduledFor).Select(tasks => new UpcomingTasksResult
        {
            Description = tasks.Description,
            Id = tasks.Id,
            ScheduledFor = tasks.ScheduledFor,
            Title = tasks.Title,
            UserId = tasks.UserId,
            Status = tasks.Status
        }).Skip((request.PageNumber - 1) * request.PageCount)
                .Take(request.PageCount)
                .ToListAsync(cancellationToken: cancellationToken);

        AdminTasksDto userTasksResult = new()
        {
            Page = request.PageNumber,
            Size = request.PageCount,
            TotalPages = totalPages,
            TotalResults = totalResults,
            Results = result
        };
        return Result<AdminTasksDto>.Success("Users tasks retrieved successfully.", userTasksResult);
    }
}

public class AdminTasksDto : UserTasksDto
{

}