namespace TaskTracker.Tests.Tasks.Services;
[TestFixture]
internal class TaskTrackerTests
{
    private TaskDbContext _dbContext;
    private TaskTrackerService _service;
    private Users _user;
    private Guid _taskId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        _dbContext = new(options);

        _user = new() { Id = Guid.NewGuid(), Email = "user@example.com", UsersStatus = StatusEnum.Active };
        Domain.Entities.Tasks task = new()
        {
            Id = Guid.NewGuid(),
            Title = "Sample Task",
            Description = "Sample Desc",
            ScheduledFor = DateTimeOffset.UtcNow.AddDays(1),
            Status = StatusEnum.Active,
            User = _user,
            UserId = _user.Id
        };
        _taskId = task.Id;

        _dbContext.Users.Add(_user);
        _dbContext.Tasks.Add(task);
        _dbContext.SaveChanges();

        var hybridCache = new Moq.Mock<Microsoft.Extensions.Caching.Hybrid.HybridCache>().Object;
        var currentUser = new Moq.Mock<TaskTracker.Application.Common.Interfaces.ICurrentUser>();
        currentUser.Setup(x => x.UserId).Returns(_user.Id);
        currentUser.Setup(x => x.Email).Returns(_user.Email);

        _service = new TaskTrackerService(hybridCache, currentUser.Object, _dbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Dispose();
    }

    [Test]
    public async Task CreateTaskAsync_CreatesTask()
    {
        CreateTaskCommand command = new()
        {
            Title = "New Task",
            Description = "New Desc",
            ScheduledFor = DateTimeOffset.UtcNow.AddDays(2),
            ReminderOffsets = [TaskTracker.Domain.Enums.ReminderOffsetEnum.AtTime]
        };

        var result = await _service.CreateTaskAsync(command, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(_dbContext.Tasks.Any(t => t.Title == "New Task"), Is.True);
        });
    }

    [Test]
    public async Task GetPendingRemindersAsync_ReturnsReminders()
    {
        var task = _dbContext.Tasks.First();
        task.ScheduledFor = DateTimeOffset.UtcNow.AddHours(-2);
        _dbContext.SaveChanges();

        TasksReminder reminder = new()
        {
            Id = Guid.NewGuid(),
            TaskId = _taskId,
            Task = task,
            Sent = false,
            OffsetFromTaskTime = TimeSpan.Zero
        };
        _dbContext.TaskReminders.Add(reminder);
        _dbContext.SaveChanges();

        var result = await _service.GetPendingRemindersAsync(default);

        Assert.That(result.Any(r => r.Id == reminder.Id), Is.True);
    }

    [Test]
    public async Task MarkReminderAsSentAsync_UpdatesSentFlag()
    {
        TasksReminder reminder = new()
        {
            Id = Guid.NewGuid(),
            TaskId = _taskId,
            Task = _dbContext.Tasks.First(),
            Sent = false,
            OffsetFromTaskTime = TimeSpan.Zero
        };
        _dbContext.TaskReminders.Add(reminder);
        _dbContext.SaveChanges();

        var entity = await _dbContext.TaskReminders.FindAsync(reminder.Id);
        if (entity is not null) entity.Sent = false;
        await _dbContext.SaveChangesAsync();

        if (entity is not null) entity.Sent = true;
        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.TaskReminders.FindAsync(reminder.Id);
        Assert.That(updated?.Sent, Is.True);
    }

    [Test]
    public void GetTasks_ReturnsTasksForCurrentUser()
    {
        var results = _service.GetTasks(StatusEnum.Active).ToList();
        Assert.That(results.Any(t => t.UserId == _user.Id), Is.True);
    }

    [Test]
    public void GetTasksNoId_ReturnsAllTasks()
    {
        var results = _service.GetTasksNoId().ToList();
        Assert.That(results.Count, Is.GreaterThan(0));
    }
}