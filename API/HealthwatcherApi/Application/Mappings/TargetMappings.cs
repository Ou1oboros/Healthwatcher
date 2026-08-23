using HealthwatcherApi.Application.Contracts.DTOs.Target;
using HealthwatcherApi.Domain.Entities;
using HealthwatcherApi.Shared.Common;

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
        Url = target.Url,
        CheckedAt = target.CheckedAt,
        IsEnabled = target.IsEnabled,
        HealthCheckRecord = target.HealthCheckRecord.RecordDto(),
    };

    public static PreviewTargetDto ToPreviewDto(this Target target) => new PreviewTargetDto
    {
        Id = target.Id,
        Name = target.Name,
        Url = target.Url,
        Status = target.HealthCheckRecord.Status,
        IsEnabled = target.IsEnabled,
        CheckedAt = target.CheckedAt,
        ResponseTimeMs = target.HealthCheckRecord.ResponseTimeMs,
        StatusCode = target.HealthCheckRecord.StatusCode,
    };

    public static IReadOnlyList<PreviewTargetDto> ToPreviewDtos(this IEnumerable<Target> targets) =>
        targets.Select(ToPreviewDto).ToList();

    public static TargetHistoryDto ToDto(this TargetHistory history) => new TargetHistoryDto
    {
        Id = history.Id,
        CheckedAt = history.CheckedAt,
        HealthCheckRecord = history.HealthCheckRecord.RecordDto(),
    };

    public static IReadOnlyList<TargetHistoryDto> ToDtos(this IEnumerable<TargetHistory> history) =>
        history.Select(ToDto).ToList();

    public static HealthCheckRecordDto RecordDto(this HealthCheckRecord record) => new HealthCheckRecordDto
    {
        Error = record.Error,
        Status = record.Status,
        StatusCode = record.StatusCode,
        ResponseTimeMs = record.ResponseTimeMs,
    };
}
