
namespace TaskTracker.Application.TaskReminders.Commands;
public record DeleteTaskCommand(Guid TaskId) : IRequest<Result>;

public class DeleteTaskValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskValidator()
    {
        RuleFor(x => x.TaskId).NotEmpty();
    }
}

public class DeleteTaskCommandHandler(ITaskTrackerService taskTrackerService) : IRequestHandler<DeleteTaskCommand, Result>
{
    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var result = await taskTrackerService.UpdateTaskAsync(request.TaskId, StatusEnum.Deleted, cancellationToken);
        return result;
    }
}
