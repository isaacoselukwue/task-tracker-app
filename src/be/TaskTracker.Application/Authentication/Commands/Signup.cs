namespace TaskTracker.Application.Authentication.Commands;
public record SignupCommand : IRequest<Result>
{
    public string? EmailAddress { get; set; }
    public string? Password { get; set; }
    public string? ConfirmPassword { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class SignupValidator : AbstractValidator<SignupCommand>
{
    public SignupValidator()
    {
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password);
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
        RuleFor(x => x.PhoneNumber).NotEmpty().MinimumLength(7).MaximumLength(14)
            .Matches(@"^[0]\d+$").WithMessage("Phone number must start with '0' and contain only digits"); ;
    }
}

public class SignupCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<SignupCommand, Result>
{
    public async Task<Result> Handle(SignupCommand request, CancellationToken cancellationToken)
    {
        var result = await identityService.SignUpUserAsync(request.EmailAddress!, request.Password!, request.FirstName!, request.LastName!, request.PhoneNumber!);

        if (!result.Item1.Succeeded)
        {
            return result.Item1;
        }
        Dictionary<string, string> emailData = new()
        {
            {"{{token}}", result.token },
            {"{{userid}}", result.Item1.Message }
        };
        await publisher.Publish(new NotificationEvent(request.EmailAddress!, "Account Activation!", NotificationTypeEnum.SignUpAccountActivation, emailData), cancellationToken);
        return Result.Success("Signup successful. Please check your mail for activation link.");
    }
}