using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public record HouseholdInviteRequest(string Email);
public record HouseholdRespondRequest(Guid LinkId, bool Accept);

public static class HouseholdEndpoints
{
    public static void MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/household").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, HouseholdService households) =>
            Results.Ok(await households.GetViewAsync(p.UserId())));

        group.MapPost("/invite", async (HouseholdInviteRequest req, ClaimsPrincipal p,
            HouseholdService households) =>
        {
            try
            {
                await households.InviteAsync(p.UserId(), req.Email);
                return Results.Ok(await households.GetViewAsync(p.UserId()));
            }
            catch (HouseholdError e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });

        group.MapPost("/respond", async (HouseholdRespondRequest req, ClaimsPrincipal p,
            HouseholdService households) =>
        {
            try
            {
                await households.RespondAsync(p.UserId(), req.LinkId, req.Accept);
                return Results.Ok(await households.GetViewAsync(p.UserId()));
            }
            catch (HouseholdError e)
            {
                return Results.BadRequest(new { error = e.Message });
            }
        });

        group.MapDelete("/{linkId:guid}", async (Guid linkId, ClaimsPrincipal p,
            HouseholdService households) =>
        {
            try
            {
                await households.UnlinkAsync(p.UserId(), linkId);
                return Results.NoContent();
            }
            catch (HouseholdError)
            {
                return Results.NotFound();
            }
        });

        // Combined view: the caller plus every accepted partner. Members see
        // each other's net worth and category totals, never itemised accounts.
        group.MapGet("/summary", async (ClaimsPrincipal p, AppDbContext db,
            HouseholdService households, SummaryService summaries) =>
        {
            var userId = p.UserId();
            var memberIds = new List<Guid> { userId };
            memberIds.AddRange(await households.AcceptedPartnerIdsAsync(userId));

            var users = await db.Users.AsNoTracking()
                .Where(u => memberIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var memberSummaries = new List<object>();
            decimal netWorth = 0, assets = 0, liabilities = 0;
            foreach (var id in memberIds)
            {
                var summary = await summaries.BuildAsync(id);
                netWorth += summary.NetWorth;
                assets += summary.TotalAssets;
                liabilities += summary.TotalLiabilities;
                memberSummaries.Add(new
                {
                    users[id].DisplayName,
                    IsYou = id == userId,
                    summary.NetWorth,
                    summary.TotalAssets,
                    summary.TotalLiabilities,
                    summary.AssetTotals,
                    summary.LiabilityTotals,
                });
            }

            return Results.Ok(new
            {
                CombinedNetWorth = netWorth,
                CombinedAssets = assets,
                CombinedLiabilities = liabilities,
                Members = memberSummaries,
            });
        });
    }
}
