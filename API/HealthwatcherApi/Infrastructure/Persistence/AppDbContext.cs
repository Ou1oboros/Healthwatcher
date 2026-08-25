using System.Linq.Expressions;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using HealthwatcherApi.Infrastructure.Persistence.Converters;
using HealthwatcherApi.Infrastructure.Persistence.EntitiesConfiguration;
using HealthwatcherApi.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace HealthwatcherApi.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly RequestContext _requestContext;

    public DbSet<Target> Targets => Set<Target>();
    public DbSet<TargetHistory> TargetHistory => Set<TargetHistory>();

    // Not domain data: the row replicas use to elect one health-check timer.
    public DbSet<MonitorLease> MonitorLeases => Set<MonitorLease>();

    public AppDbContext(DbContextOptions<AppDbContext> options, RequestContext requestContext)
        : base(options)
    {
        _requestContext = requestContext;
    }

    // Covers DateTimeOffset? too, so Target.CheckedAt gets the same conversion.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<DateTimeOffset>().HaveConversion<UtcDateTimeOffsetConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                continue;

            // WHERE is_deleted = false, on every query against this entity.
            ParameterExpression parameter = Expression.Parameter(entityType.ClrType, "e");
            MemberExpression isDeleted = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            BinaryExpression notDeleted = Expression.Equal(isDeleted, Expression.Constant(false));

            modelBuilder.Entity(entityType.ClrType)
                .HasQueryFilter(Expression.Lambda(notDeleted, parameter));
        }

        modelBuilder.ApplyEntitiesConfiguration();

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampAuditFields();
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampAuditFields();
        return base.SaveChanges();
    }

    private void StampAuditFields()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string username = _requestContext.CurrentUsername ?? "system";

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
                entry.Entity.CreatedBy = username;
                entry.Entity.UpdatedBy = username;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = username;
            }
        }
    }
}
