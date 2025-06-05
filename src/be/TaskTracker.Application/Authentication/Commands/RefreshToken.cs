namespace TaskTracker.Application.Authentication.Commands;
public record RefreshTokenCommand : IRequest<Result<LoginDto>>
{
    public string? EncryptedToken { get; set; }
}

public class RefreshTokenValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.EncryptedToken).NotEmpty();
    }
}

public class RefreshTokenCommandHandler(IIdentityService identityService) : IRequestHandler<RefreshTokenCommand, Result<LoginDto>>
{
    public async Task<Result<LoginDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        Result<LoginDto> result = await identityService.RefreshUserTokenAsync(request.EncryptedToken!);
        return result;
    }
}