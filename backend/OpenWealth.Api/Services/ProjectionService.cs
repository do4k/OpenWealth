using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

public record ProjectionPoint(
    DateOnly Date,
    decimal NetWorth,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal Property,
    decimal Savings,
    decimal Investments,
    decimal Mortgages,
    decimal StudentLoans);

/// <summary>
/// Simulates future paydays month by month using the same stepping rules the
/// accrual service applies for real, plus optional expected growth on
/// investments (projection-only — real investment balances are never touched).
/// </summary>
public static class ProjectionService
{
    public static List<ProjectionPoint> Project(User user, DateOnly from, int months)
    {
        // Work on a deep copy so simulating never mutates tracked entities.
        var sim = Clone(user);
        var payday = user.Income?.PaydayDayOfMonth ?? 1;
        var points = new List<ProjectionPoint> { ToPoint(AccrualService.TakeSnapshot(sim, from)) };

        var date = from;
        for (var i = 0; i < months; i++)
        {
            date = AccrualService.NextPayday(date, payday);
            AccrualService.ApplyMonthlyStep(sim, date);
            foreach (var investment in sim.Investments)
            {
                if (investment.ExpectedAnnualGrowthPercent is { } growth && growth != 0)
                    investment.CurrentValue = Math.Round(
                        investment.CurrentValue * (1 + growth / 100m / 12m), 2, MidpointRounding.AwayFromZero);
            }
            points.Add(ToPoint(AccrualService.TakeSnapshot(sim, date)));
        }
        return points;
    }

    private static ProjectionPoint ToPoint(NetWorthSnapshot s) => new(
        s.Date, s.NetWorth, s.TotalAssets, s.TotalLiabilities,
        s.Property, s.Savings, s.Investments, s.Mortgages, s.StudentLoans);

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
        }).ToList(),
        Investments = user.Investments.Select(i => new Investment
        {
            Id = i.Id, Name = i.Name, Type = i.Type, CurrentValue = i.CurrentValue,
            ExpectedAnnualGrowthPercent = i.ExpectedAnnualGrowthPercent,
        }).ToList(),
        Mortgages = user.Mortgages.Select(m => new Mortgage
        {
            Id = m.Id, Name = m.Name, OutstandingBalance = m.OutstandingBalance,
            AnnualInterestRatePercent = m.AnnualInterestRatePercent, RateType = m.RateType,
            FixedRateEndDate = m.FixedRateEndDate, FollowOnRatePercent = m.FollowOnRatePercent,
            TermMonthsRemaining = m.TermMonthsRemaining,
        }).ToList(),
        Properties = user.Properties.Select(p => new Property
        {
            Id = p.Id, Name = p.Name, EstimatedValue = p.EstimatedValue,
        }).ToList(),
    };
}
