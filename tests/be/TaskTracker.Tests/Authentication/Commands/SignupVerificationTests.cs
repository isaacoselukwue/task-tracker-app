namespace TaskTracker.Tests.Authentication.Commands;
[TestFixture]
class SignupVerificationTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private Mock<IPublisher> _mockPublisher;
    private SignupVerificationCommand emptyUserIdCommand;
    private SignupVerificationCommand invalidUserIdCommand;
    private SignupVerificationCommand emptyTokenCommand;
    private SignupVerificationCommand validCommand;
    private SignupVerificationValidator _validator;
    private SignupVerificationCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new();
        _mockPublisher = new();
        _validator = new();

        emptyUserIdCommand = new()
        {
            UserId = "",
            ActivationToken = "valid-token"
        };

        invalidUserIdCommand = new()
        {
            UserId = "not-a-guid",
            ActivationToken = "valid-token"
        };

        emptyTokenCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ActivationToken = ""
        };

        validCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ActivationToken = "valid-token"
        };

        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task SignupVerification_EmptyUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyUserIdCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "UserId"));
        });
    }

    [Test]
    public async Task SignupVerification_InvalidUserIdFormat_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidUserIdCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "UserId"));
        });
    }

    [Test]
    public async Task SignupVerification_EmptyToken_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyTokenCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Has.Some.Matches<FluentValidation.Results.ValidationFailure>(f => f.PropertyName == "ActivationToken"));
        });
    }

    [Test]
    public async Task SignupVerification_ValidData_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task SignupVerification_InvalidToken_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.ValidateSignupAsync(
            validCommand.UserId!,
            validCommand.ActivationToken!))
        .ReturnsAsync((Result.Failure("Verification failed", ["Invalid activation token"]), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid activation token"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SignupVerification_TokenExpired_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.ValidateSignupAsync(
            validCommand.UserId!,
            validCommand.ActivationToken!))
        .ReturnsAsync((Result.Failure("Verification failed", ["Token has expired"]), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Token has expired"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SignupVerification_ValidTokenAndUserId_ReturnsSuccess_AndPublishesNotification()
    {
        string userEmail = "test@example.com";

        _mockIdentityService.Setup(x => x.ValidateSignupAsync(
            validCommand.UserId!,
            validCommand.ActivationToken!))
        .ReturnsAsync((Result.Success("Account successfully activated"), userEmail));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Account successfully activated"));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == userEmail &&
                n.Subject == "Account Activation Succeeded!" &&
                n.NotificationType == NotificationTypeEnum.SignUpCompleted),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}