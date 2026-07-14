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
}
