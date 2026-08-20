using System.ComponentModel.DataAnnotations;
using Enums;
using HealthwatcherApi.Shared;
using Microsoft.EntityFrameworkCore;

namespace HealthwatcherApi.Domain.Entities;

public class HealthCheckRecord
{
    public double? ResponseTimeMs { get; private set; }
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;
    public int? StatusCode { get; private set; }

    [MaxLength(ValidationConstants.ErrorMaxLength)]
    public string? Error { get; private set; }

    public HealthCheckRecord(double? responseTimeMs, ConnectionStatus status, int? statusCode, string? error)
    {
        ResponseTimeMs = responseTimeMs;
        Status = status;
        StatusCode = statusCode;
        Error = error;
    }

    private HealthCheckRecord()
    {
    }
}
