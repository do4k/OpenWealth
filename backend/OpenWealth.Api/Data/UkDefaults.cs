using OpenWealth.Api.Models;

namespace OpenWealth.Api.Data;

/// <summary>
/// Default UK figures a new account is seeded with. All of these are editable
/// per user afterwards, so they can be refreshed each tax year without a
/// code change. Values are for the 2025/26 tax year.
/// </summary>
public static class UkDefaults
{
    public static TaxSettings NewTaxSettings(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TaxYearLabel = "2025/26",
        PersonalAllowance = 12_570m,
        PersonalAllowanceTaperThreshold = 100_000m,
        BasicRateLimit = 37_700m,
        HigherRateLimit = 125_140m,
        BasicRatePercent = 20m,
        HigherRatePercent = 40m,
        AdditionalRatePercent = 45m,
        NiPrimaryThresholdAnnual = 12_570m,
        NiUpperEarningsLimitAnnual = 50_270m,
        NiMainRatePercent = 8m,
        NiUpperRatePercent = 2m,
    };

    public static List<StudentLoanPlanSetting> NewStudentLoanPlanSettings(Guid userId) =>
    [
        NewPlan(userId, StudentLoanPlan.Plan1, 26_065m, 9m, 4.3m),
        NewPlan(userId, StudentLoanPlan.Plan2, 28_470m, 9m, 7.3m),
        NewPlan(userId, StudentLoanPlan.Plan4, 32_745m, 9m, 4.3m),
        NewPlan(userId, StudentLoanPlan.Plan5, 25_000m, 9m, 4.3m),
        NewPlan(userId, StudentLoanPlan.Postgraduate, 21_000m, 6m, 7.3m),
    ];

    private static StudentLoanPlanSetting NewPlan(
        Guid userId, StudentLoanPlan plan, decimal threshold, decimal repaymentRate, decimal interestRate) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Plan = plan,
        AnnualRepaymentThreshold = threshold,
        RepaymentRatePercent = repaymentRate,
        InterestRatePercent = interestRate,
    };
}
