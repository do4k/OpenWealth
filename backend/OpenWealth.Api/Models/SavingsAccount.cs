namespace OpenWealth.Api.Models;

public enum SavingsAccountType
{
    CurrentAccount = 0,
    EasyAccess = 1,
    FixedTerm = 2,
    CashIsa = 3,
    PremiumBonds = 4,
    Other = 5,
}

public class SavingsAccount
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public SavingsAccountType Type { get; set; }
    public decimal Balance { get; set; }
    public decimal? AnnualInterestRatePercent { get; set; }
    /// <summary>
    /// Standing order added to the balance every payday by the accrual service
    /// and by projections. Negative values model a regular withdrawal.
    /// </summary>
    public decimal MonthlyDeposit { get; set; }
}
