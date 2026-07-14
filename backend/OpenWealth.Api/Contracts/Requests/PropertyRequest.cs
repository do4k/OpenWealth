namespace OpenWealth.Api.Contracts.Requests;

public record PropertyRequest(
    string Name,
    decimal EstimatedValue,
    decimal? ExpectedAnnualGrowthPercent = null);
