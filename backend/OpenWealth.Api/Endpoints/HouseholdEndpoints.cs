using System.Security.Claims;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class HouseholdEndpoints
{
    public static void MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/household").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, HouseholdService household) =>
            Results.Ok(await household.GetViewAsync(p.UserId())));

        group.MapPost("/invite", async (HouseholdInviteRequest req, ClaimsPrincipal p, HouseholdService household) =>
        {
            try
            {
                await household.InviteAsync(p.UserId(), req.Email);
                return Results.Ok();
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
        });

        group.MapPost("/accept", async (ClaimsPrincipal p, HouseholdService household) =>
        {
            try
            {
                await household.AcceptAsync(p.UserId());
                return Results.Ok();
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
        });

        group.MapPut("/visibility", async (HouseholdVisibilityRequest req, ClaimsPrincipal p, HouseholdService household) =>
        {
            try
            {
                var visibility = await household.SetVisibilityAsync(p.UserId(), req.Visibility);
                return Results.Ok(new { Visibility = visibility });
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
        });

        // Declining a pending invite and leaving an accepted household are the
        // same request; HouseholdService.LeaveAsync treats them identically.
        group.MapDelete("/membership", async (ClaimsPrincipal p, HouseholdService household) =>
        {
            var left = await household.LeaveAsync(p.UserId());
            return left ? Results.NoContent() : Results.NotFound();
        });

        group.MapGet("/summary", async (ClaimsPrincipal p, HouseholdService household, SummaryService summaries) =>
        {
            try
            {
                return Results.Ok(await household.BuildSummaryAsync(p.UserId(), summaries));
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
        });
    }
}
