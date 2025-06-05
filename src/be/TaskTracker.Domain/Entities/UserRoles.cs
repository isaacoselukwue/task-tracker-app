global using Microsoft.AspNetCore.Identity;

namespace TaskTracker.Domain.Entities;
public class UserRoles : IdentityRole<Guid>
{
    public DateTimeOffset Created { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
    public StatusEnum UserRoleStatus { get; set; }
}