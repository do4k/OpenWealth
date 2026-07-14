using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Simulates future paydays month by month using the same stepping rules the
/// accrual service applies for real, plus each item's optional expected
/// growth rate (investments, custom assets, properties, and — layered on top
/// of any real interest/payment — custom debts). Growth is projection-only:
/// the real recorded balances are never touched automatically.
/// </summary>
public static class ProjectionService
{
    public static List<ProjectionPoint> Project(User user, DateOnly from, int months)
    {
        // Work on a deep copy so simulating never mutates tracked entities.
        var sim = Clone(user);
        var payday = user.Income?.PaydayDayOfMonth ?? 1;
        var points = new List<ProjectionPoint> { ToPoint(MonthlyStepCalculator.TakeSnapshot(sim, from)) };

        var date = from;
        for (var i = 0; i < months; i++)
        {
            date = MonthlyStepCalculator.NextPayday(date, payday);
            MonthlyStepCalculator.ApplyMonthlyStep(sim, date);
            foreach (var investment in sim.Investments)
            {
                if (investment.ExpectedAnnualGrowthPercent is { } growth && growth != 0)
                    investment.CurrentValue =
                        (investment.CurrentValue * (1 + growth / 100m / 12m)).RoundToPence();
            }
            foreach (var asset in sim.CustomAssets)
            {
                if (asset.ExpectedAnnualGrowthPercent is { } growth && growth != 0)
                    asset.Value = Math.Max(0m, (asset.Value * (1 + growth / 100m / 12m)).RoundToPence());
            }
            foreach (var property in sim.Properties)
            {
                if (property.ExpectedAnnualGrowthPercent is { } growth && growth != 0)
                    property.EstimatedValue =
                        Math.Max(0m, (property.EstimatedValue * (1 + growth / 100m / 12m)).RoundToPence());
            }
            foreach (var debt in sim.CustomDebts)
            {
                if (debt.ExpectedAnnualGrowthPercent is { } growth && growth != 0)
                    debt.Balance = Math.Max(0m, (debt.Balance * (1 + growth / 100m / 12m)).RoundToPence());
            }
            points.Add(ToPoint(MonthlyStepCalculator.TakeSnapshot(sim, date)));
        }
        return points;
    }

    private static ProjectionPoint ToPoint(NetWorthSnapshot s) => new(
        s.Date, s.NetWorth, s.TotalAssets, s.TotalLiabilities,
        s.Property, s.Savings, s.Investments, s.OtherAssets,
        s.Mortgages, s.StudentLoans, s.OtherDebts);

    private static User Clone(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        PasswordHash = user.PasswordHash,
        Income = user.Income is null ? null : new IncomeDetails
        {
            AnnualSalary = user.Income.AnnualSalary,
            AnnualBonus = user.Income.AnnualBonus,
            EmployeePensionPercent = user.Income.EmployeePensionPercent,
            EmployerPensionPercent = user.Income.EmployerPensionPercent,
            PensionMethod = user.Income.PensionMethod,
            PensionOnBonus = user.Income.PensionOnBonus,
            ChildrenReceivingChildBenefit = user.Income.ChildrenReceivingChildBenefit,
            PaydayDayOfMonth = user.Income.PaydayDayOfMonth,
        },
        TaxSettings = user.TaxSettings,
        StudentLoanPlanSettings = user.StudentLoanPlanSettings,
        StudentLoans = user.StudentLoans.Select(l => new StudentLoan
        {
            Id = l.Id, Plan = l.Plan, Balance = l.Balance,
        }).ToList(),
        SavingsAccounts = user.SavingsAccounts.Select(s => new SavingsAccount
        {
            Id = s.Id, Name = s.Name, Type = s.Type, Balance = s.Balance,
            AnnualInterestRatePercent = s.AnnualInterestRatePercent,
            MonthlyDeposit = s.MonthlyDeposit,
        }).ToList(),
        Investments = user.Investments.Select(i => new Investment
        {
            Id = i.Id, Name = i.Name, Type = i.Type, CurrentValue = i.CurrentValue,
            ExpectedAnnualGrowthPercent = i.ExpectedAnnualGrowthPercent,
            ReceivesIncomePensionContributions = i.ReceivesIncomePensionContributions,
        }).ToList(),
        Mortgages = user.Mortgages.Select(m => new Mortgage
        {
            Id = m.Id, Name = m.Name, OutstandingBalance = m.OutstandingBalance,
            AnnualInterestRatePercent = m.AnnualInterestRatePercent, RateType = m.RateType,
            FixedRateEndDate = m.FixedRateEndDate, FollowOnRatePercent = m.FollowOnRatePercent,
            TermMonthsRemaining = m.TermMonthsRemaining,
            ReinvestDestinationType = m.ReinvestDestinationType, ReinvestDestinationId = m.ReinvestDestinationId,
            ReinvestMonthlyAmount = m.ReinvestMonthlyAmount,
        }).ToList(),
        Properties = user.Properties.Select(p => new Property
        {
            Id = p.Id, Name = p.Name, EstimatedValue = p.EstimatedValue,
            ExpectedAnnualGrowthPercent = p.ExpectedAnnualGrowthPercent,
        }).ToList(),
        CustomAssets = user.CustomAssets.Select(a => new CustomAsset
        {
            Id = a.Id, Name = a.Name, Value = a.Value,
            ExpectedAnnualGrowthPercent = a.ExpectedAnnualGrowthPercent,
        }).ToList(),
        CustomDebts = user.CustomDebts.Select(d => new CustomDebt
        {
            Id = d.Id, Name = d.Name, Balance = d.Balance,
            AnnualInterestRatePercent = d.AnnualInterestRatePercent, MonthlyPayment = d.MonthlyPayment,
            ExpectedAnnualGrowthPercent = d.ExpectedAnnualGrowthPercent,
            ReinvestDestinationType = d.ReinvestDestinationType, ReinvestDestinationId = d.ReinvestDestinationId,
            ReinvestMonthlyAmount = d.ReinvestMonthlyAmount,
        }).ToList(),
    };
}
