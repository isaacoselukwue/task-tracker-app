namespace TaskTracker.Tests.Tasks.Queries;
[TestFixture]
internal class AdminTasksTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private AdminTasksQuery invalidZeroPageNumberQuery;
    private AdminTasksQuery invalidZeroPageCountQuery;
    private AdminTasksQuery validQuery;
    private AdminTasksValidator _validator;
    private AdminTasksQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new();
        _validator = new();

        invalidZeroPageNumberQuery = new() { PageNumber = 0, PageCount = 10 };
        invalidZeroPageCountQuery = new() { PageNumber = 1, PageCount = 0 };
        validQuery = new() { PageNumber = 1, PageCount = 10 };

        _handler = new(_mockTaskTrackerService.Object);
    }

    [Test]
    public async Task AdminTasks_ZeroPageNumber_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageNumberQuery);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task AdminTasks_ZeroPageCount_FailsValidation()
    {
        var validationResult = await _validator.ValidateAsync(invalidZeroPageCountQuery);

        Assert.Multiple(() =>
        {
            Assert.That(validationResult.IsValid, Is.False);
            Assert.That(validationResult.Errors, Is.Not.Empty);
        });
    }

    [Test]
    public async Task AdminTasks_ValidParams_PassesValidation()
    {
        var validationResult = await _validator.ValidateAsync(validQuery);

        Assert.That(validationResult.IsValid, Is.True);
    }

    [Test]
    public async Task AdminTasks_EmptyResults_ReturnsEmptyList()
    {
        var emptyList = new List<UpcomingTasksResult>().AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasksNoId()).Returns(emptyList);

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
    public async Task AdminTasks_SinglePage_ReturnsCorrectPagination()
    {
        var testData = GetTestTasks(5).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasksNoId()).Returns(testData);

        validQuery.PageNumber = 1;
        validQuery.PageCount = 10;

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
    public async Task AdminTasks_MultiplePages_ReturnsCorrectPage()
    {
        var testData = GetTestTasks(25).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasksNoId()).Returns(testData);

        validQuery.PageNumber = 2;
        validQuery.PageCount = 10;

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
    public async Task AdminTasks_LastPage_ReturnsRemainingItems()
    {
        var testData = GetTestTasks(25).AsAsyncQueryable();

        _mockTaskTrackerService.Setup(x => x.GetTasksNoId()).Returns(testData);

        validQuery.PageNumber = 3;
        validQuery.PageCount = 10;

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
            results.Add(new UpcomingTasksResult
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