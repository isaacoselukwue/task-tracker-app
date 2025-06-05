namespace TaskTracker.Application.Accounts.Commands;

public record DeleteAccountCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public bool IsPermanant { get; set; }
}

public class DeleteAccountValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
    }
}

public class DeleteAccountCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<DeleteAccountCommand, Result>
{
    public async Task<Result> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.DeleteUserAsync(request.UserId.ToString(), request.IsPermanant);
        if(result.Item1.Succeeded)
            await publisher.Publish(new NotificationEvent(result.usersEmail!, "Sorry to see you go!", NotificationTypeEnum.DeleteAccountSuccess, []), cancellationToken);
        return result.Item1;
    }
}
