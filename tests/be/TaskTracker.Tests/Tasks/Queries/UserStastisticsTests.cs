namespace TaskTracker.Tests.Tasks.Queries;
[TestFixture]
internal class UserStastisticsTests
{
    private Mock<ITaskTrackerService> _mockTaskTrackerService;
    private UserStastisticsQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockTaskTrackerService = new();
        _handler = new(_mockTaskTrackerService.Object);
    }

    [Test]
    public async Task UserStastistics_ReturnsCorrectData()
    {
        UserStastisticsDto expected = new()
        {
            UsersCount = 100,
            ActiveUsersCount = 80,
            DeactivatedUsersCount = 10,
            DeletedUsersCount = 5,
            PendingUsersCount = 5
        };

        _mockTaskTrackerService.Setup(x => x.GetUserStastisticsAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        UserStastisticsQuery query = new() { RefreshData = false };
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data?.UsersCount, Is.EqualTo(100));
            Assert.That(result.Data?.ActiveUsersCount, Is.EqualTo(80));
            Assert.That(result.Data?.DeactivatedUsersCount, Is.EqualTo(10));
            Assert.That(result.Data?.DeletedUsersCount, Is.EqualTo(5));
            Assert.That(result.Data?.PendingUsersCount, Is.EqualTo(5));
        });
    }

    [Test]
    public async Task UserStastistics_RefreshTrue_CallsServiceWithRefresh()
    {
        UserStastisticsDto expected = new()
        {
            UsersCount = 50,
            ActiveUsersCount = 40,
            DeactivatedUsersCount = 5,
            DeletedUsersCount = 3,
            PendingUsersCount = 2
        };

        _mockTaskTrackerService.Setup(x => x.GetUserStastisticsAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(expected);

        UserStastisticsQuery query = new()  { RefreshData = true };
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Data?.UsersCount, Is.EqualTo(50));
            Assert.That(result.Data?.ActiveUsersCount, Is.EqualTo(40));
            Assert.That(result.Data?.DeactivatedUsersCount, Is.EqualTo(5));
            Assert.That(result.Data?.DeletedUsersCount, Is.EqualTo(3));
            Assert.That(result.Data?.PendingUsersCount, Is.EqualTo(2));
        });
    }
}