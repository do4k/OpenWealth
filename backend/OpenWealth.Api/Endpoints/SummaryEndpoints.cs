using System.Security.Claims;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class SummaryEndpoints
{
    public static void MapSummaryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/summary", async (ClaimsPrincipal principal, SummaryService summaries) =>
            Results.Ok(await summaries.BuildAsync(principal.UserId())))
            .RequireAuthorization();
    }
}
