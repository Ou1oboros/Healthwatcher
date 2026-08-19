namespace HealthwatcherApi.Application.Contracts;

public abstract class BaseDto
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}
