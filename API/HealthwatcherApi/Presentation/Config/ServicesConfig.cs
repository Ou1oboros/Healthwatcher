using HealthwatcherApi.Application.Options;
using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Application.Services.Implementation;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Domain.Services.Implementation;
using HealthwatcherApi.Infrastructure.Monitoring;
using HealthwatcherApi.Infrastructure.Persistence;
using HealthwatcherApi.Infrastructure.Persistence.Repositories;
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

        // SQLite: one file, no server process. The whole backend fits in a single small pod,
        // which matters more on a reviewer's minikube than anything Postgres would buy here.
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

    /// <summary>Validated at startup: a bad interval in the ConfigMap fails the pod loudly instead of silently monitoring nothing.</summary>
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

        services.AddHttpClient(HttpHealthProbe.HttpClientName)
            .ConfigureHttpClient((provider, client) =>
            {
                MonitoringOptions options = provider.GetRequiredService<
                    Microsoft.Extensions.Options.IOptions<MonitoringOptions>>().Value;

                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Healthwatcher/1.0");
            })
            // Pooled handlers keep connections warm between cycles and recycle them often
            // enough that a target's DNS record changing is picked up.
            .SetHandlerLifetime(TimeSpan.FromMinutes(5));

        services.AddHostedService<HealthMonitorBackgroundService>();

        return services;
    }
}
