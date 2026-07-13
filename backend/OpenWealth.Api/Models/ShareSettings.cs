namespace OpenWealth.Api.Models;

public enum ShareVisibility
{
    /// <summary>Only the headline net worth figure.</summary>
    NetWorthOnly = 0,
    /// <summary>Net worth plus totals per category (property, savings, etc.).</summary>
    CategoryTotals = 1,
    /// <summary>Every individual item, as the owner sees it.</summary>
    FullBreakdown = 2,
}

/// <summary>
/// Opt-in public profile. Only the account owner can enable sharing of their own
/// data — profiles exist solely for authenticated users, so wealth data is always
/// owned by the person it describes.
/// </summary>
public class ShareSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool Enabled { get; set; }
    /// <summary>URL-safe random identifier for the public link.</summary>
    public required string Slug { get; set; }
    public string? PassphraseHash { get; set; }
    public ShareVisibility Visibility { get; set; } = ShareVisibility.NetWorthOnly;
}
