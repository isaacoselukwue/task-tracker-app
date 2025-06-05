global using TaskTracker.Domain.Enums;

namespace TaskTracker.Domain.Events;
public class NotificationEvent(string receiver, string subject, NotificationTypeEnum notificationType,
    Dictionary<string, string> replacements) : BaseEvent
{
    public string Receiver { get; } = receiver;
    public string? Subject { get; } = subject;
    public Dictionary<string, string> Replacements { get; } = replacements;
    public NotificationTypeEnum NotificationType { get; } = notificationType;
}