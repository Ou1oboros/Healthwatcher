using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;

namespace HealthwatcherApi.Domain.Services.Implementation;

public class TargetDomainService(ITargetRepository targetRepository) : ITargetDomainService
{
    public async Task<Target?> InsertTarget(string url)
    {
        string name = ExtractName(url);
        Target? target = await targetRepository.InsertTargetAsync(name, url);
        if (target == null)
            throw new BusinessException("");
        return target;
    }

    private static string ExtractName(string url)
    {
        if (!url.Contains("://"))
            url = "https://" + url;

        string host = new Uri(url).Host; // e.g. "www.example.com"
        string[] parts = host.Split('.');
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }


    private static void ValidateUrlIsFree(string name) => throw new NotImplementedException();
}
