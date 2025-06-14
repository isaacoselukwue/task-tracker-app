namespace TaskTracker.Tests.Accounts.Commands;

class DeactivateAccountTests
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
    public async Task DeactivateAccount_InvalidRequest_Fails()
    {
        DeactivateAccountCommand command = new();
        _mockIdentityService.Setup(x => x.DeactivateAccountAsync())
            .ReturnsAsync((Result.Failure(ResultMessage.DeactivateAccountFailed, ["Invalid user"]), "test@example.com"));
        DeactivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(command, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task DeactivateAccount_ValidRequest_Succeeds()
    {
        DeactivateAccountCommand command = new();
        _mockIdentityService.Setup(x => x.DeactivateAccountAsync())
            .ReturnsAsync((Result.Success(ResultMessage.DeactivateAccountSuccess), "test@example.com"));
        DeactivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(command, CancellationToken.None);
        _mockPublisher.Verify(x => x.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.Succeeded, Is.True);
    }
}