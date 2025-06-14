
namespace TaskTracker.Application.TaskReminders.Commands;
public record CreateTaskCommand : IRequest<Result>
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset ScheduledFor { get; set; }
    public List<ReminderOffsetEnum> ReminderOffsets { get; set; } = [];
}

public class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.ScheduledFor).NotEmpty();
        RuleForEach(x => x.ReminderOffsets).IsInEnum();
    }
}

public class CreateTasksCommandHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<CreateTaskCommand, Result>
{
    public async Task<Result> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await taskTrackerService.CreateTaskAsync(request, cancellationToken);
        return result;
    }
}
