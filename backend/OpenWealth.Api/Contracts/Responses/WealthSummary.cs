namespace OpenWealth.Api.Contracts.Responses;

public record WealthSummary(
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    List<CategoryTotal> AssetTotals,
    List<CategoryTotal> LiabilityTotals,
    List<NetWorthItem> Items,
    TakeHomeBreakdown? TakeHome);
