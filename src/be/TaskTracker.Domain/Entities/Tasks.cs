namespace TaskTracker.Domain.Entities;
public class Tasks : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Users? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StatusEnum Status { get; set; } = StatusEnum.Active;
    public ICollection<TasksReminder>? Reminders { get; set; }
}
