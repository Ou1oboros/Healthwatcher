using Enums;

namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

// One dashboard row - everything the list view needs in a single request.
public class PreviewTargetDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public required ConnectionStatus Status { get; init; }
    public required bool IsEnabled { get; init; }

    /// <summary>Null until the target has been probed at least once.</summary>
    public required DateTimeOffset? CheckedAt { get; init; }
    public required double? ResponseTimeMs { get; init; }
    public required int? StatusCode { get; init; }
}
