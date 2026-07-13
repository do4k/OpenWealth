using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

public record HouseholdMember(Guid LinkId, string DisplayName, string Email, DateTime LinkedAtUtc);
public record HouseholdInvite(Guid LinkId, string DisplayName, string Email, DateTime SentAtUtc);
public record HouseholdView(
    List<HouseholdMember> Members,
    List<HouseholdInvite> InvitesSent,
    List<HouseholdInvite> InvitesReceived);

public class HouseholdError(string message) : Exception(message);

/// <summary>
/// Consent-based account linking. Invites can only target accounts that
/// already exist (no shadow profiles), require an explicit accept from the
/// invitee, and either member can dissolve the link at any time.
/// </summary>
public class HouseholdService(AppDbContext db)
{
    public async Task<HouseholdView> GetViewAsync(Guid userId)
    {
        var links = await db.HouseholdLinks.AsNoTracking()
            .Where(l => l.InviterUserId == userId || l.InviteeUserId == userId)
            .ToListAsync();
        var otherIds = links.Select(l => OtherSide(l, userId)).Distinct().ToList();
        var others = await db.Users.AsNoTracking()
            .Where(u => otherIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        var members = new List<HouseholdMember>();
        var sent = new List<HouseholdInvite>();
        var received = new List<HouseholdInvite>();
        foreach (var link in links)
        {
            if (!others.TryGetValue(OtherSide(link, userId), out var other))
                continue;
            if (link.Status == HouseholdLinkStatus.Accepted)
                members.Add(new HouseholdMember(
                    link.Id, other.DisplayName, other.Email, link.RespondedAtUtc ?? link.CreatedAtUtc));
            else if (link.InviterUserId == userId)
                sent.Add(new HouseholdInvite(link.Id, other.DisplayName, other.Email, link.CreatedAtUtc));
            else
                received.Add(new HouseholdInvite(link.Id, other.DisplayName, other.Email, link.CreatedAtUtc));
        }
        return new HouseholdView(members, sent, received);
    }

    public async Task<HouseholdLink> InviteAsync(Guid inviterId, string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        var invitee = await db.Users.SingleOrDefaultAsync(u => u.Email == normalized)
            ?? throw new HouseholdError(
                "No account with that email. They need to create their own account first — " +
                "household data is always owned by the person it describes.");
        if (invitee.Id == inviterId)
            throw new HouseholdError("You can't invite yourself.");

        var existing = await db.HouseholdLinks.SingleOrDefaultAsync(l =>
            (l.InviterUserId == inviterId && l.InviteeUserId == invitee.Id) ||
            (l.InviterUserId == invitee.Id && l.InviteeUserId == inviterId));
        if (existing is not null)
            throw new HouseholdError(existing.Status == HouseholdLinkStatus.Accepted
                ? "You're already linked with that person."
                : "There's already a pending invite between you.");

        var link = new HouseholdLink
        {
            Id = Guid.NewGuid(),
            InviterUserId = inviterId,
            InviteeUserId = invitee.Id,
            Status = HouseholdLinkStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.HouseholdLinks.Add(link);
        await db.SaveChangesAsync();
        return link;
    }

    public async Task RespondAsync(Guid userId, Guid linkId, bool accept)
    {
        // Only the invitee can respond — the inviter cannot accept on their behalf.
        var link = await db.HouseholdLinks.SingleOrDefaultAsync(l =>
                l.Id == linkId && l.InviteeUserId == userId && l.Status == HouseholdLinkStatus.Pending)
            ?? throw new HouseholdError("Invite not found.");
        if (accept)
        {
            link.Status = HouseholdLinkStatus.Accepted;
            link.RespondedAtUtc = DateTime.UtcNow;
        }
        else
        {
            db.HouseholdLinks.Remove(link);
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Removes a link (or withdraws a pending invite). Either side may do this.</summary>
    public async Task UnlinkAsync(Guid userId, Guid linkId)
    {
        var link = await db.HouseholdLinks.SingleOrDefaultAsync(l =>
                l.Id == linkId && (l.InviterUserId == userId || l.InviteeUserId == userId))
            ?? throw new HouseholdError("Link not found.");
        db.HouseholdLinks.Remove(link);
        await db.SaveChangesAsync();
    }

    /// <summary>User ids of everyone in an accepted link with this user.</summary>
    public async Task<List<Guid>> AcceptedPartnerIdsAsync(Guid userId) =>
        await db.HouseholdLinks.AsNoTracking()
            .Where(l => l.Status == HouseholdLinkStatus.Accepted &&
                        (l.InviterUserId == userId || l.InviteeUserId == userId))
            .Select(l => l.InviterUserId == userId ? l.InviteeUserId : l.InviterUserId)
            .Distinct()
            .ToListAsync();

    private static Guid OtherSide(HouseholdLink link, Guid userId) =>
        link.InviterUserId == userId ? link.InviteeUserId : link.InviterUserId;
}
