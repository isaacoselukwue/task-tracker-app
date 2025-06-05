namespace TaskTracker.Application.Accounts.Commands;

public record DeactivateAccountAdminCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
}
public class DeactivateAccountAdminValidator : AbstractValidator<DeactivateAccountAdminCommand>
{
    public DeactivateAccountAdminValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
    }
}

public class DeactivateAccountAdminCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<DeactivateAccountAdminCommand, Result>
{
    public async Task<Result> Handle(DeactivateAccountAdminCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.DeactivateAccountAsync(request.UserId);
        if (result.Item1.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(result.usersEmail, "Sorry To See You Go!", NotificationTypeEnum.DeactivateAccountSuccess, []), cancellationToken);
        }
        return result.Item1;
    }
}