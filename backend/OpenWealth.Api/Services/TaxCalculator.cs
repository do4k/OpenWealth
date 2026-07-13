using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

public record StudentLoanRepayment(StudentLoanPlan Plan, decimal AnnualRepayment);

public record TakeHomeBreakdown(
    decimal GrossIncome,
    decimal EmployeePensionContribution,
    decimal EmployerPensionContribution,
    decimal PersonalAllowance,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal NationalInsurance,
    List<StudentLoanRepayment> StudentLoanRepayments,
    decimal TotalStudentLoanRepayments,
    decimal AnnualTakeHome,
    decimal MonthlyTakeHome);

/// <summary>
/// Annualised UK PAYE calculation: income tax (with personal allowance taper),
/// employee Class 1 National Insurance, and student loan repayments. All rates
/// and thresholds come from the user's editable settings rather than being
/// hard-coded.
/// </summary>
public static class TaxCalculator
{
    public static TakeHomeBreakdown Calculate(
        IncomeDetails income,
        TaxSettings tax,
        IEnumerable<StudentLoanPlan> activeLoanPlans,
        IReadOnlyList<StudentLoanPlanSetting> planSettings)
    {
        var gross = income.AnnualSalary + income.AnnualBonus;

        var pensionablePay = income.PensionOnBonus ? gross : income.AnnualSalary;
        var employeePension = Round2(pensionablePay * income.EmployeePensionPercent / 100m);
        var employerPension = Round2(pensionablePay * income.EmployerPensionPercent / 100m);

        // Salary sacrifice reduces pay before both tax and NI. Net-pay
        // arrangements reduce taxable pay only. Relief-at-source contributions
        // come out of net pay (basic-rate relief is added inside the pension,
        // so PAYE deductions are unaffected).
        var payForTax = income.PensionMethod switch
        {
            PensionMethod.SalarySacrifice or PensionMethod.NetPay => gross - employeePension,
            _ => gross,
        };
        var payForNi = income.PensionMethod == PensionMethod.SalarySacrifice
            ? gross - employeePension
            : gross;

        var personalAllowance = TaperedPersonalAllowance(payForTax, tax);
        var taxableIncome = Math.Max(0m, payForTax - personalAllowance);
        var incomeTax = IncomeTax(taxableIncome, tax);
        var nationalInsurance = NationalInsurance(payForNi, tax);

        // Student loan deductions are based on NI-able earnings. Each distinct
        // plan the user holds is charged over its own threshold.
        var settingsByPlan = planSettings.ToDictionary(s => s.Plan);
        var loanRepayments = activeLoanPlans.Distinct()
            .Where(settingsByPlan.ContainsKey)
            .Select(plan =>
            {
                var s = settingsByPlan[plan];
                var over = Math.Max(0m, payForNi - s.AnnualRepaymentThreshold);
                return new StudentLoanRepayment(plan, Round2(over * s.RepaymentRatePercent / 100m));
            })
            .Where(r => r.AnnualRepayment > 0)
            .ToList();
        var totalLoanRepayments = loanRepayments.Sum(r => r.AnnualRepayment);

        var takeHome = gross - employeePension - incomeTax - nationalInsurance - totalLoanRepayments;

        return new TakeHomeBreakdown(
            GrossIncome: gross,
            EmployeePensionContribution: employeePension,
            EmployerPensionContribution: employerPension,
            PersonalAllowance: personalAllowance,
            TaxableIncome: taxableIncome,
            IncomeTax: incomeTax,
            NationalInsurance: nationalInsurance,
            StudentLoanRepayments: loanRepayments,
            TotalStudentLoanRepayments: totalLoanRepayments,
            AnnualTakeHome: Round2(takeHome),
            MonthlyTakeHome: Round2(takeHome / 12m));
    }

    /// <summary>£1 of allowance is lost for every £2 of income over the taper threshold.</summary>
    public static decimal TaperedPersonalAllowance(decimal income, TaxSettings tax)
    {
        if (income <= tax.PersonalAllowanceTaperThreshold)
            return tax.PersonalAllowance;
        var reduction = (income - tax.PersonalAllowanceTaperThreshold) / 2m;
        return Math.Max(0m, tax.PersonalAllowance - reduction);
    }

    public static decimal IncomeTax(decimal taxableIncome, TaxSettings tax)
    {
        var basic = Math.Min(taxableIncome, tax.BasicRateLimit);
        var higher = Math.Clamp(taxableIncome - tax.BasicRateLimit, 0m, tax.HigherRateLimit - tax.BasicRateLimit);
        var additional = Math.Max(0m, taxableIncome - tax.HigherRateLimit);
        return Round2(
            basic * tax.BasicRatePercent / 100m +
            higher * tax.HigherRatePercent / 100m +
            additional * tax.AdditionalRatePercent / 100m);
    }

    public static decimal NationalInsurance(decimal earnings, TaxSettings tax)
    {
        var main = Math.Clamp(earnings - tax.NiPrimaryThresholdAnnual, 0m,
            tax.NiUpperEarningsLimitAnnual - tax.NiPrimaryThresholdAnnual);
        var upper = Math.Max(0m, earnings - tax.NiUpperEarningsLimitAnnual);
        return Round2(main * tax.NiMainRatePercent / 100m + upper * tax.NiUpperRatePercent / 100m);
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
