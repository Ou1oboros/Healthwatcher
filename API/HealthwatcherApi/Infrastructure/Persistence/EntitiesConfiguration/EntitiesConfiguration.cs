using HealthwatcherApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Infrastructure.Persistence.EntitiesConfiguration;

// When the schema expands you should split the configuration across multiple files
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
    }
}
