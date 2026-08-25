using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Domain.Exceptions;
using HealthwatcherApi.Domain.IRepositories;
using HealthwatcherApi.Domain.Services.Abstraction;
using HealthwatcherApi.Shared.Common;

namespace HealthwatcherApi.Domain.Services.Implementation;

public class TargetDomainService(ITargetRepository targetRepository) : ITargetDomainService
{
    // Labels that belong to the suffix rather than the name, so "nic.gov.sa" is named
    // after "nic". Not a full public suffix list, but it covers what this dashboard sees.
    private static readonly HashSet<string> SuffixSecondLevelLabels =
        new(StringComparer.OrdinalIgnoreCase) { "ac", "co", "com", "edu", "gov", "mil", "net", "org", "sch" };

    public async Task<Target> InsertTarget(string url, CancellationToken cancellationToken = default)
    {
        Uri uri = ParseUrl(url);
        string normalizedUrl = uri.ToString();
        string name = ExtractName(uri);

        await ValidateUrlIsFree(normalizedUrl, cancellationToken);

        Target target = new Target(name, normalizedUrl, new HealthCheckRecord());
        targetRepository.AddTarget(target);

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

    // Names a target after the registrable part of its host: "www.google.com" and
    // "api.google.com" are both "google". An address or "localhost" keeps the host.
    private static string ExtractName(Uri uri)
    {
        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
            return uri.Host;

        string[] labels = uri.Host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (labels.Length < 2)
            return uri.Host;

        bool suffixIsTwoLabels = labels.Length > 2 && SuffixSecondLevelLabels.Contains(labels[^2]);
        return suffixIsTwoLabels ? labels[^3] : labels[^2];
    }

    private async Task ValidateUrlIsFree(string url, CancellationToken cancellationToken)
    {
        if (await targetRepository.ExistsByUrlAsync(url, cancellationToken))
            throw new BusinessException($"A target for '{url}' already exists.");
    }
}
