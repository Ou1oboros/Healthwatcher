using HealthwatcherApi.Domain.Services.Implementation;

namespace HealthwatcherApi.Tests.Domain;

/// <summary>
/// Domain services hold rules that span more than one entity. They take primitives,
/// so these tests need no DTOs, no mocks and no container.
/// </summary>
public class TargetDomainServiceTests
{
    private readonly TargetDomainService _sut = new TargetDomainService();

    [Fact]
    public void InsertTarget_RejectsADuplicateName() => throw new NotImplementedException();

    [Fact]
    public void RenameTarget_RejectsANameAlreadyInUse() => throw new NotImplementedException();

    [Fact]
    public void RenameTarget_LeavesTheNameUnchangedWhenRejected() => throw new NotImplementedException();
}
