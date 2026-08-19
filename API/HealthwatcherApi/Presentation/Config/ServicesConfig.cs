using HealthwatcherApi.Application.Services.Abstraction;
using HealthwatcherApi.Application.Services.Implementation;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Domain.Services.Implementation;
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

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(config.GetConnectionString("DefaultConnection"))
            .UseSnakeCaseNamingConvention());

        // The DbContext is the unit of work; services decide when to commit.
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddScoped<ITargetRepository, TargetRepository>();

        services.AddScoped<ITargetDomainService, TargetDomainService>();

        services.AddScoped<ITargetService, TargetService>();

        services.AddHealthChecks();

        string[] origins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? DefaultCorsOrigins;

        services.AddCors(options => options.AddPolicy(SpaCorsPolicy, policy => policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        return services;
    }
}
