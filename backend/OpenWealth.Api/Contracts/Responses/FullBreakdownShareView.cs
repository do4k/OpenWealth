using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.FullBreakdown"/> tier of <see cref="WealthSummaryExtensions.ToShareView"/>.</summary>
public record FullBreakdownShareView(
    string DisplayName,
    ShareVisibility Visibility,
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    List<CategoryTotal> AssetTotals,
    List<CategoryTotal> LiabilityTotals,
    List<NetWorthItem> Items)
    : CategoryTotalsShareView(DisplayName, Visibility, NetWorth, TotalAssets, TotalLiabilities, AssetTotals, LiabilityTotals);
