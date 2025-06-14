namespace TaskTracker.Tests.Authentication.Commands;
[TestFixture]
class RefreshTokenTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private RefreshTokenCommand emptyTokenCommand;
    private RefreshTokenCommand validCommand;
    private RefreshTokenValidator _validator;
    private RefreshTokenCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new();
        _validator = new();

        emptyTokenCommand = new()
        {
            EncryptedToken = ""
        };

        validCommand = new()
        {
            EncryptedToken = "valid-encrypted-token"
        };

        _handler = new(_mockIdentityService.Object);
    }

    [Test]
    public async Task RefreshToken_EmptyToken_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyTokenCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task RefreshToken_ValidToken_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task RefreshToken_InvalidToken_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.RefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result<LoginDto>.Failure(ResultMessage.TokenRefreshFailed, ["Invalid or expired token"]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid or expired token"));
        });
    }

    [Test]
    public async Task RefreshToken_RevokedToken_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.RefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result<LoginDto>.Failure(ResultMessage.TokenRefreshFailed, ["Token has been revoked"]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.TokenRefreshFailed));
        });
    }

    [Test]
    public async Task RefreshToken_ValidToken_ReturnsSuccess()
    {
        LoginDto loginDto = new()
        {
            AccessToken = new Microsoft.AspNetCore.Authentication.BearerToken.AccessTokenResponse
            {
                AccessToken = "new-access-token",
                ExpiresIn = 86400,
                RefreshToken = "new-refresh-token"
            }
        };

        _mockIdentityService.Setup(x => x.RefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result<LoginDto>.Success(ResultMessage.AccessTokenGenerated, loginDto));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data?.AccessToken?.AccessToken, Is.EqualTo("new-access-token"));
            Assert.That(result.Data?.AccessToken?.RefreshToken, Is.EqualTo("new-refresh-token"));
            Assert.That(result.Message, Is.EqualTo(ResultMessage.AccessTokenGenerated));
        });
    }
}