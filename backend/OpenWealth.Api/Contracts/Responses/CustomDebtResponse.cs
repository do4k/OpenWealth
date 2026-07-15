using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

public record CustomDebtResponse(
    Guid Id,
    string Name,
    decimal Balance,
    decimal? AnnualInterestRatePercent,
    decimal? MonthlyPayment,
    decimal? ExpectedAnnualGrowthPercent,
    ReinvestDestinationType ReinvestDestinationType,
    Guid? ReinvestDestinationId,
    decimal? ReinvestMonthlyAmount,
    bool IsPaidOff);
