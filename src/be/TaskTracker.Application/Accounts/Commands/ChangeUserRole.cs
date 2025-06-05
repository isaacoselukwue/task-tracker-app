global using TaskTracker.Domain.Constants;

namespace TaskTracker.Application.Accounts.Commands;

public record ChangeUserRoleCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
}

public class ChangeUserRoleValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleValidator()
    {
        RuleFor(v => v.UserId).NotEmpty();
        RuleFor(v => v.Role).NotEmpty().Must(BeAValidRole).WithMessage($"Supported roles are {Roles.Admin} and {Roles.User} only");
    }
    private bool BeAValidRole(string? role)
    {
        return role == Roles.Admin || role == Roles.User;
    }
}

public class ChangeUserRoleCommandHandler(IIdentityService identityService, IPublisher publisher) : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var (result, email) = await identityService.ChangeUserRoleAsync(request.UserId.ToString(), request.Role!);
        if(result.Succeeded)
        {
            await publisher.Publish(new NotificationEvent(email, "Role Changed", NotificationTypeEnum.ChangeRoleSuccess, []), cancellationToken);
        }
        return result;
    }
}