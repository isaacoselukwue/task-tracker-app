using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Application.Common.Interfaces;
public interface ITaskDbContext
{
    DbSet<PasswordHistories> PasswordHistories { get; set; }
    DbSet<UserRoles> Roles { get; set; }
    DbSet<Tasks> Tasks { get; set; }
    DbSet<IdentityUserRole<Guid>> UserRoles { get; set; }
    DbSet<Users> Users { get; set; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
