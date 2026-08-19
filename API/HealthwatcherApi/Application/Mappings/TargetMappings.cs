using HealthwatcherApi.Application.Contracts;
using HealthwatcherApi.Domain.Entities;

namespace HealthwatcherApi.Application.Mappings;

/// <summary>
/// Entity to DTO only. Entities are created through their own constructors and
/// domain methods, never mapped into from the outside — so there is no reverse.
/// Hand-written on purpose: a missed property is a compile error, not a surprise
/// null in production.
/// </summary>
public static class TargetMappings
{
    public static TargetDto ToDto(this Target target) => new TargetDto
    {
        Id = target.Id,
        CreatedAt = target.CreatedAt,
        UpdatedAt = target.UpdatedAt,
        Name = target.Name,
    };

    public static PreviewTargetDto ToPreviewDto(this Target target) => new PreviewTargetDto
    {
        Id = target.Id,
        Name = target.Name,
        UpdatedAt = target.UpdatedAt,
    };

    public static IReadOnlyList<PreviewTargetDto> ToPreviewDtos(this IEnumerable<Target> targets) =>
        targets.Select(ToPreviewDto).ToList();
}
