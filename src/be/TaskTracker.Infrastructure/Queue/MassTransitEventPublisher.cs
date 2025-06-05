global using MassTransit;
global using MediatR;

namespace TaskTracker.Infrastructure.Queue;
public class MassTransitEventPublisher(ILogger<MassTransitEventPublisher> logger, IPublishEndpoint publishEndpoint) : IPublisher
{
    private readonly ILogger<MassTransitEventPublisher> _logger = logger;
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        _logger.LogInformation("Publish<T> called with {NotificationType}", notification.GetType().Name);
        if (notification is NotificationEvent notificationEvent)
        {
            await _publishEndpoint.Publish(notificationEvent, cancellationToken);
        }
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Publish called with {NotificationType}", notification.GetType().Name);
        if (notification is NotificationEvent notificationEvent)
        {
            await _publishEndpoint.Publish(notificationEvent, cancellationToken);
        }
    }
}
