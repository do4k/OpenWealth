using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Extensions;

public static class MortgageExtensions
{
    /// <summary>Shapes a mortgage for API responses, adding computed payment/status fields.</summary>
    public static object ToResponse(this Mortgage m) => new
    {
        m.Id,
        m.Name,
        m.PropertyId,
        m.OutstandingBalance,
        m.AnnualInterestRatePercent,
        m.RateType,
        m.FixedRateEndDate,
        m.FollowOnRatePercent,
        m.TermMonthsRemaining,
        m.ReinvestDestinationType,
        m.ReinvestDestinationId,
        m.ReinvestMonthlyAmount,
        MonthlyPayment = MortgageCalculator.MonthlyPayment(m),
        IsFixedPeriodOver = MortgageCalculator.IsFixedPeriodOver(m, DateOnly.FromDateTime(DateTime.UtcNow)),
        IsPaidOff = m.OutstandingBalance <= 0,
    };
}
