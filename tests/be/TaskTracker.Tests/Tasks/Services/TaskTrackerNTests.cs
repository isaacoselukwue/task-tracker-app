global using TaskTracker.Infrastructure.Tasks;
global using TaskTracker.Infrastructure.Data;

namespace TaskTracker.Tests.Tasks.Services;
[TestFixture]
internal class TaskTrackerNTests
{
    private TaskDbContext _taskDbContext;
    private TaskTrackerNService _service;
    private Guid _pendingReminderId;
    private Guid _sentReminderId;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        _taskDbContext = new(options);

        Users user = new() { Id = Guid.NewGuid(), Email = "user@example.com" };
        Domain.Entities.Tasks task = new()
        {
            Id = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Desc",
            ScheduledFor = DateTimeOffset.UtcNow.AddHours(-1),
            Status = StatusEnum.Active,
            User = user
        };

        _pendingReminderId = Guid.NewGuid();
        _sentReminderId = Guid.NewGuid();

        _taskDbContext.Users.Add(user);
        _taskDbContext.Tasks.Add(task);
        _taskDbContext.TaskReminders.AddRange(
            new()
            {
                Id = _pendingReminderId,
                TaskId = task.Id,
                Task = task,
                Sent = false,
                OffsetFromTaskTime = TimeSpan.Zero
            },
            new()
            {
                Id = _sentReminderId,
                TaskId = task.Id,
                Task = task,
                Sent = true,
                OffsetFromTaskTime = TimeSpan.Zero
            }
        );
        _taskDbContext.SaveChanges();

        _service = new(_taskDbContext);
    }

    [TearDown]
    public void TearDown()
    {
        _taskDbContext.Dispose();
    }

    [Test]
    public async Task GetPendingRemindersAsync_ReturnsOnlyPendingReminders()
    {
        var result = await _service.GetPendingRemindersAsync(default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Count, Is.EqualTo(1));
            var reminder = result.First();
            Assert.That(reminder.Id, Is.EqualTo(_pendingReminderId));
            Assert.That(reminder.TaskName, Is.EqualTo("Test Task"));
            Assert.That(reminder.UsersEmail, Is.EqualTo("user@example.com"));
        });
    }

    //[Test]
    //public async Task MarkReminderAsSentAsync_UpdatesSentFlag()
    //{
    //    await _service.MarkReminderAsSentAsync(_pendingReminderId, default);

    //    var updated = await _dbContext.TaskReminders.FindAsync(_pendingReminderId);
    //    Assert.That(updated.Sent, Is.True);
    //}
}