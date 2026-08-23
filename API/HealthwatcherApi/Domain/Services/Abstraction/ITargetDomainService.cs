using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.Services.Abstraction;

// Cross-entity rules that don't belong on a single entity. Takes primitives, not DTOs -
// Domain can't depend on Application's contracts.
public interface ITargetDomainService
{
    Task<Target> InsertTarget(string url, CancellationToken cancellationToken = default);

    void RenameTarget(Target target, string newName);

    void DeleteTarget(Target target);
}
