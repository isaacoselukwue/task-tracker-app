namespace TaskTracker.Tests.Accounts.Commands;

[TestFixture]
public class DeleteAccountTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private DeleteAccountCommand invalidCommand;
    private DeleteAccountCommand validSoftDeleteCommand;
    private DeleteAccountCommand validPermanentDeleteCommand;
    private DeleteAccountValidator _validator;
    private DeleteAccountCommandHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new Mock<IPublisher>();
        _mockIdentityService = new Mock<IIdentityService>();
        _validator = new();
        invalidCommand = new DeleteAccountCommand { UserId = Guid.Empty, IsPermanant = false };
        validSoftDeleteCommand = new DeleteAccountCommand { UserId = Guid.NewGuid(), IsPermanant = false };
        validPermanentDeleteCommand = new DeleteAccountCommand { UserId = Guid.NewGuid(), IsPermanant = true };
        _handler = new(_mockIdentityService.Object, _mockPublisher.Object);
    }

    [Test]
    public async Task DeleteAccount_EmptyUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task DeleteAccount_ValidUserId_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validSoftDeleteCommand);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task DeleteAccount_NonExistentUserId_FailsAccountDeletion()
    {
        _mockIdentityService.Setup(x => x.DeleteUserAsync(validSoftDeleteCommand.UserId.ToString(), false))
            .ReturnsAsync((Result.Failure(ResultMessage.DeleteAccountFailed, ["Invalid user"]), string.Empty));

        var result = await _handler.Handle(validSoftDeleteCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAccount_AlreadySoftDeletedUser_FailsAccountDeletion()
    {
        _mockIdentityService.Setup(x => x.DeleteUserAsync(validSoftDeleteCommand.UserId.ToString(), false))
            .ReturnsAsync((Result.Failure(ResultMessage.DeleteAccountFailed, ["Account is already on soft delete"]), string.Empty));

        var result = await _handler.Handle(validSoftDeleteCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Account is already on soft delete"));
        });

        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeleteAccount_SuccessfulSoftDelete_ReturnsSuccess()
    {
        string email = "test@example.com";
        _mockIdentityService.Setup(x => x.DeleteUserAsync(validSoftDeleteCommand.UserId.ToString(), false))
            .ReturnsAsync((Result.Success(ResultMessage.DeleteAccountSuccess), email));

        var result = await _handler.Handle(validSoftDeleteCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.DeleteAccountSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == email &&
                n.Subject == "Sorry to see you go!" &&
                n.NotificationType == NotificationTypeEnum.DeleteAccountSuccess),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task DeleteAccount_SuccessfulPermanentDelete_ReturnsSuccess()
    {
        string email = "test@example.com";
        _mockIdentityService.Setup(x => x.DeleteUserAsync(validPermanentDeleteCommand.UserId.ToString(), true))
            .ReturnsAsync((Result.Success(ResultMessage.DeleteAccountSuccess), email));

        var result = await _handler.Handle(validPermanentDeleteCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.DeleteAccountSuccess));
        });

        _mockPublisher.Verify(p => p.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == email &&
                n.Subject == "Sorry to see you go!" &&
                n.NotificationType == NotificationTypeEnum.DeleteAccountSuccess),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}