namespace TaskTracker.Application.Authentication.Commands;
public record SignupVerificationCommand : IRequest<Result>
{
    public string? UserId { get; set; }
    public string? ActivationToken { get; set; }
}

public class SignupVerificationValidator : AbstractValidator<SignupVerificationCommand>
{
    public SignupVerificationValidator()
    {
        RuleFor(x => x.UserId).NotEmpty().Must(BeAValidGuid).WithMessage("Invalid user");
        RuleFor(x => x.ActivationToken).NotEmpty();
    }
    private bool BeAValidGuid(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid)) return false;
        return Guid.TryParse(guid, out _);
    }
}

public class SignupVerificationCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<SignupVerificationCommand, Result>
{
    public async Task<Result> Handle(SignupVerificationCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.ValidateSignupAsync(request.UserId!, request.ActivationToken!);
        if (!result.Item1.Succeeded) return result.Item1;

        await publisher.Publish(new NotificationEvent(result.usersEmail!, "Account Activation Succeeded!", NotificationTypeEnum.SignUpCompleted, []), cancellationToken);
        return result.Item1;
    }
}