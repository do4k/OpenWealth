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
}
