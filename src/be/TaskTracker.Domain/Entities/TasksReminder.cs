namespace TaskTracker.Domain.Entities;
public class TasksReminder : BaseAuditableEntity
{
    public Guid TaskId { get; set; }
    public Tasks? Task { get; set; }
    public TimeSpan OffsetFromTaskTime { get; set; }
    public bool Sent { get; set; } = false;
    public DateTime? SentAt { get; set; }
}
