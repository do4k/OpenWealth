using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.CategoryTotals"/> tier of <see cref="WealthSummaryExtensions.ToShareView"/>.</summary>
public record CategoryTotalsShareView(
    string DisplayName,
    ShareVisibility Visibility,
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    List<CategoryTotal> AssetTotals,
    List<CategoryTotal> LiabilityTotals)
    : NetWorthOnlyShareView(DisplayName, Visibility, NetWorth);
