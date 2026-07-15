using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Owns the household invite/accept/leave workflow and its invariants — a
/// user has at most one membership row, ever (pending or accepted), and
/// invites only ever resolve against an existing registered user.
/// </summary>
public class HouseholdService(AppDbContext db)
{
    public async Task<HouseholdView> GetViewAsync(Guid userId, CancellationToken ct = default)
    {
        var myMembership = await db.HouseholdMembers.AsNoTracking()
            .SingleOrDefaultAsync(m => m.UserId == userId, ct);
        if (myMembership is null)
            return new HouseholdView(InHousehold: false);

        var members = await db.HouseholdMembers.AsNoTracking()
            .Where(m => m.HouseholdId == myMembership.HouseholdId)
            .Join(db.Users.AsNoTracking(), m => m.UserId, u => u.Id, (m, u) => new { Member = m, User = u })
            .ToListAsync(ct);

        return new HouseholdView(
            InHousehold: true,
            MyStatus: myMembership.Status,
            MyVisibility: myMembership.Visibility,
            Members: members.Select(x => x.Member.ToMemberView(x.User, userId)).ToList());
    }

    public async Task InviteAsync(Guid userId, string email, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var invitee = await db.Users.SingleOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (invitee is null)
            throw new DomainException("No OpenWealth user with that email.");
        if (invitee.Id == userId)
            throw new DomainException("You can't invite yourself.");

        var inviteeExisting = await db.HouseholdMembers.AnyAsync(m => m.UserId == invitee.Id, ct);
        if (inviteeExisting)
            throw new DomainException($"{invitee.DisplayName} is already in a household.");

        var myMembership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == userId, ct);
        if (myMembership is not null && myMembership.Status == HouseholdMemberStatus.Invited)
            throw new DomainException("Accept or decline your own pending invite first.");

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
        await db.SaveChangesAsync(ct);
    }

    public async Task AcceptAsync(Guid userId, CancellationToken ct = default)
    {
        var membership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == userId, ct);
        if (membership is null || membership.Status != HouseholdMemberStatus.Invited)
            throw new DomainException("No pending invite.");
        membership.Status = HouseholdMemberStatus.Active;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ShareVisibility> SetVisibilityAsync(Guid userId, ShareVisibility visibility, CancellationToken ct = default)
    {
        var membership = await db.HouseholdMembers.SingleOrDefaultAsync(
            m => m.UserId == userId && m.Status == HouseholdMemberStatus.Active, ct);
        if (membership is null)
            throw new DomainException("You're not an active household member.");
        membership.Visibility = visibility;
        await db.SaveChangesAsync(ct);
        return membership.Visibility;
    }

    /// <summary>
    /// Declining a pending invite and leaving an accepted household are the
    /// same operation from the data's point of view: remove my own row, and
    /// clean up the household if that leaves it empty. Returns false if the
    /// caller had no membership to remove.
    /// </summary>
    public async Task<bool> LeaveAsync(Guid userId, CancellationToken ct = default)
    {
        var membership = await db.HouseholdMembers.SingleOrDefaultAsync(m => m.UserId == userId, ct);
        if (membership is null) return false;

        var householdId = membership.HouseholdId;
        db.HouseholdMembers.Remove(membership);
        await db.SaveChangesAsync(ct);

        var remaining = await db.HouseholdMembers.AnyAsync(m => m.HouseholdId == householdId, ct);
        if (!remaining)
            await db.Households.Where(h => h.Id == householdId).ExecuteDeleteAsync(ct);

        return true;
    }

    public async Task<HouseholdSummaryView> BuildSummaryAsync(Guid userId, SummaryService summaries, CancellationToken ct = default)
    {
        var myMembership = await db.HouseholdMembers.AsNoTracking()
            .SingleOrDefaultAsync(m => m.UserId == userId && m.Status == HouseholdMemberStatus.Active, ct);
        if (myMembership is null)
            throw new DomainException("You're not an active household member.");

        var activeMembers = await db.HouseholdMembers.AsNoTracking()
            .Where(m => m.HouseholdId == myMembership.HouseholdId && m.Status == HouseholdMemberStatus.Active)
            .Join(db.Users.AsNoTracking(), m => m.UserId, u => u.Id,
                (m, u) => new { m.UserId, u.DisplayName, m.Visibility })
            .ToListAsync(ct);

        var views = new List<object>();
        var totalNetWorth = 0m;
        // Sequential by design: SummaryService shares this request's scoped
        // AppDbContext, which isn't safe for concurrent operations, and
        // households are small (a handful of members at most).
        foreach (var member in activeMembers)
        {
            var summary = await summaries.BuildAsync(member.UserId, ct);
            totalNetWorth += summary.NetWorth;
            views.Add(summary.ToShareView(member.DisplayName, member.Visibility));
        }

        return new HouseholdSummaryView(totalNetWorth, views);
    }
}
