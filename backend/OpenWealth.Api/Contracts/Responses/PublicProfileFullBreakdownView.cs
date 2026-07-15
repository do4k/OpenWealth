using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.FullBreakdown"/> tier of the public profile response.</summary>
public record PublicProfileFullBreakdownView(
    string DisplayName, ShareVisibility Visibility, decimal NetWorth,
    decimal TotalAssets, decimal TotalLiabilities, List<CategoryTotal> AssetTotals, List<CategoryTotal> LiabilityTotals,
    List<NetWorthItem> Items,
    IEnumerable<TrendPointTierView> History, IEnumerable<TrendPointTierView> Projection)
    : FullBreakdownShareView(DisplayName, Visibility, NetWorth, TotalAssets, TotalLiabilities, AssetTotals, LiabilityTotals, Items);
