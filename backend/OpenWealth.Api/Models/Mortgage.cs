namespace OpenWealth.Api.Models;

public enum MortgageRateType
{
    Fixed = 0,
    Variable = 1,
}

public class Mortgage
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Optional link to a property so equity can be computed.</summary>
    public Guid? PropertyId { get; set; }
    public required string Name { get; set; }
    public decimal OutstandingBalance { get; set; }
    /// <summary>Interest rate is configured per mortgage, unlike student loans.</summary>
    public decimal AnnualInterestRatePercent { get; set; }
    public MortgageRateType RateType { get; set; }
    /// <summary>When the fixed deal ends and the mortgage rolls onto a variable rate.</summary>
    public DateOnly? FixedRateEndDate { get; set; }
    /// <summary>Expected rate after the fixed period ends (e.g. lender SVR), for planning.</summary>
    public decimal? FollowOnRatePercent { get; set; }
    public int TermMonthsRemaining { get; set; }

    /// <summary>
    /// Once this mortgage is fully paid off, payday automation redirects
    /// <see cref="ReinvestMonthlyAmount"/> into this destination every month
    /// instead of the payment just disappearing. Starts the payday after the
    /// balance reaches zero, never in the same month as the final payment.
    /// </summary>
    public ReinvestDestinationType ReinvestDestinationType { get; set; } = ReinvestDestinationType.None;
    /// <summary>Id of the SavingsAccount or Investment to reinvest into, matching <see cref="ReinvestDestinationType"/>.</summary>
    public Guid? ReinvestDestinationId { get; set; }
    /// <summary>
    /// Fixed monthly amount to redirect once paid off — set explicitly rather than
    /// reusing the amortised payment, since that figure stops being meaningful
    /// once the balance is zero.
    /// </summary>
    public decimal? ReinvestMonthlyAmount { get; set; }
}
