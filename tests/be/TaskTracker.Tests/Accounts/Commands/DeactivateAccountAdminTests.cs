namespace TaskTracker.Tests.Accounts.Commands;

[TestFixture]
public class DeactivateAccountAdminTests
{
    private Mock<IIdentityService> _mockIdentityService;
    private Mock<IPublisher> _mockPublisher;

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new();
        _mockPublisher = new();
    }

    [Test]
    public async Task DeactivateAccountAdmin_UserNotFound_Fails()
    {
        Guid userId = Guid.NewGuid();
        DeactivateAccountAdminCommand command = new() { UserId = userId };

        _mockIdentityService.Setup(x => x.DeactivateAccountAsync(userId))
            .ReturnsAsync((Result.Failure(ResultMessage.DeactivateAccountFailed, ["Invalid user"]), string.Empty));

        DeactivateAccountAdminCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
        _mockPublisher.Verify(x => x.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeactivateAccountAdmin_AccountNotActive_Fails()
    {
        Guid userId = Guid.NewGuid();
        DeactivateAccountAdminCommand command = new() { UserId = userId };

        _mockIdentityService.Setup(x => x.DeactivateAccountAsync(userId))
            .ReturnsAsync((Result.Failure(ResultMessage.DeactivateAccountFailed, ["Account is not active"]), "test@example.com"));

        DeactivateAccountAdminCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Account is not active"));
        });

        _mockPublisher.Verify(x => x.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task DeactivateAccountAdmin_ValidRequest_Succeeds()
    {
        Guid userId = Guid.NewGuid();
        var userEmail = "test@example.com";
        DeactivateAccountAdminCommand command = new() { UserId = userId };

        _mockIdentityService.Setup(x => x.DeactivateAccountAsync(userId))
            .ReturnsAsync((Result.Success(ResultMessage.DeactivateAccountSuccess), userEmail));

        DeactivateAccountAdminCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo(ResultMessage.DeactivateAccountSuccess));
        });

        _mockPublisher.Verify(x => x.Publish(
            It.Is<NotificationEvent>(n =>
                n.Receiver == userEmail &&
                n.Subject == "Sorry To See You Go!" &&
                n.NotificationType == NotificationTypeEnum.DeactivateAccountSuccess
            ),
            It.IsAny<CancellationToken>()
        ), Times.Once);
    }

    [Test]
    public async Task DeactivateAccountAdmin_ValidationFails_WhenUserIdEmpty()
    {
        DeactivateAccountAdminCommand command = new() { UserId = Guid.Empty };
        var validator = new DeactivateAccountAdminValidator();

        var result = await validator.ValidateAsync(command);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Count.GreaterThan(0));
        });
    }
}