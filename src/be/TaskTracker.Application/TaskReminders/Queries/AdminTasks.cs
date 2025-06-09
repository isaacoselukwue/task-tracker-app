
namespace TaskTracker.Application.TaskReminders.Queries;
public record AdminTasksQuery : IRequest<Result<AdminTasksDto>>
{
    public int PageCount { get; set; }
    public int PageNumber { get; set; }
    public string? SearchString { get; set; }
    public StatusEnum? Status { get; set; }
    public DateTimeOffset? UpcomingStartDate { get; set; }
    public DateTimeOffset? UpcomingEndDate { get; set; }
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
        IQueryable<UpcomingTasksResult> tasks = taskTrackerService.GetTasks();

        if (!string.IsNullOrEmpty(request.SearchString))
        {
            tasks = tasks.Where(x => x.Title.Contains(request.SearchString));
        }

        if (request.UpcomingStartDate.HasValue && request.UpcomingEndDate.HasValue)
        {
            DateTimeOffset start = request.UpcomingStartDate.Value.Date;
            DateTimeOffset end = request.UpcomingEndDate.Value.Date.AddDays(1).AddTicks(-1);
            tasks = tasks.Where(x => x.ScheduledFor >= start && x.ScheduledFor <= end);
        }

        if (request.Status.HasValue)
        {
            tasks = tasks.Where(x => x.Status == request.Status.Value);
        }

        int totalResults = await tasks.CountAsync(cancellationToken: cancellationToken);
        int totalPages = (int)Math.Ceiling((double)totalResults / request.PageCount);

        List<UpcomingTasksResult> result = await tasks.Select(tasks => new UpcomingTasksResult
        {
            Description = tasks.Description,
            Id = tasks.Id,
            ScheduledFor = tasks.ScheduledFor,
            Title = tasks.Title
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