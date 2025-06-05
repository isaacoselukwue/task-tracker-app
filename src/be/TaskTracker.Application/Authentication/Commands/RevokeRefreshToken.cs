namespace TaskTracker.Application.Authentication.Commands;
public class RevokeRefreshTokenCommand : IRequest<Result>
{
    public string? EncryptedToken { get; set; }
}

public class RevokeRefreshTokenValidator : AbstractValidator<RevokeRefreshTokenCommand>
{
    public RevokeRefreshTokenValidator()
    {
        RuleFor(x => x.EncryptedToken).NotEmpty();
    }
}

public class RevokeRefreshTokenCommandHandler(IIdentityService identityService) : IRequestHandler<RevokeRefreshTokenCommand, Result>
{
    public async Task<Result> Handle(RevokeRefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return await identityService.RevokeRefreshUserTokenAsync(request.EncryptedToken!);
    }
}