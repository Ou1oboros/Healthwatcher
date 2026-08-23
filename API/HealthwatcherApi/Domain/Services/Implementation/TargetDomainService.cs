using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;

namespace HealthwatcherApi.Domain.Services.Implementation;

public class TargetDomainService(ITargetRepository targetRepository) : ITargetDomainService
{
    public async Task<Target> InsertTarget(string url, CancellationToken cancellationToken = default)
    {
        Uri uri = ParseUrl(url);
        string normalizedUrl = uri.ToString();
        string name = ExtractName(uri);

        await ValidateUrlIsFree(normalizedUrl, cancellationToken);

        Target? target = await targetRepository.InsertTargetAsync(name, normalizedUrl, cancellationToken);
        if (target == null)
            throw new BusinessException("Failed to create the target.");

        return target;
    }

    public void RenameTarget(Target target, string newName)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.Rename(newName);
    }

    public void DeleteTarget(Target target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.IsDeleted = true;
    }

    private static Uri ParseUrl(string url)
    {
        string candidate = url.Contains("://", StringComparison.Ordinal) ? url : $"https://{url}";

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new BusinessException($"'{url}' is not a valid http or https URL.");
        }

        return uri;
    }

    private static string ExtractName(Uri uri)
    {
        string[] parts = uri.Host.Split('.');
        return parts.Length >= 2 ? parts[^2] : parts[0];
    }

    private async Task ValidateUrlIsFree(string url, CancellationToken cancellationToken)
    {
        if (await targetRepository.ExistsByUrlAsync(url, cancellationToken))
            throw new BusinessException($"A target for '{url}' already exists.");
    }
}
