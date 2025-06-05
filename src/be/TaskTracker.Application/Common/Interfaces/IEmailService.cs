global using TaskTracker.Domain.Events;

namespace TaskTracker.Application.Common.Interfaces;
public interface IEmailService
{
    Task SendEmail(NotificationEvent notification, CancellationToken cancellationToken);
}
