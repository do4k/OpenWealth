using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.FullBreakdown"/> tier of <see cref="TrendPointExtensions.ToShareView"/>.</summary>
public record TrendPointFullBreakdownView(
    DateOnly Date,
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal Property,
    decimal Savings,
    decimal Investments,
    decimal OtherAssets,
    decimal Mortgages,
    decimal StudentLoans,
    decimal OtherDebts)
    : TrendPointCategoryTotalsView(Date, NetWorth, TotalAssets, TotalLiabilities);
