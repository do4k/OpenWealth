using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record InvestmentRequest(
    string Name, InvestmentType Type, decimal CurrentValue, decimal? ExpectedAnnualGrowthPercent);
