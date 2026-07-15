using OpenWealth.Api.Services;

namespace OpenWealth.Api.Extensions;

public static class DomainExceptionExtensions
{
    /// <summary>Maps a domain error to the HTTP response an endpoint should return for it.</summary>
    public static IResult ToResult(this DomainException ex) =>
        Results.Json(new { error = ex.Message }, statusCode: ex.StatusCode);
}
