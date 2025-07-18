global using TaskTracker.Infrastructure;
global using TaskTracker.Infrastructure.Email;
global using TaskTracker.Service;
global using TaskTracker.Service.Consumers;
global using TaskTracker.Service.Jobs;
global using Serilog;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.AddSerilog((context, config) =>
{
    config.Enrich.FromLogContext()
        .WriteTo.Console()
        .ReadFrom.Configuration(builder.Configuration);
});


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
builder.Services.AddSingleton<INotificationJob, NotificationJob>();

var host = builder.Build();
host.MapGet("/", () => "Hello World!");
host.Run();
