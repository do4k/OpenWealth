namespace OpenWealth.Api.Contracts.Responses;

public record ProjectionPoint(
    DateOnly Date,
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal Property,
    decimal Savings,
    decimal Investments,
    decimal Mortgages,
    decimal StudentLoans);
