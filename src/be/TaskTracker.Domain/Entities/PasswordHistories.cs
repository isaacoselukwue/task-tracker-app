global using TaskTracker.Domain.Common;

namespace TaskTracker.Domain.Entities;
public class PasswordHistories : BaseAuditableEntity
{
    public Guid UserId { get; set; }
    public Users? User { get; set; }
    public string? PasswordHash { get; set; }
}