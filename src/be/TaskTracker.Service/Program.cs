global using TaskTracker.Infrastructure;
global using TaskTracker.Infrastructure.Email;
global using TaskTracker.Service;
global using TaskTracker.Service.Consumers;
global using TaskTracker.Service.Jobs;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.AddInfrastructureWorkerServices();

RabbitMqSettings queueSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;

builder.Services.AddMassTransit(config =>
{
    config.AddConsumer<NotificationConsumer>();

    config.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(queueSettings.Host, queueSettings.VHost, h =>
        {
            h.Username(queueSettings.Username!);
            h.Password(queueSettings.Password!);
        });

        cfg.ReceiveEndpoint("notification-queue", e =>
        {
            e.ConfigureConsumer<NotificationConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
        cfg.UseInstrumentation();
    });
    config.AddHealthChecks();
});

builder.Services.AddScoped<NotificationConsumer>();
builder.Services.AddScoped<INotificationJob, NotificationJob>();

var host = builder.Build();
host.Run();
