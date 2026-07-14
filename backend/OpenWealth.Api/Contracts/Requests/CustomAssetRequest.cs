namespace OpenWealth.Api.Contracts.Requests;

public record CustomAssetRequest(string Name, decimal Value, decimal? ExpectedAnnualGrowthPercent);
