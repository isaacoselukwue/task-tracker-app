namespace TaskTracker.Tests.Accounts.Commands;

[TestFixture]
public class ChangeUserRoleTests
{
    private Mock<IPublisher> _mockPublisher;
    private Mock<IIdentityService> _mockIdentityService;
    private ChangeUserRoleValidator _validator;
    private ChangeUserRoleCommand invalidCommand;
    private ChangeUserRoleCommand validCommand;
    [SetUp]
    public void Setup()
    {
        _validator = new();
        _mockPublisher = new();
        _mockIdentityService = new();

        invalidCommand = new ChangeUserRoleCommand { UserId = Guid.Empty, Role = string.Empty };

        validCommand = new ChangeUserRoleCommand { UserId = Guid.NewGuid(), Role = Roles.Admin };
    }

    [Test]
    public async Task ChangeUserRole_InvalidUserId_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ChangeUserRole_InvalidRole_FailsValidation()
    {
        invalidCommand.Role = "InvalidRole";
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
            Assert.That(validationResult.Errors.Select(x=>x.ErrorMessage), Contains.Item("Supported roles are Admin and User only"));
        });
    }

    [Test]
    public async Task ChangeUserRole_ValidRequest_Succeeds()
    {
        _mockIdentityService.Setup(x => x.ChangeUserRoleAsync(validCommand.UserId.ToString(), validCommand.Role!))
            .ReturnsAsync((Result.Success(ResultMessage.ChangeUserRoleSuccess), "test@example.com"));
        ChangeUserRoleCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        _mockPublisher.Verify(x => x.Publish(It.IsAny<NotificationEvent>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result.Succeeded, Is.True);
    }

    [Test]
    public async Task ChangeUserRole_InValidUser_Fails()
    {
        _mockIdentityService.Setup(x => x.ChangeUserRoleAsync(validCommand.UserId.ToString(), validCommand.Role!))
            .ReturnsAsync((Result.Failure(ResultMessage.ChangeUserRoleFailed, ["Invalid user"]), string.Empty));
        ChangeUserRoleCommandHandler handler = new(_mockIdentityService.Object, _mockPublisher.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Is.Not.Empty);
            Assert.That(result.Errors, Contains.Item("Invalid user"));
        });
    }
}
