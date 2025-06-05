namespace TaskTracker.Application.Accounts.Commands;

public record DeactivateAccountCommand : IRequest<Result>
{
}

public class DeactivateAccountCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<DeactivateAccountCommand, Result>
{
    public async Task<Result> Handle(DeactivateAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.DeactivateAccountAsync();
        if(result.Item1.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(result.usersEmail, "Sorry To See You Go!", NotificationTypeEnum.DeactivateAccountSuccess, []), cancellationToken);
        }
        return result.Item1;
    }
}
