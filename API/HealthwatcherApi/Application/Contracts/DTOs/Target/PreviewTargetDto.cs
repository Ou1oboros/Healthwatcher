namespace HealthwatcherApi.Application.Contracts;

public class PreviewTargetDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
