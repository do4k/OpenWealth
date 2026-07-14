namespace OpenWealth.Api.Models;

/// <summary>
/// Which wealth figure a <see cref="Goal"/> tracks — matching the fields
/// already recorded on every <see cref="NetWorthSnapshot"/> and projected
/// point, so goal progress reuses the exact same numbers shown on Trends.
/// </summary>
public enum GoalMetric
{
    NetWorth = 0,
    Savings = 1,
    Investments = 2,
    TotalAssets = 3,
    /// <summary>Lower is better — e.g. a target of £0 for "debt-free by date".</summary>
    TotalLiabilities = 4,
}
