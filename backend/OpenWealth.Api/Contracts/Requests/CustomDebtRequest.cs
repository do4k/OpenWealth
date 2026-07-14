using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record CustomDebtRequest(
    string Name,
    decimal Balance,
    decimal? AnnualInterestRatePercent,
    decimal? MonthlyPayment,
    decimal? ExpectedAnnualGrowthPercent = null,
    ReinvestDestinationType ReinvestDestinationType = ReinvestDestinationType.None,
    Guid? ReinvestDestinationId = null,
    decimal? ReinvestMonthlyAmount = null);
