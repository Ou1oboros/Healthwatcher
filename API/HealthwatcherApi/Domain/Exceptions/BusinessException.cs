namespace HealthwatcherApi.Domain.Exceptions;

/// <summary>A violated domain rule; ExceptionMiddleware surfaces it as a 400.</summary>
public class BusinessException(string message) : Exception(message);
