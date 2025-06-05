namespace TaskTracker.Domain.Entities;
public class Tasks : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Users? User { get; set; }
    public StatusEnum Status { get; set; }
}
