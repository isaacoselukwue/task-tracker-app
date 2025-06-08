namespace TaskTracker.Domain.Constants;
public static class ReminderOffsets
{
    public static readonly TimeSpan AtTime = TimeSpan.Zero;
    public static readonly TimeSpan OneHourBefore = TimeSpan.FromHours(-1);
    public static readonly TimeSpan OneDayBefore = TimeSpan.FromDays(-1);
}