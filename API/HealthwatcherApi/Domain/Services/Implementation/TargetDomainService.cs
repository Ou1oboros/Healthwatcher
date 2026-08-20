using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Services.Abstraction;

namespace HealthwatcherApi.Domain.Services.Implementation;

public class TargetDomainService : ITargetDomainService
{
    public Target InsertTarget(string name)
    {
        throw new NotImplementedException();
    }

    public void RenameTarget(Target target, string newName)
    {
        throw new NotImplementedException();
    }


    private static void ValidateNameIsFree(string name) => throw new NotImplementedException();
}
