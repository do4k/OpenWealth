namespace OpenWealth.Api.Models;

/// <summary>
/// A liability that isn't a mortgage or student loan — a credit card, car
/// finance, a personal loan. With a rate and monthly payment set, the payday
/// automation accrues interest and applies the payment each month, and
/// projections show the balance amortising down.
/// </summary>
public class CustomDebt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Balance { get; set; }
    public decimal? AnnualInterestRatePercent { get; set; }
    public decimal? MonthlyPayment { get; set; }

    /// <summary>
    /// An additional expected trend applied only in projections, on top of
    /// whatever <see cref="AnnualInterestRatePercent"/> and
    /// <see cref="MonthlyPayment"/> already do to the balance each month — for
    /// modelling a debt you expect to keep growing beyond its stated rate
    /// (e.g. a credit card you expect to keep spending on). Positive grows the
    /// balance faster, negative slower; <see cref="Balance"/> itself is never
    /// changed automatically.
    /// </summary>
    public decimal? ExpectedAnnualGrowthPercent { get; set; }

    /// <summary>
    /// Once this debt is fully paid off, payday automation redirects
    /// <see cref="ReinvestMonthlyAmount"/> into this destination every month
    /// instead of the payment just disappearing. Starts the payday after the
    /// balance reaches zero, never in the same month as the final payment.
    /// </summary>
    public ReinvestDestinationType ReinvestDestinationType { get; set; } = ReinvestDestinationType.None;
    /// <summary>Id of the SavingsAccount or Investment to reinvest into, matching <see cref="ReinvestDestinationType"/>.</summary>
    public Guid? ReinvestDestinationId { get; set; }
    /// <summary>Fixed monthly amount to redirect once paid off — set explicitly, not tied to <see cref="MonthlyPayment"/>.</summary>
    public decimal? ReinvestMonthlyAmount { get; set; }
}
