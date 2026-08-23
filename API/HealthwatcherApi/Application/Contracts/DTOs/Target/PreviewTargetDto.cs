using Enums;

namespace HealthwatcherApi.Application.Contracts.DTOs.Target;

public class PreviewTargetDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required ConnectionStatus Status { get; init; }
}
