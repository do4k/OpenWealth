namespace OpenWealth.Api.Models;

/// <summary>How employee pension contributions are taken, which changes tax/NI treatment.</summary>
public enum PensionMethod
{
    /// <summary>Salary is reduced before tax and NI.</summary>
    SalarySacrifice = 0,
    /// <summary>Deducted from gross pay before tax but after NI.</summary>
    NetPay = 1,
    /// <summary>Deducted from net pay; provider reclaims basic-rate relief.</summary>
    ReliefAtSource = 2,
}

public class IncomeDetails
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal AnnualSalary { get; set; }
    public decimal AnnualBonus { get; set; }
    public decimal EmployeePensionPercent { get; set; }
    public decimal EmployerPensionPercent { get; set; }
    public PensionMethod PensionMethod { get; set; } = PensionMethod.SalarySacrifice;
    /// <summary>Whether pension percentages also apply to the bonus.</summary>
    public bool PensionOnBonus { get; set; }
    /// <summary>Number of children child benefit is claimed for; drives the HICBC calculation.</summary>
    public int ChildrenReceivingChildBenefit { get; set; }

    /// <summary>Day of the month interest and repayments are applied (clamped in short months).</summary>
    public int PaydayDayOfMonth { get; set; } = 1;
    /// <summary>Opt-in switch for the automatic payday accrual service.</summary>
    public bool AutomationEnabled { get; set; }
    /// <summary>Date the accrual service last ran for this user; paydays after this are due.</summary>
    public DateOnly? LastAccrualDate { get; set; }
}
