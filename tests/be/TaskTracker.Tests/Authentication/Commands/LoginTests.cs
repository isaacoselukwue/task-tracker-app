namespace TaskTracker.Tests.Authentication.Commands;
[TestFixture]
class LoginTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private LoginCommand emptyEmailCommand;
    private LoginCommand invalidEmailCommand;
    private LoginCommand emptyPasswordCommand;
    private LoginCommand validCommand;
    private LoginValidator _validator;
    private LoginCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new();
        _mockIdentityService = new();
        _validator = new();

        emptyEmailCommand = new()
        {
            EmailAddress = "",
            Password = "ValidPass123!"
        };

        invalidEmailCommand = new()
        {
            EmailAddress = "not-an-email",
            Password = "ValidPass123!"
        };

        emptyPasswordCommand = new()
        {
            EmailAddress = "test@example.com",
            Password = ""
        };

        validCommand = new()
        {
            EmailAddress = "test@example.com",
            Password = "ValidPass123!"
        };

        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task Login_EmptyEmail_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyEmailCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Login_InvalidEmail_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidEmailCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Login_EmptyPassword_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyPasswordCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task Login_ValidData_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task Login_InvalidCredentials_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.SignInUserAsync(
            validCommand.EmailAddress!, validCommand.Password!))
            .ReturnsAsync(Result<LoginDto>.Failure(ResultMessage.LoginFailedGeneric, ["Invalid credentials"]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid credentials"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Login_AccountLocked_ReturnsFailed_AndPublishesNotification()
    {
        _mockIdentityService.Setup(x => x.SignInUserAsync(
            validCommand.EmailAddress!, validCommand.Password!))
            .ReturnsAsync(Result<LoginDto>.Failure(ResultMessage.LoginFailedAccountLocked, [ResultMessage.LoginFailedAccountLocked]));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item(ResultMessage.LoginFailedAccountLocked));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == validCommand.EmailAddress &&
                n.Subject == "Account Temporarily Disabled!" &&
                n.NotificationType == NotificationTypeEnum.SignInBlockedAccount),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsSuccess_AndPublishesNotification()
    {
        LoginDto loginDto = new()
        {
            AccessToken = new Microsoft.AspNetCore.Authentication.BearerToken.AccessTokenResponse { AccessToken = "valid-access-token", ExpiresIn = DateTime.UtcNow.AddDays(7).Second, RefreshToken = "valid-refresh-token" },
        };

        _mockIdentityService.Setup(x => x.SignInUserAsync(
            validCommand.EmailAddress!, validCommand.Password!))
            .ReturnsAsync(Result<LoginDto>.Success( "success", loginDto));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data?.AccessToken, Is.Not.Null);
            Assert.That(result.Data?.AccessToken?.AccessToken, Is.EqualTo("valid-access-token"));
            Assert.That(result.Message, Is.EqualTo("success"));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == validCommand.EmailAddress &&
                n.Subject == "Sign In Successful!" &&
                n.NotificationType == NotificationTypeEnum.SignInSuccess &&
                n.Replacements.Count == 2),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}