using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Infrastructure.Monitoring.Leasing;
using HealthwatcherApi.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Infrastructure.Persistence.EntitiesConfiguration;

// Split this across multiple files once the schema grows.
public static class EntitiesConfiguration
{
    public static void ApplyEntitiesConfiguration(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Target>().OwnsOne(
            o => o.HealthCheckRecord,
            sa =>
            {
                sa.Property(p => p.StatusCode).HasColumnName("status_code");
                sa.Property(p => p.Error).HasColumnName("error");
                sa.Property(p => p.ResponseTimeMs).HasColumnName("response_time_ms");
                sa.Property(p => p.Status).HasColumnName("status");
            });

        modelBuilder.Entity<TargetHistory>()
            .HasOne<Target>()
            .WithMany()
            .HasForeignKey(h => h.TargetId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TargetHistory>().OwnsOne(
            o => o.HealthCheckRecord,
            sa =>
            {
                sa.Property(p => p.StatusCode).HasColumnName("status_code");
                sa.Property(p => p.Error).HasColumnName("error");
                sa.Property(p => p.ResponseTimeMs).HasColumnName("response_time_ms");
                sa.Property(p => p.Status).HasColumnName("status");
            });

        ConfigureMonitorLease(modelBuilder);
    }

    private static void ConfigureMonitorLease(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MonitorLease>(lease =>
        {
            // Fixed key: there is one lease, and the seed below owns its id.
            lease.Property(l => l.Id).ValueGeneratedNever();

            lease.Property(l => l.Owner).HasMaxLength(ValidationConstants.LeaseOwnerMaxLength).IsRequired();

            // Adds "AND token = <the value read>" to the UPDATE, so only one replica can win
            // a lease that has just expired.
            lease.Property(l => l.Token).IsConcurrencyToken();

            // Seeded expired and unowned, so the first pod to tick can claim it. Anonymous
            // object because the setters are private; fixed token because seed data must be stable.
            lease.HasData(new
            {
                Id = MonitorLease.SingletonId,
                Owner = string.Empty,
                ExpiresAt = DateTimeOffset.MinValue,
                Token = new Guid("00000000-0000-0000-0000-000000000001"),
            });
        });
    }
}
