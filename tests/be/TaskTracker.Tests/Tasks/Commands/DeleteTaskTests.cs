namespace TaskTracker.Tests.Tasks.Commands;
[TestFixture]
internal class DeleteTaskTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private DeleteTaskCommand invalidCommand;
    private DeleteTaskCommand validCommand;
    private DeleteTaskValidator _validator;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new Mock<ITaskTrackerService>();
        _validator = new();

        invalidCommand = new(Guid.Empty);
        validCommand = new(Guid.NewGuid());
    }

    [Test]
    public async Task DeleteTask_InvalidCommand_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task DeleteTask_ValidCommand_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);
        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task DeleteTask_ServiceReturnsSuccess_ReturnsSuccessResult()
    {
        _mockTaskTrackerService.Setup(x => x.UpdateTaskAsync(validCommand.TaskId, StatusEnum.Deleted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("Task deleted successfully"));

        DeleteTaskCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Task deleted successfully"));
        });
    }

    [Test]
    public async Task DeleteTask_ServiceReturnsFailure_ReturnsFailureResult()
    {
        _mockTaskTrackerService.Setup(x => x.UpdateTaskAsync(validCommand.TaskId, StatusEnum.Deleted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Task could not be deleted", ["Delete failed"]));

        DeleteTaskCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Delete failed"));
        });
    }
}