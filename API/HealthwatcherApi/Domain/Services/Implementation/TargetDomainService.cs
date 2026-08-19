using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Services.Abstraction;

namespace HealthwatcherApi.Domain.Services.Implementation;

public class TargetDomainService : ITargetDomainService
{
    public Target InsertTarget(string name)
    {
        ValidateNameIsFree(name);
        return new Target(name);
    }

    public void RenameTarget(Target target, string newName)
    {
        ValidateNameIsFree(newName);
        target.Name = newName;
    }


    private static void ValidateNameIsFree(string name) => throw new NotImplementedException();
}
