using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class HouseholdEndpoints
{
    public static void MapHouseholdEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/household").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var myMembership = await db.HouseholdMembers.AsNoTracking()
                .SingleOrDefaultAsync(m => m.UserId == userId);
            if (myMembership is null)
                return Results.Ok(new { InHousehold = false });

            var members = await db.HouseholdMembers.AsNoTracking()
                .Where(m => m.HouseholdId == myMembership.HouseholdId)
                .Join(db.Users.AsNoTracking(), m => m.UserId, u => u.Id, (m, u) => new { m, u })
                .ToListAsync();

            return Results.Ok(new
            {
                InHousehold = true,
                MyStatus = myMembership.Status,
                MyVisibility = myMembership.Visibility,
                Members = members.Select(x => new
                {
                    MembershipId = x.m.Id,
                    UserId = x.u.Id,
                    x.u.DisplayName,
                    x.u.Email,
                    x.m.Status,
                    x.m.Visibility,
                    IsMe = x.u.Id == userId,
                }),
            });
        });

        group.MapPost("/invite", async (HouseholdInviteRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var invitee = await db.Users.SingleOrDefaultAsync(u => u.Email == req.Email);
            if (invitee is null)
                return Results.BadRequest(new { error = "No OpenWealth user with that email." });
            if (invitee.Id == userId)
                return Results.BadRequest(new { error = "You can't invite yourself." });

            var inviteeExisting = await db.HouseholdMembers.AnyAsync(m => m.UserId == invitee.Id);
            if (inviteeExisting)
                return Results.BadRequest(new { error = $"{invitee.DisplayName} is already in a household." });

            var myMembership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == userId);
            if (myMembership is not null && myMembership.Status == HouseholdMemberStatus.Invited)
                return Results.BadRequest(new { error = "Accept or decline your own pending invite first." });

            Guid householdId;
            if (myMembership is null)
            {
                var household = new Household { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
                db.Households.Add(household);
                householdId = household.Id;
                db.HouseholdMembers.Add(new HouseholdMember
                {
                    Id = Guid.NewGuid(),
                    HouseholdId = householdId,
                    UserId = userId,
                    Status = HouseholdMemberStatus.Active,
                    InvitedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow,
                });
            }
            else
            {
                householdId = myMembership.HouseholdId;
            }

            db.HouseholdMembers.Add(new HouseholdMember
            {
                Id = Guid.NewGuid(),
                HouseholdId = householdId,
                UserId = invitee.Id,
                Status = HouseholdMemberStatus.Invited,
                InvitedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
            });
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPost("/accept", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var membership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == p.UserId());
            if (membership is null || membership.Status != HouseholdMemberStatus.Invited)
                return Results.BadRequest(new { error = "No pending invite." });
            membership.Status = HouseholdMemberStatus.Active;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        group.MapPut("/visibility", async (HouseholdVisibilityRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var membership = await db.HouseholdMembers.SingleOrDefaultAsync(
                m => m.UserId == p.UserId() && m.Status == HouseholdMemberStatus.Active);
            if (membership is null)
                return Results.BadRequest(new { error = "You're not an active household member." });
            membership.Visibility = req.Visibility;
            await db.SaveChangesAsync();
            return Results.Ok(new { membership.Visibility });
        });

        // Declining a pending invite and leaving an accepted household are the
        // same operation from the data's point of view: remove my own row, and
        // clean up the household if that leaves it empty.
        group.MapDelete("/membership", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var membership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == userId);
            if (membership is null) return Results.NotFound();

            var householdId = membership.HouseholdId;
            db.HouseholdMembers.Remove(membership);
            await db.SaveChangesAsync();

            var remaining = await db.HouseholdMembers.AnyAsync(m => m.HouseholdId == householdId);
            if (!remaining)
            {
                await db.Households.Where(h => h.Id == householdId).ExecuteDeleteAsync();
            }
            return Results.NoContent();
        });

        group.MapGet("/summary", async (ClaimsPrincipal p, AppDbContext db, SummaryService summaries) =>
        {
            var userId = p.UserId();
            var myMembership = await db.HouseholdMembers.AsNoTracking()
                .SingleOrDefaultAsync(m => m.UserId == userId && m.Status == HouseholdMemberStatus.Active);
            if (myMembership is null)
                return Results.BadRequest(new { error = "You're not an active household member." });

            var activeMembers = await db.HouseholdMembers.AsNoTracking()
                .Where(m => m.HouseholdId == myMembership.HouseholdId && m.Status == HouseholdMemberStatus.Active)
                .Join(db.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                    (m, u) => new { UserId = m.UserId, u.DisplayName, m.Visibility })
                .ToListAsync();

            var views = new List<object>();
            var totalNetWorth = 0m;
            foreach (var member in activeMembers)
            {
                var summary = await summaries.BuildAsync(member.UserId);
                totalNetWorth += summary.NetWorth;
                views.Add(summary.ToShareView(member.DisplayName, member.Visibility));
            }

            return Results.Ok(new { TotalNetWorth = totalNetWorth, Members = views });
        });
    }
}
