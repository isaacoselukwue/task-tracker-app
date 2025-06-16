namespace TaskTracker.Tests.Accounts.Commands;
[TestFixture]
public class PasswordResetTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private PasswordResetCommand invalidEmptyUserIdCommand;
    private PasswordResetCommand invalidGuidUserIdCommand;
    private PasswordResetCommand emptyTokenCommand;
    private PasswordResetCommand emptyNewPasswordCommand;
    private PasswordResetCommand passwordMismatchCommand;
    private PasswordResetCommand validCommand;
    private PasswordResetValidator _validator;
    private PasswordResetCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new();
        _mockIdentityService = new();
        _validator = new();

        invalidEmptyUserIdCommand = new()
        {
            UserId = "",
            ResetToken = "valid-token",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        invalidGuidUserIdCommand = new()
        {
            UserId = "not-a-guid",
            ResetToken = "valid-token",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        emptyTokenCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ResetToken = "",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        emptyNewPasswordCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ResetToken = "valid-token",
            NewPassword = "",
            ConfirmPassword = ""
        };

        passwordMismatchCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ResetToken = "valid-token",
            NewPassword = "NewPass123!",
            ConfirmPassword = "DifferentPass456!"
        };

        validCommand = new()
        {
            UserId = Guid.NewGuid().ToString(),
            ResetToken = "valid-token",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        };

        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task PasswordReset_EmptyUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidEmptyUserIdCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task PasswordReset_InvalidGuidUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidGuidUserIdCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task PasswordReset_EmptyToken_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyTokenCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task PasswordReset_EmptyNewPassword_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(emptyNewPasswordCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task PasswordReset_PasswordMismatch_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(passwordMismatchCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task PasswordReset_ValidData_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task PasswordReset_UserNotFound_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.ResetPasswordAsync(
            validCommand.NewPassword!, validCommand.UserId!, validCommand.ResetToken!))
            .ReturnsAsync((Result.Failure(ResultMessage.ResetPasswordFailed, ["Invalid user"]), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PasswordReset_InvalidToken_ReturnsFailed()
    {
        _mockIdentityService.Setup(x => x.ResetPasswordAsync(
            validCommand.NewPassword!, validCommand.UserId!, validCommand.ResetToken!))
            .ReturnsAsync((Result.Failure(ResultMessage.ResetPasswordFailed, ["Invalid token"]), string.Empty));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Invalid token"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task PasswordReset_ValidResetRequest_ReturnsSuccessAndPublishesNotification()
    {
        string email = "test@example.com";

        _mockIdentityService.Setup(x => x.ResetPasswordAsync(
            validCommand.NewPassword!, validCommand.UserId!, validCommand.ResetToken!))
            .ReturnsAsync((Result.Success(ResultMessage.ResetPasswordSuccess), email));

        var result = await _handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.ResetPasswordSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == email &&
                n.Subject == "Password Reset Successful" &&
                n.NotificationType == NotificationTypeEnum.PasswordResetSuccess),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}