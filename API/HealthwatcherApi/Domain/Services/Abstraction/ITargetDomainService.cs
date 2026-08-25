using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.Services.Abstraction;

// Cross-entity rules. Takes primitives, since Domain can't depend on Application's contracts.
public interface ITargetDomainService
{
    Task<Target> InsertTarget(string url, CancellationToken cancellationToken = default);

    void RenameTarget(Target target, string newName);

    void DeleteTarget(Target target);
}
