using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;
public class TaskReminderConfiguration : IEntityTypeConfiguration<TasksReminder>
{
    public void Configure(EntityTypeBuilder<TasksReminder> builder)
    {
        builder.ToTable("TaskReminder");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.OffsetFromTaskTime).IsRequired();
        builder.Property(r => r.Sent).IsRequired().HasDefaultValue(false);
        builder.Property(r => r.SentAt).IsRequired(false);
        builder.HasOne(r => r.Task)
               .WithMany(t => t.Reminders)
               .HasForeignKey(r => r.TaskId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
