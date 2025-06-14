namespace TaskTracker.Tests.Tasks.Commands;
[TestFixture]
internal class CreateTaskTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private CreateTaskCommand invalidCommand;
    private CreateTaskCommand validCommand;
    private CreateTaskValidator _validator;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new Mock<ITaskTrackerService>();
        _validator = new();

        invalidCommand = new()
        {
            Title = "",
            ScheduledFor = default,
            ReminderOffsets = [(ReminderOffsetEnum)99]
        };

        validCommand = new()
        {
            Title = "Test Task",
            Description = "Test Description",
            ScheduledFor = DateTimeOffset.UtcNow.AddDays(1),
            ReminderOffsets = [ReminderOffsetEnum.AtTime]
        };
    }

    [Test]
    public async Task CreateTask_InvalidCommand_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidCommand);
        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task CreateTask_ValidCommand_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validCommand);
        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task CreateTask_ServiceReturnsSuccess_ReturnsSuccessResult()
    {
        _mockTaskTrackerService.Setup(x => x.CreateTaskAsync(validCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success("Task successfully created"));

        CreateTasksCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Message, Is.EqualTo("Task successfully created"));
        });
    }

    [Test]
    public async Task CreateTask_ServiceReturnsFailure_ReturnsFailureResult()
    {
        _mockTaskTrackerService.Setup(x => x.CreateTaskAsync(validCommand, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("We could not create task at this time", ["Tasks not saved"]));

        CreateTasksCommandHandler handler = new(_mockTaskTrackerService.Object);
        var result = await handler.Handle(validCommand, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.Errors, Contains.Item("Tasks not saved"));
        });
    }
}