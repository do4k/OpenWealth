using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

public record MortgageResponse(
    Guid Id,
    string Name,
    Guid? PropertyId,
    decimal OutstandingBalance,
    decimal AnnualInterestRatePercent,
    MortgageRateType RateType,
    DateOnly? FixedRateEndDate,
    decimal? FollowOnRatePercent,
    int TermMonthsRemaining,
    ReinvestDestinationType ReinvestDestinationType,
    Guid? ReinvestDestinationId,
    decimal? ReinvestMonthlyAmount,
    decimal MonthlyPayment,
    bool IsFixedPeriodOver,
    bool IsPaidOff);
