global using MassTransit.Logging;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Hosting;
global using Npgsql;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Resources;
global using OpenTelemetry.Trace;
global using TaskTracker.Infrastructure.Data;
global using TaskTracker.Infrastructure.Data.Interceptors;
global using TaskTracker.Infrastructure.Email;
global using TaskTracker.Infrastructure.Identity;
global using TaskTracker.Infrastructure.Queue;
global using TaskTracker.Infrastructure.Tasks;

namespace TaskTracker.Infrastructure;
public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string not found");
        }

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        builder.Services.AddDbContextPool<TaskDbContext>((sp, options) =>
        {
            using (var scope = sp.CreateScope())
            {
                var scopedProvider = scope.ServiceProvider;
                options.AddInterceptors(scopedProvider.GetServices<ISaveChangesInterceptor>());
            }
            options.UseNpgsql(connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null
                        );
                });
        });
        builder.Services.AddScoped<TaskDbContextInitialiser>();
        builder.Services.AddScoped<ITaskDbContext>(provider => provider.GetRequiredService<TaskDbContext>());
        builder.Services.AddIdentityCore<Users>(
            options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.SignIn.RequireConfirmedAccount = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(24);
            }
            )
            .AddRoles<UserRoles>()
            .AddSignInManager()
            .AddTokenProvider<DataProtectorTokenProvider<Users>>("TaskTrackerApp")
            .AddEntityFrameworkStores<TaskDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddDataProtection()
                        .PersistKeysToDbContext<TaskDbContext>()
                        .SetApplicationName("TaskTrackerApplicationService");

        string serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "TaskTracker";
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddNpgsqlInstrumentation()
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddMeter("Microsoft.AspNetCore.Server.Kestrel")
                    .AddMeter("System.Net.Http")
                    .AddMeter("System.Net.NameResolution");
            })
            .WithTracing(tracing =>
            {
                tracing
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddNpgsql();

                tracing.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                });
            });

        //builder.Services.AddMemoryCache();
        //builder.Services.AddStackExchangeRedisCache(options =>
        //{
        //    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
        //});
        builder.Services.AddHybridCache(options =>
        {
            options.MaximumPayloadBytes = 1024 * 1024 * 20;
            options.MaximumKeyLength = 512;

            options.DefaultEntryOptions = new Microsoft.Extensions.Caching.Hybrid.HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromHours(24),
                LocalCacheExpiration = TimeSpan.FromHours(24)
            };
        });

        builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
        builder.Services.AddTransient<IEmailService, EmailService>();

        builder.Services.AddTransient<IIdentityService, IdentityService>();

        builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        builder.Services.AddTransient<IJwtService, JwtService>();

        builder.Services.AddTransient<ITaskTrackerService, TaskTrackerService>();

        builder.Services.AddMassTransit(config =>
        {
            config.UsingRabbitMq((context, cfg) =>
            {
                RabbitMqSettings queueSettings = builder.Configuration.GetSection("RabbitMqSettings").Get<RabbitMqSettings>()!;

                cfg.Host(queueSettings.Host, queueSettings.VHost, h =>
                {
                    h.Username(queueSettings.Username!);
                    h.Password(queueSettings.Password!);
                });

                cfg.ConfigureEndpoints(context);
                cfg.UseInstrumentation();
            });
            config.AddHealthChecks();
        });
        builder.Services.AddScoped<IPublisher, MassTransitEventPublisher>();
    }

    public static void AddInfrastructureWorkerServices(this IHostApplicationBuilder builder)
    {
        string serviceName = builder.Configuration["OpenTelemetry:ServiceName"] ?? "TaskWorker";
        builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing
                .AddSource(DiagnosticHeaders.DefaultListenerName)
                .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation()
                    .AddRedisInstrumentation()
                    .AddNpgsql();

                tracing.AddOtlpExporter(exporter =>
                {
                    exporter.Endpoint = new Uri(builder.Configuration["OpenTelemetry:OtlpEndpoint"] ?? "http://localhost:4317");
                });
            });

        string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Connection string not found");
        }

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        builder.Services.AddDbContextPool<TaskDbContext>((sp, options) =>
        {
            using (var scope = sp.CreateScope())
            {
                var scopedProvider = scope.ServiceProvider;
                options.AddInterceptors(scopedProvider.GetServices<ISaveChangesInterceptor>());
            }
            options.UseNpgsql(connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null
                        );
                });
        });

        builder.Services.Configure<MailSettings>(builder.Configuration.GetSection("MailSettings"));
        builder.Services.AddTransient<IEmailService, EmailService>();
        builder.Services.AddTransient<ITaskTrackerService, TaskTrackerService>();
        builder.Services.AddSingleton(TimeProvider.System);
    }
}
