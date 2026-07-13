namespace OpenWealth.Api.Models;

public enum HouseholdLinkStatus
{
    Pending = 0,
    Accepted = 1,
}

/// <summary>
/// A consent-based link between two existing accounts. Both sides keep full
/// ownership of their own data — a link only grants each member a read-only
/// view of the other's net worth and category totals, and either side can
/// revoke it at any time. Invites can only target accounts that already
/// exist, so a household can never contain a shadow profile.
/// </summary>
public class HouseholdLink
{
    public Guid Id { get; set; }
    public Guid InviterUserId { get; set; }
    public Guid InviteeUserId { get; set; }
    public HouseholdLinkStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RespondedAtUtc { get; set; }
}
