namespace OpenWealth.Api.Models;

public enum StudentLoanPlan
{
    Plan1 = 1,
    Plan2 = 2,
    Plan4 = 4,
    Plan5 = 5,
    Postgraduate = 100,
}

public class StudentLoan
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public StudentLoanPlan Plan { get; set; }
    public decimal Balance { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Per-user, per-plan configuration. Interest rate is configured globally here
/// (it applies to every loan of that plan), alongside the repayment threshold
/// and rate used by the take-home calculation. Seeded with current UK values
/// and editable so figures can be updated each tax year.
/// </summary>
public class StudentLoanPlanSetting
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public StudentLoanPlan Plan { get; set; }
    public decimal AnnualRepaymentThreshold { get; set; }
    public decimal RepaymentRatePercent { get; set; }
    public decimal InterestRatePercent { get; set; }
}
