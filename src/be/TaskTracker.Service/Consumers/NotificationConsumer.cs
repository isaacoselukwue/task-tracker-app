global using MassTransit;
global using TaskTracker.Application.Common.Interfaces;
global using TaskTracker.Domain.Events;

namespace TaskTracker.Service.Consumers;
internal class NotificationConsumer(ILogger<NotificationConsumer> logger, IEmailService emailService) : IConsumer<NotificationEvent>
{
    public async Task Consume(ConsumeContext<NotificationEvent> context)
    {
        try
        {
            await emailService.SendEmail(context.Message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while consuming the notification event");
        }
    }
}