global using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
global using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
global using System.Reflection;

namespace TaskTracker.Infrastructure.Data;
public class TaskDbContext : IdentityDbContext<Users, UserRoles, Guid>, IDataProtectionKeyContext, ITaskDbContext
{
    public TaskDbContext(DbContextOptions<TaskDbContext> options) : base(options)
    {
    }
    public new virtual DbSet<Users> Users { get; set; }
    public new virtual DbSet<UserRoles> Roles { get; set; }
    public new virtual DbSet<IdentityUserRole<Guid>> UserRoles { get; set; }
    public virtual DbSet<DataProtectionKey> DataProtectionKeys { get; set; }
    public virtual DbSet<PasswordHistories> PasswordHistories { get; set; }
    public virtual DbSet<Domain.Entities.Tasks> Tasks { get; set; }
    public virtual DbSet<TasksReminder> TaskReminders { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Users>().ToTable("Users");
        builder.Entity<UserRoles>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
