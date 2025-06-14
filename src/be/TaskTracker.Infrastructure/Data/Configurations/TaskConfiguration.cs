namespace TaskTracker.Infrastructure.Data.Configurations;
public class TaskConfiguration : IEntityTypeConfiguration<Domain.Entities.Tasks>
{
    public void Configure(EntityTypeBuilder<Domain.Entities.Tasks> builder)
    {
        builder.ToTable("Task");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasDefaultValue(StatusEnum.Active);
        builder.Property(e => e.Created)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.LastModified)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.ScheduledFor)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
