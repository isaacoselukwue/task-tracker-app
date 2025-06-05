namespace TaskTracker.Domain.Entities;
public class Users : IdentityUser<Guid>
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public StatusEnum UsersStatus { get; set; }
    public DateTimeOffset? LastLoginDate { get; set; }
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}