using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

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
        var employeePension = (pensionablePay * income.EmployeePensionPercent / 100m).RoundToPence();
        var employerPension = (pensionablePay * income.EmployerPensionPercent / 100m).RoundToPence();

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

        // Adjusted net income is what HMRC uses for the personal allowance
        // taper, the £100k childcare cliff and the child benefit charge.
        // Salary sacrifice and net-pay contributions reduce it directly;
        // relief-at-source contributions are deducted grossed-up by basic-rate
        // relief (the provider adds 25p per 80p contributed).
        var adjustedNetIncome = income.PensionMethod switch
        {
            PensionMethod.ReliefAtSource when tax.BasicRatePercent < 100m =>
                gross - employeePension * 100m / (100m - tax.BasicRatePercent),
            PensionMethod.ReliefAtSource => gross,
            _ => gross - employeePension,
        };
        adjustedNetIncome = Math.Max(0m, adjustedNetIncome.RoundToPence());

        var personalAllowance = TaperedPersonalAllowance(adjustedNetIncome, tax);
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
                return new StudentLoanRepayment(plan, (over * s.RepaymentRatePercent / 100m).RoundToPence());
            })
            .Where(r => r.AnnualRepayment > 0)
            .ToList();
        var totalLoanRepayments = loanRepayments.Sum(r => r.AnnualRepayment);

        var takeHome = gross - employeePension - incomeTax - nationalInsurance - totalLoanRepayments;

        return new TakeHomeBreakdown(
            GrossIncome: gross,
            EmployeePensionContribution: employeePension,
            EmployerPensionContribution: employerPension,
            AdjustedNetIncome: adjustedNetIncome,
            PersonalAllowance: personalAllowance,
            TaxableIncome: taxableIncome,
            IncomeTax: incomeTax,
            NationalInsurance: nationalInsurance,
            StudentLoanRepayments: loanRepayments,
            TotalStudentLoanRepayments: totalLoanRepayments,
            AnnualTakeHome: takeHome.RoundToPence(),
            MonthlyTakeHome: (takeHome / 12m).RoundToPence(),
            FamilyBenefits: CalculateFamilyBenefits(adjustedNetIncome, income.ChildrenReceivingChildBenefit, tax));
    }

    public static FamilyBenefits CalculateFamilyBenefits(decimal adjustedNetIncome, int children, TaxSettings tax)
    {
        var benefit = children <= 0
            ? 0m
            : ((tax.ChildBenefitFirstChildWeekly +
                tax.ChildBenefitAdditionalChildWeekly * (children - 1)) * 52m).RoundToPence();

        // High Income Child Benefit Charge: 1% of the benefit is clawed back
        // per step of adjusted net income above the lower threshold, reaching
        // 100% at the upper threshold (steps of £200 with the 2024+ £60k–£80k
        // band). The step size derives from the configured band so the maths
        // stays right if the thresholds change.
        var hicbcPercent = 0m;
        var band = tax.HicbcUpperThreshold - tax.HicbcLowerThreshold;
        if (band > 0 && adjustedNetIncome > tax.HicbcLowerThreshold)
        {
            var step = band / 100m;
            hicbcPercent = Math.Min(100m, Math.Floor((adjustedNetIncome - tax.HicbcLowerThreshold) / step));
        }
        var charge = (benefit * hicbcPercent / 100m).RoundToPence();

        return new FamilyBenefits(
            AdjustedNetIncome: adjustedNetIncome,
            ChildcareIncomeLimit: tax.ChildcareIncomeLimit,
            LosesFreeChildcare: adjustedNetIncome > tax.ChildcareIncomeLimit,
            ChildcareHeadroom: (tax.ChildcareIncomeLimit - adjustedNetIncome).RoundToPence(),
            ChildrenReceivingChildBenefit: Math.Max(0, children),
            AnnualChildBenefit: benefit,
            HicbcPercent: hicbcPercent,
            HicbcCharge: charge,
            NetChildBenefit: (benefit - charge).RoundToPence());
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
        return (basic * tax.BasicRatePercent / 100m +
                higher * tax.HigherRatePercent / 100m +
                additional * tax.AdditionalRatePercent / 100m).RoundToPence();
    }

    public static decimal NationalInsurance(decimal earnings, TaxSettings tax)
    {
        var main = Math.Clamp(earnings - tax.NiPrimaryThresholdAnnual, 0m,
            tax.NiUpperEarningsLimitAnnual - tax.NiPrimaryThresholdAnnual);
        var upper = Math.Max(0m, earnings - tax.NiUpperEarningsLimitAnnual);
        return (main * tax.NiMainRatePercent / 100m + upper * tax.NiUpperRatePercent / 100m).RoundToPence();
    }
}
