namespace OpenWealth.Api.Services;

/// <summary>
/// Thrown when a service-layer business rule is violated. Carries the HTTP
/// status the endpoint should map it to (400 by default; e.g. 409 for a
/// conflict like a duplicate email).
/// </summary>
public sealed class DomainException(string message, int statusCode = StatusCodes.Status400BadRequest) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
