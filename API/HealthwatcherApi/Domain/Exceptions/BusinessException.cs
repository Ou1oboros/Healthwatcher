namespace HealthwatcherApi.Domain.Exceptions;

/// <summary>
/// A domain rule was violated. Surfaced to the caller as a 400 — see ExceptionMiddleware.
/// </summary>
public class BusinessException(string message) : Exception(message);
