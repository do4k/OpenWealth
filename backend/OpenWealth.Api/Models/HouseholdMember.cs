namespace OpenWealth.Api.Models;

/// <summary>
/// One user's link to a household — at most one row per user at any time
/// (enforced by a unique index on UserId), whether the invite is still
/// pending or already accepted. Invites only ever target an existing
/// registered user found by email; there is no way to create a placeholder
/// row for someone without an account.
/// </summary>
public class HouseholdMember
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid UserId { get; set; }
    public HouseholdMemberStatus Status { get; set; } = HouseholdMemberStatus.Invited;

    /// <summary>
    /// What this member discloses to the rest of the household — separate
    /// from (and can differ from) their public Sharing visibility.
    /// </summary>
    public ShareVisibility Visibility { get; set; } = ShareVisibility.NetWorthOnly;

    public Guid InvitedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
