namespace OpenWealth.Api.Contracts.Responses;

/// <summary>
/// Common shape shared by recorded history (<see cref="OpenWealth.Api.Models.NetWorthSnapshot"/>)
/// and simulated projections (<see cref="ProjectionPoint"/>), so both can be
/// shaped the same way for the public profile.
/// </summary>
public record TrendPoint(
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
    decimal OtherDebts);
