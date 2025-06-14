namespace TaskTracker.Tests.Tasks.Commands;
[TestFixture]
internal class MarkTaskAsDoneTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private MarkTaskAsDoneCommand invalidCommand;
    private MarkTaskAsDoneCommand validCommand;
    private MarkTaskAsDoneValidator _validator;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new Mock<ITaskTrackerService>();
        _validator = new();

        invalidCommand = new(Guid.Empty);
        validCommand = new(Guid.NewGuid());
    }

    [Test]
    public async Task MarkTaskAsDone_InvalidCommand_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task MarkTaskAsDone_ValidCommand_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);
        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task MarkTaskAsDone_ServiceReturnsSuccess_ReturnsSuccessResult()
    {
        _mockTaskTrackerService.Setup(x => x.MarkTaskAsDoneAsync(validCommand.TaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("Task marked as completed"));

        MarkTaskAsDoneCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Task marked as completed"));
        });
    }

    [Test]
    public async Task MarkTaskAsDone_ServiceReturnsFailure_ReturnsFailureResult()
    {
        _mockTaskTrackerService.Setup(x => x.MarkTaskAsDoneAsync(validCommand.TaskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Task could not be marked as completed", ["Please try later"]));

        MarkTaskAsDoneCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Please try later"));
        });
    }
}