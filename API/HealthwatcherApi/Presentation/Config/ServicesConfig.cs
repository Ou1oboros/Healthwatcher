using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Application.Services.Implementation;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Domain.Services.Implementation;
using HealthwatcherApi.Infrastructure.Monitoring;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using HealthwatcherApi.Infrastructure.Persistence;
using HealthwatcherApi.Infrastructure.Persistence.Repositories;
using HealthwatcherApi.Infrastructure.Persistence.Seeding;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Presentation.Config;

public static class ServicesConfig
{
    public const string SpaCorsPolicy = "SpaClient";

    private static readonly string[] DefaultCorsOrigins = ["http://localhost:4200"];

    public static IServiceCollection AddProjectServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<RequestContext>();

        services.AddMonitoringOptions(config);

        // SQLite: one file, no server process, so the whole backend fits in one small pod.
        services.AddDbContext<AppDbContext>(options => options
            .UseSqlite(config.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

        // The DbContext is the unit of work; services decide when to commit.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ITargetRepository, TargetRepository>();

        services.AddScoped<ITargetDomainService, TargetDomainService>();

        services.AddScoped<ITargetService, TargetService>();
        services.AddScoped<IHealthMonitorService, HealthMonitorService>();

        services.AddMonitoring();

        services.AddHealthChecks();

        string[] origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? DefaultCorsOrigins;

        services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }

    // ValidateOnStart, so bad config fails the pod instead of silently monitoring nothing.
    private static IServiceCollection AddMonitoringOptions(this IServiceCollection services, IConfiguration config)
    {
        services.AddOptions<MonitoringOptions>()
            .Bind(config.GetSection(MonitoringOptions.SectionName))
            .Validate(options => !options.Validate().Any(), "Invalid Monitoring configuration.")
            .ValidateOnStart();

        return services;
    }

    private static IServiceCollection AddMonitoring(this IServiceCollection services)
    {
        services.AddScoped<IHealthProbe, HttpHealthProbe>();

        services.AddScoped<IMonitorLeaseStore, MonitorLeaseStore>();

        services.AddHttpClient(HttpHealthProbe.HttpClientName)
            .ConfigureHttpClient((provider, client) =>
            {
                MonitoringOptions options = provider.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<MonitoringOptions>>().Value;

                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Healthwatcher/1.0");
            })
            // Warm connections between cycles, recycled often enough to notice a DNS change.
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        // Ordered: migrate and seed first, then start probing what was seeded.
        services.AddHostedService<DatabaseInitializer>();
        services.AddHostedService<HealthMonitorBackgroundService>();

        return services;
    }
}
