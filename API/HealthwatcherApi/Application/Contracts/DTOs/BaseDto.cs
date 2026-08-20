namespace HealthwatcherApi.Application.Contracts;

public abstract class BaseDto
{
    public Guid Id { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
