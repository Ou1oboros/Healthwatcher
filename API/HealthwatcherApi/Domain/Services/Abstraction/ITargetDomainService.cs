using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Domain.Services.Abstraction;

/// <summary>
/// Rules that span more than one entity, and so have no single entity to live on.
/// Takes primitives rather than DTOs — the domain must not depend on the
/// application layer's contracts.
/// </summary>
public interface ITargetDomainService
{
    Task<Target> InsertTarget(string url, CancellationToken cancellationToken = default);

    void RenameTarget(Target target, string newName);

    void DeleteTarget(Target target);
}
