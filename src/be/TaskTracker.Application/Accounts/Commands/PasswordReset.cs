namespace TaskTracker.Application.Accounts.Commands;

public class PasswordResetCommand : IRequest<Result>
{
    public string? UserId { get; set; }
    public string? ResetToken { get; set; }
    public string? NewPassword { get; set; }
    public string? ConfirmPassword { get; set; }
}

public class PasswordResetValidator : AbstractValidator<PasswordResetCommand>
{
    public PasswordResetValidator()
    {
        RuleFor(v => v.UserId).NotEmpty().Must(BeAValidGuid).WithMessage("Invalid user.");
        RuleFor(v => v.ResetToken).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty();
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
    private bool BeAValidGuid(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) return false;
        return Guid.TryParse(guid, out _);
    }
}

public class PasswordResetCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<PasswordResetCommand, Result>
{
    public async Task<Result> Handle(PasswordResetCommand request, CancellationToken cancellationToken)
    {
        (Result result, string emailAddress) = await identityService.ResetPasswordAsync(request.NewPassword!, request.UserId!, request.ResetToken!);
        if (result.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(emailAddress, "Password Reset Successful", NotificationTypeEnum.PasswordResetSuccess, []), cancellationToken);
        }
        return result;
    }
}