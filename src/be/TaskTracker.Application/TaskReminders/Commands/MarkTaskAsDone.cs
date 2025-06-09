
namespace TaskTracker.Application.TaskReminders.Commands;
public record MarkTaskAsDoneCommand(Guid TaskId) : IRequest<Result>;

public class MarkTaskAsDoneValidator : AbstractValidator<MarkTaskAsDoneCommand>
{
    public MarkTaskAsDoneValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}

public class MarkTaskAsDoneCommandHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<MarkTaskAsDoneCommand, Result>
{
    public async Task<Result> Handle(MarkTaskAsDoneCommand request, CancellationToken cancellationToken)
    {
        var result = await taskTrackerService.MarkTaskAsDoneAsync(request.TaskId, cancellationToken);
        return result;
    }
}
