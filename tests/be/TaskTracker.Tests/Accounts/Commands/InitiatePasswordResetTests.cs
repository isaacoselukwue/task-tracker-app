namespace TaskTracker.Tests.Accounts.Commands;

[TestFixture]
class InitiatePasswordResetTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private InitiatePasswordResetCommand invalidEmptyCommand;
    private InitiatePasswordResetCommand invalidFormatCommand;
    private InitiatePasswordResetCommand validCommand;
    private InitiatePasswordResetValidator _validator;
    private InitiatePasswordResetCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new();
        _mockIdentityService = new();
        _validator = new();
        invalidEmptyCommand = new InitiatePasswordResetCommand { EmailAddress = "" };
        invalidFormatCommand = new InitiatePasswordResetCommand { EmailAddress = "invalid-email" };
        validCommand = new InitiatePasswordResetCommand { EmailAddress = "test@example.com" };
        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task InitiatePasswordReset_EmptyEmail_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidEmptyCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task InitiatePasswordReset_InvalidEmailFormat_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidFormatCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task InitiatePasswordReset_ValidEmail_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task InitiatePasswordReset_UserNotFound_ReturnsSuccessButNoNotification()
    {
        _mockIdentityService.Setup(x => x.InitiateForgotPasswordAsync(validCommand.EmailAddress!))
            .ReturnsAsync((Result.Success(ResultMessage.ForgotPasswordSuccess), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.ForgotPasswordSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task InitiatePasswordReset_InactiveUser_ReturnsSuccessButNoNotification()
    {
        _mockIdentityService.Setup(x => x.InitiateForgotPasswordAsync(validCommand.EmailAddress!))
            .ReturnsAsync((Result.Success(ResultMessage.ForgotPasswordSuccess), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.ForgotPasswordSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task InitiatePasswordReset_ValidActiveUser_ReturnsSuccessAndPublishesNotification()
    {
        string token = "reset-token-123";
        string userId = Guid.NewGuid().ToString();

        _mockIdentityService.Setup(x => x.InitiateForgotPasswordAsync(validCommand.EmailAddress!))
            .ReturnsAsync((Result.Success(userId), token));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.ForgotPasswordSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == validCommand.EmailAddress &&
                n.Subject == "Password Reset Request" &&
                n.NotificationType == NotificationTypeEnum.PasswordResetInitiation &&
                n.Replacements.Count == 2 &&
                n.Replacements["{{token}}"] == token &&
                n.Replacements["{{userid}}"] == userId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}