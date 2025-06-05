namespace TaskTracker.Application.Accounts.Commands;

public record ChangePasswordCommand : IRequest<Result>
{
    public string? NewPassword { get; set; }
    public string? ConfirmNewPassword { get; set; }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty();
        RuleFor(x => x.ConfirmNewPassword).Equal(x => x.NewPassword);
    }
}

public class ChangePasswordCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.ChangePasswordAsync(request.NewPassword!);
        if(result.Item1.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(result.email, "Password Changed", NotificationTypeEnum.ChangePasswordSuccess, []), cancellationToken);
        }
        return result.Item1;
    }
}
