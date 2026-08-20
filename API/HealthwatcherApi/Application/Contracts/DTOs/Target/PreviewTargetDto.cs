namespace HealthwatcherApi.Application.Contracts;

public class PreviewTargetDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
