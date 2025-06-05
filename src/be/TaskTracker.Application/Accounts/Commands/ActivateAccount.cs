namespace TaskTracker.Application.Accounts.Commands;

public record ActivateAccountCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
}

public class ActivateAccountValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
    }
}

public class ActivateAccountCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<ActivateAccountCommand, Result>
{
    public async Task<Result> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.ActivateAccountAsync(request.UserId);
        if (result.Item1.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(result.email, "Account Activated", NotificationTypeEnum.AccountActivationAdmin, []), cancellationToken);
        }
        return result.Item1;
    }
}
