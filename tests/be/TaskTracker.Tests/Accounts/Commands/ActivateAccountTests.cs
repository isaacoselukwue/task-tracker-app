namespace TaskTracker.Tests.Accounts.Commands;
[TestFixture]
public class ActivateAccountTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private ActivateAccountCommand invalidCommand;
    private ActivateAccountCommand validCommand;
    private ActivateAccountValidator _validator;

    [SetUp]
    public void Setup()
    {
        _mockPublisher = new Mock<IPublisher>();
        _mockIdentityService = new Mock<IIdentityService>();
        _validator = new();
        invalidCommand = new ActivateAccountCommand { UserId = Guid.Empty };
        validCommand = new ActivateAccountCommand { UserId = Guid.NewGuid() };
    }

    [Test]
    public async Task ActivateAccount_InvalidUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.EqualTo(false));
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }
    [Test]
    public async Task ActivateAccount_NonExistentUserId_FailsAccountActivation()
    {
        Users users = new();
        _mockIdentityService.Setup(x => x.ActivateAccountAsync(validCommand.UserId))
            .ReturnsAsync((Result.Failure(ResultMessage.ActivateAccountFailed, ["Invalid user"]), string.Empty));
        ActivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }

    [Test]
    public async Task ActivateAccount_AlreadyActiveUser_ReturnsUserActive()
    {
        Users users = new()
        {
            Id = validCommand.UserId,
            UsersStatus = Domain.Enums.StatusEnum.Active,
        };
        _mockIdentityService.Setup(x => x.ActivateAccountAsync(validCommand.UserId))
            .ReturnsAsync((Result.Failure(ResultMessage.ActivateAccountFailed, ["Account is already active"]), string.Empty));
        ActivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Account is already active"));
        });
    }

    [Test]
    public async Task ActivateAccount_AccountActivationFails_ReturnsFailedAccount()
    {
        Users users = new()
        {
            Id = validCommand.UserId,
            UsersStatus = Domain.Enums.StatusEnum.Pending,
        };
        _mockIdentityService.Setup(x => x.ActivateAccountAsync(validCommand.UserId))
            .ReturnsAsync((Result.Failure(ResultMessage.ActivateAccountFailed, ["Account update error"]), string.Empty));
        ActivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Account update error"));
        });
    }

    [Test]
    public async Task ActivateAccount_ValidUserId_PublishesUserActivatedEvent()
    {
        Users users = new()
        {
            Id = validCommand.UserId,
            Email = "",
            UsersStatus = Domain.Enums.StatusEnum.Active,
        };
        _mockIdentityService.Setup(x => x.ActivateAccountAsync(validCommand.UserId))
            .ReturnsAsync((Result.Success(ResultMessage.ActivateAccountSuccess), users.Email));
        ActivateAccountCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        _mockPublisher.Verify(p => p.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.Succeeded, Is.True);
    }
}
