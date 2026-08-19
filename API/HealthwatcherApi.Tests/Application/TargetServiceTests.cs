using HealthwatcherApi.Application.Services.Implementation;
using HealthwatcherApi.Application.Transactions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Implementation;
using NSubstitute;

namespace HealthwatcherApi.Tests.Application;

/// <summary>
/// Application services orchestrate: load, delegate to the domain, save, map out.
/// Repositories and the unit of work are substituted, so nothing here touches a database.
/// </summary>
public class TargetServiceTests
{
    private readonly ITargetRepository _targetRepository = Substitute.For<ITargetRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly TargetService _sut;

    public TargetServiceTests()
    {
        _sut = new TargetService(
            _targetRepository,
            new TargetDomainService(),
            _unitOfWork);
    }

    [Fact]
    public Task GetTargetById_ThrowsWhenTheTargetDoesNotExist() => throw new NotImplementedException();

    [Fact]
    public Task GetTargetById_MapsTheEntity() => throw new NotImplementedException();

    [Fact]
    public Task GetTargets_EchoesThePageItWasAskedFor() => throw new NotImplementedException();

    [Fact]
    public Task InsertTarget_SavesExactlyOnce() => throw new NotImplementedException();

    [Fact]
    public Task RenameTarget_ThrowsWhenTheTargetDoesNotExist() => throw new NotImplementedException();

    [Fact]
    public Task DeleteTarget_SoftDeletesTheTarget() => throw new NotImplementedException();
}
