using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record MortgageRequest(
    string Name,
    Guid? PropertyId,
    decimal OutstandingBalance,
    decimal AnnualInterestRatePercent,
    MortgageRateType RateType,
    DateOnly? FixedRateEndDate,
    decimal? FollowOnRatePercent,
    int TermMonthsRemaining,
    ReinvestDestinationType ReinvestDestinationType = ReinvestDestinationType.None,
    Guid? ReinvestDestinationId = null,
    decimal? ReinvestMonthlyAmount = null);
