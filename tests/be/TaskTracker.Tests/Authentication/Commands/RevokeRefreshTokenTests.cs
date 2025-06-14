namespace TaskTracker.Tests.Authentication.Commands;
[TestFixture]
class RevokeRefreshTokenTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private RevokeRefreshTokenCommand emptyTokenCommand;
    private RevokeRefreshTokenCommand validCommand;
    private RevokeRefreshTokenValidator _validator;
    private RevokeRefreshTokenCommandHandler _handler;

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
    public async Task RevokeRefreshToken_EmptyToken_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyTokenCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task RevokeRefreshToken_ValidToken_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task RevokeRefreshToken_InvalidToken_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.RevokeRefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result.Failure(ResultMessage.TokenRefreshFailed, ["Invalid token"]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid token"));
            Assert.That(result.Message, Is.EqualTo(ResultMessage.TokenRefreshFailed));
        });
    }

    [Test]
    public async Task RevokeRefreshToken_UserNotFound_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.RevokeRefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result.Failure(ResultMessage.TokenRefreshFailed, ["Invalid user"]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task RevokeRefreshToken_ValidToken_ReturnsSuccess()
    {
        _mockIdentityService.Setup(x => x.RevokeRefreshUserTokenAsync(validCommand.EncryptedToken!))
            .ReturnsAsync(Result.Success("Refresh token successfully revoked"));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Refresh token successfully revoked"));
        });
    }
}