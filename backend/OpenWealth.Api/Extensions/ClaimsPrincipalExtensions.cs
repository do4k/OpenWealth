using System.Security.Claims;

namespace OpenWealth.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>Reads the authenticated user id from the JWT subject claim.</summary>
    public static Guid UserId(this ClaimsPrincipal principal)
    {
        var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new InvalidOperationException("Authenticated principal has no subject claim.");
        return Guid.Parse(sub);
    }
}
