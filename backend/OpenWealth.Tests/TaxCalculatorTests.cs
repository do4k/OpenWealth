using OpenWealth.Api.Data;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Tests;

public class TaxCalculatorTests
{
    private static readonly TaxSettings Tax = UkDefaults.NewTaxSettings(Guid.NewGuid());
    private static readonly List<StudentLoanPlanSetting> Plans =
        UkDefaults.NewStudentLoanPlanSettings(Guid.NewGuid());

    private static TakeHomeBreakdown Calc(
        decimal salary,
        decimal bonus = 0,
        decimal pensionPercent = 0,
        PensionMethod method = PensionMethod.SalarySacrifice,
        params StudentLoanPlan[] loans)
    {
        var income = new IncomeDetails
        {
            AnnualSalary = salary,
            AnnualBonus = bonus,
            EmployeePensionPercent = pensionPercent,
            PensionMethod = method,
        };
        return TaxCalculator.Calculate(income, Tax, loans, Plans);
    }

    [Fact]
    public void NoTaxBelowPersonalAllowance()
    {
        var result = Calc(12_000m);
        Assert.Equal(0m, result.IncomeTax);
        Assert.Equal(0m, result.NationalInsurance);
        Assert.Equal(12_000m, result.AnnualTakeHome);
    }

    [Fact]
    public void BasicRateSalary_30k()
    {
        var result = Calc(30_000m);
        // Taxable 17,430 @ 20% = 3,486; NI 17,430 @ 8% = 1,394.40
        Assert.Equal(3_486m, result.IncomeTax);
        Assert.Equal(1_394.40m, result.NationalInsurance);
        Assert.Equal(30_000m - 3_486m - 1_394.40m, result.AnnualTakeHome);
    }

    [Fact]
    public void HigherRateSalary_60k()
    {
        var result = Calc(60_000m);
        // Taxable 47,430: 37,700 @ 20% + 9,730 @ 40% = 7,540 + 3,892 = 11,432
        Assert.Equal(11_432m, result.IncomeTax);
        // NI: 37,700 @ 8% + 9,730 @ 2% = 3,016 + 194.60 = 3,210.60
        Assert.Equal(3_210.60m, result.NationalInsurance);
    }

    [Fact]
    public void PersonalAllowanceTapersAbove100k()
    {
        // At 110,000 the allowance is reduced by 5,000 to 7,570
        Assert.Equal(7_570m, TaxCalculator.TaperedPersonalAllowance(110_000m, Tax));
        // Fully gone at 125,140
        Assert.Equal(0m, TaxCalculator.TaperedPersonalAllowance(125_140m, Tax));
    }

    [Fact]
    public void AdditionalRateSalary_150k()
    {
        var result = Calc(150_000m);
        Assert.Equal(0m, result.PersonalAllowance);
        // 37,700 @ 20% + 87,440 @ 40% + 24,860 @ 45% = 7,540 + 34,976 + 11,187 = 53,703
        Assert.Equal(53_703m, result.IncomeTax);
    }

    [Fact]
    public void Plan2LoanRepayment()
    {
        var result = Calc(40_000m, loans: StudentLoanPlan.Plan2);
        // (40,000 - 28,470) * 9% = 1,037.70
        Assert.Equal(1_037.70m, result.TotalStudentLoanRepayments);
    }

    [Fact]
    public void Plan5AndPostgradStackTogether()
    {
        var result = Calc(40_000m, loans: [StudentLoanPlan.Plan5, StudentLoanPlan.Postgraduate]);
        // Plan 5: (40,000 - 25,000) * 9% = 1,350; PG: (40,000 - 21,000) * 6% = 1,140
        Assert.Equal(2_490m, result.TotalStudentLoanRepayments);
        Assert.Equal(2, result.StudentLoanRepayments.Count);
    }

    [Fact]
    public void NoLoanRepaymentBelowThreshold()
    {
        var result = Calc(24_000m, loans: StudentLoanPlan.Plan5);
        Assert.Equal(0m, result.TotalStudentLoanRepayments);
    }

    [Fact]
    public void DuplicateLoanPlansOnlyChargedOnce()
    {
        var one = Calc(40_000m, loans: StudentLoanPlan.Plan2);
        var two = Calc(40_000m, loans: [StudentLoanPlan.Plan2, StudentLoanPlan.Plan2]);
        Assert.Equal(one.TotalStudentLoanRepayments, two.TotalStudentLoanRepayments);
    }

    [Fact]
    public void SalarySacrificeReducesTaxAndNi()
    {
        var without = Calc(50_000m);
        var with = Calc(50_000m, pensionPercent: 10m, method: PensionMethod.SalarySacrifice);
        Assert.Equal(5_000m, with.EmployeePensionContribution);
        Assert.True(with.IncomeTax < without.IncomeTax);
        Assert.True(with.NationalInsurance < without.NationalInsurance);
        // Pay for tax/NI is 45,000: taxable 32,430 @ 20% = 6,486; NI 32,430 @ 8% = 2,594.40
        Assert.Equal(6_486m, with.IncomeTax);
        Assert.Equal(2_594.40m, with.NationalInsurance);
    }

    [Fact]
    public void NetPayReducesTaxOnly()
    {
        var result = Calc(50_000m, pensionPercent: 10m, method: PensionMethod.NetPay);
        Assert.Equal(6_486m, result.IncomeTax);
        // NI is on the full 50,000: 37,430 @ 8% = 2,994.40... capped band:
        // main band 12,570→50,270 → 37,430 @ 8% = 2,994.40
        Assert.Equal(2_994.40m, result.NationalInsurance);
    }

    [Fact]
    public void ReliefAtSourceLeavesPayeUnchanged()
    {
        var without = Calc(50_000m);
        var with = Calc(50_000m, pensionPercent: 10m, method: PensionMethod.ReliefAtSource);
        Assert.Equal(without.IncomeTax, with.IncomeTax);
        Assert.Equal(without.NationalInsurance, with.NationalInsurance);
        // But take-home is lower because the contribution leaves net pay
        Assert.Equal(without.AnnualTakeHome - 5_000m, with.AnnualTakeHome);
    }

    [Fact]
    public void BonusIsIncludedInGross()
    {
        var result = Calc(50_000m, bonus: 10_000m);
        var flat = Calc(60_000m);
        Assert.Equal(flat.IncomeTax, result.IncomeTax);
        Assert.Equal(flat.NationalInsurance, result.NationalInsurance);
    }
}

public class MortgageCalculatorTests
{
    [Fact]
    public void StandardAmortisation()
    {
        // £200,000 at 5% over 25 years ≈ £1,169.18/month
        Assert.Equal(1_169.18m, MortgageCalculator.MonthlyPayment(200_000m, 5m, 300));
    }

    [Fact]
    public void ZeroRateIsStraightLine()
    {
        Assert.Equal(1_000m, MortgageCalculator.MonthlyPayment(120_000m, 0m, 120));
    }

    [Fact]
    public void FixedPeriodOverDetection()
    {
        var m = new Mortgage
        {
            Name = "Test",
            RateType = MortgageRateType.Fixed,
            FixedRateEndDate = new DateOnly(2026, 1, 1),
        };
        Assert.True(MortgageCalculator.IsFixedPeriodOver(m, new DateOnly(2026, 6, 1)));
        Assert.False(MortgageCalculator.IsFixedPeriodOver(m, new DateOnly(2025, 6, 1)));
    }
}
