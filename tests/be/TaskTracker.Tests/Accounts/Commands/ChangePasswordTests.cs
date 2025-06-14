namespace TaskTracker.Tests.Accounts.Commands;
[TestFixture]
class ChangePasswordTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private ChangePasswordCommand invalidCommand;
    private ChangePasswordCommand validCommand;
    private ChangePasswordValidator _validator;
    [SetUp]
    public void Setup()
    {
        _mockPublisher = new Mock<IPublisher>();
        _mockIdentityService = new Mock<IIdentityService>();
        _validator = new();
        invalidCommand = new ChangePasswordCommand { NewPassword = string.Empty };
        validCommand = new ChangePasswordCommand { NewPassword = "IAmAValidPassword123@", ConfirmNewPassword = "IAmAValidPassword123@" };
    }

    [Test]
    public async Task ChangePassword_InvalidPassword_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ChangePassword_PasswordMismatch_FailsPasswordChange()
    {
        _mockIdentityService.Setup(x => x.ChangePasswordAsync(validCommand.NewPassword!))
            .ReturnsAsync((Result.Failure(ResultMessage.ChangePasswordFailed, ["Passwords do not match"]), string.Empty));
        ChangePasswordCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Passwords do not match"));
        });
    }

    [Test]
    public async Task ChangePassword_NonExistentUserId_FailsAccountActivation()
    {
        Users users = new();
        _mockIdentityService.Setup(x => x.ChangePasswordAsync(validCommand.NewPassword!))
            .ReturnsAsync((Result.Failure(ResultMessage.ActivateAccountFailed, ["Invalid user"]), string.Empty));
        ChangePasswordCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task ChangePassword_InActiveUser_FailsAccountActivation()
    {
        Users users = new()
        {
            Id = Guid.NewGuid(),
            UsersStatus = Domain.Enums.StatusEnum.InActive,
        };
        _mockIdentityService.Setup(x => x.ChangePasswordAsync(validCommand.NewPassword!))
            .ReturnsAsync((Result.Failure(ResultMessage.ActivateAccountFailed, ["Account is not active"]), string.Empty));
        ChangePasswordCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Account is not active"));
        });
    }

    [Test]
    public async Task ChangePassword_SuccessfulPasswordChange_ReturnsSuccess()
    {
        _mockIdentityService.Setup(x => x.ChangePasswordAsync(validCommand.NewPassword!))
            .ReturnsAsync((Result.Success(ResultMessage.ChangePasswordSuccess), string.Empty));
        ChangePasswordCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Errors, Is.Empty);
        });
    }
}
