using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.CategoryTotals"/> tier of <see cref="TrendPointExtensions.ToShareView"/>.</summary>
public record TrendPointCategoryTotalsView(DateOnly Date, decimal NetWorth, decimal TotalAssets, decimal TotalLiabilities)
    : TrendPointNetWorthOnlyView(Date, NetWorth);
