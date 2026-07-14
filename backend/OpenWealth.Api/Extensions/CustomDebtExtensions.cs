using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class CustomDebtExtensions
{
    /// <summary>Shapes a custom debt for API responses, adding the computed payoff flag.</summary>
    public static object ToResponse(this CustomDebt d) => new
    {
        d.Id,
        d.Name,
        d.Balance,
        d.AnnualInterestRatePercent,
        d.MonthlyPayment,
        d.ExpectedAnnualGrowthPercent,
        d.ReinvestDestinationType,
        d.ReinvestDestinationId,
        d.ReinvestMonthlyAmount,
        IsPaidOff = d.Balance <= 0,
    };
}
