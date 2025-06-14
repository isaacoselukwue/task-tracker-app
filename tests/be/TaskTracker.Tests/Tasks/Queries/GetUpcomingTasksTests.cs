namespace TaskTracker.Tests.Tasks.Queries;
[TestFixture]
internal class GetUpcomingTasksTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private GetUpcomingTasksQuery invalidZeroPageNumberQuery;
    private GetUpcomingTasksQuery invalidZeroPageCountQuery;
    private GetUpcomingTasksQuery validQuery;
    private GetUpcomingTasksValidator _validator;
    private GetUpcomingTasksQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new();
        _validator = new();

        invalidZeroPageNumberQuery = new(10, 0);
        invalidZeroPageCountQuery = new(0, 1);
        validQuery = new(10, 1);

        _handler = new(_mockTaskTrackerService.Object);
    }

    [Test]
    public async Task GetUpcomingTasks_ZeroPageNumber_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageNumberQuery);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetUpcomingTasks_ZeroPageCount_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageCountQuery);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task GetUpcomingTasks_ValidParams_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validQuery);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task GetUpcomingTasks_EmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<UpcomingTasksResult>().AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasks(StatusEnum.Active)).Returns(emptyList);

        var result = await _handler.Handle(validQuery, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Is.Empty);
            Assert.That(result.Data?.TotalResults, Is.EqualTo(0));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task GetUpcomingTasks_SinglePage_ReturnsCorrectPagination()
    {
        var testData = GetTestTasks(5).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasks(StatusEnum.Active)).Returns(testData);

        validQuery = new(10, 1);

        var result = await _handler.Handle(validQuery, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(5));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(5));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(1));
            Assert.That(result.Data?.Page, Is.EqualTo(1));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
        });
    }

    [Test]
    public async Task GetUpcomingTasks_MultiplePages_ReturnsCorrectPage()
    {
        var testData = GetTestTasks(25).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasks(StatusEnum.Active)).Returns(testData);

        validQuery = new(10, 2);

        var result = await _handler.Handle(validQuery, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(10));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(25));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(3));
            Assert.That(result.Data?.Page, Is.EqualTo(2));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
            Assert.That(result.Data?.Results.First().Title, Is.EqualTo("Task10"));
            Assert.That(result.Data?.Results.Last().Title, Is.EqualTo("Task19"));
        });
    }

    [Test]
    public async Task GetUpcomingTasks_LastPage_ReturnsRemainingItems()
    {
        var testData = GetTestTasks(25).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasks(StatusEnum.Active)).Returns(testData);

        validQuery = new(10, 3);

        var result = await _handler.Handle(validQuery, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.Results, Has.Count.EqualTo(5));
            Assert.That(result.Data?.TotalResults, Is.EqualTo(25));
            Assert.That(result.Data?.TotalPages, Is.EqualTo(3));
            Assert.That(result.Data?.Page, Is.EqualTo(3));
            Assert.That(result.Data?.Size, Is.EqualTo(10));
            Assert.That(result.Data?.Results.First().Title, Is.EqualTo("Task20"));
            Assert.That(result.Data?.Results.Last().Title, Is.EqualTo("Task24"));
        });
    }

    private static List<UpcomingTasksResult> GetTestTasks(int count)
    {
        List<UpcomingTasksResult> results = [];
        for (int i = 0; i < count; i++)
        {
            results.Add(new()
            {
                Id = Guid.NewGuid(),
                Title = $"Task{i}",
                Description = $"Description for Task{i}",
                ScheduledFor = DateTimeOffset.UtcNow.AddDays(-count + i),
                Status = StatusEnum.Active,
                UserId = Guid.NewGuid()
            });
        }
        return results;
    }
}