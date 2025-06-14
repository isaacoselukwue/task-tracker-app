namespace TaskTracker.Application.TaskReminders;
internal class TaskReminders
{
}

public class TaskReminderDto
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public DateTimeOffset DueDate { get; set; }
    public string UsersEmail { get; set; } = string.Empty;

}