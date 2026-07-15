using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.CategoryTotals"/> tier of the public profile response.</summary>
public record PublicProfileCategoryTotalsView(
    string DisplayName, ShareVisibility Visibility, decimal NetWorth,
    decimal TotalAssets, decimal TotalLiabilities, List<CategoryTotal> AssetTotals, List<CategoryTotal> LiabilityTotals,
    IEnumerable<object> History, IEnumerable<object> Projection)
    : CategoryTotalsShareView(DisplayName, Visibility, NetWorth, TotalAssets, TotalLiabilities, AssetTotals, LiabilityTotals);
