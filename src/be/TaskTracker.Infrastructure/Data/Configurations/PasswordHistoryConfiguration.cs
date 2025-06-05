global using Microsoft.EntityFrameworkCore;
global using Microsoft.EntityFrameworkCore.Metadata.Builders;
global using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Data.Configurations;
public class PasswordHistoryConfiguration : IEntityTypeConfiguration<PasswordHistories>
{
    public void Configure(EntityTypeBuilder<PasswordHistories> builder)
    {
        builder.ToTable("PasswordHistory");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();
        builder.Property(e => e.PasswordHash)
            .IsRequired()
            .HasMaxLength(256);
        builder.Property(e => e.Created)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property(e => e.LastModified)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}