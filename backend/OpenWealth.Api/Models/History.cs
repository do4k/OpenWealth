namespace OpenWealth.Api.Models;

/// <summary>
/// Point-in-time record of a user's wealth, written whenever a payday accrual
/// runs (and when automation is first enabled). These form the historic series
/// on the trends chart.
/// </summary>
public class NetWorthSnapshot
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal NetWorth { get; set; }
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal Property { get; set; }
    public decimal Savings { get; set; }
    public decimal Investments { get; set; }
    public decimal Mortgages { get; set; }
    public decimal StudentLoans { get; set; }
}

/// <summary>
/// Audit record of one balance change made by the payday automation, so users
/// can see exactly what was applied and when.
/// </summary>
public class AccrualEvent
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string Category { get; set; }
    public required string ItemName { get; set; }
    public decimal InterestAmount { get; set; }
    public decimal PaymentAmount { get; set; }
    public decimal NewBalance { get; set; }
}
