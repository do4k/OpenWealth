using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Applies the monthly payday events to a user's balances: a month of interest
/// on savings, and a month of interest plus a month's repayment on student
/// loans and mortgages. The same stepping maths is reused by
/// <see cref="ProjectionService"/> so history and forecasts never disagree.
/// </summary>
public class AccrualService(AppDbContext db, ILogger<AccrualService> logger)
{
    /// <summary>The next payday strictly after <paramref name="after"/>, clamped for short months.</summary>
    public static DateOnly NextPayday(DateOnly after, int paydayDayOfMonth)
    {
        var candidate = ClampedDate(after.Year, after.Month, paydayDayOfMonth);
        if (candidate > after)
            return candidate;
        var next = after.AddMonths(1);
        return ClampedDate(next.Year, next.Month, paydayDayOfMonth);
    }

    private static DateOnly ClampedDate(int year, int month, int day) =>
        new(year, month, Math.Min(Math.Max(1, day), DateTime.DaysInMonth(year, month)));

    /// <summary>
    /// Applies one month of interest/repayments to the user's balances in place
    /// and returns the audit events. Pure with respect to the database.
    /// </summary>
    public static List<AccrualEvent> ApplyMonthlyStep(User user, DateOnly date)
    {
        var events = new List<AccrualEvent>();

        void Record(string category, string name, decimal interest, decimal payment, decimal newBalance) =>
            events.Add(new AccrualEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Date = date,
                Category = category,
                ItemName = name,
                InterestAmount = interest,
                PaymentAmount = payment,
                NewBalance = newBalance,
            });

        foreach (var account in user.SavingsAccounts)
        {
            if (account.AnnualInterestRatePercent is not { } rate || rate == 0 || account.Balance <= 0)
                continue;
            var interest = Round2(account.Balance * rate / 100m / 12m);
            account.Balance = Round2(account.Balance + interest);
            Record("Savings", account.Name, interest, 0m, account.Balance);
        }

        // A month's student loan repayment comes out of pay on payday; interest
        // accrues at the globally configured per-plan rate. Repayments are
        // shared across loans of the same plan in proportion to balance.
        var planSettings = user.StudentLoanPlanSettings.ToDictionary(s => s.Plan);
        var monthlyRepaymentByPlan = MonthlyLoanRepayments(user);
        foreach (var group in user.StudentLoans.GroupBy(l => l.Plan))
        {
            var totalBalance = group.Sum(l => l.Balance);
            monthlyRepaymentByPlan.TryGetValue(group.Key, out var planRepayment);
            foreach (var loan in group)
            {
                if (loan.Balance <= 0)
                    continue;
                var rate = planSettings.TryGetValue(loan.Plan, out var s) ? s.InterestRatePercent : 0m;
                var interest = Round2(loan.Balance * rate / 100m / 12m);
                var share = totalBalance > 0 ? loan.Balance / totalBalance : 0m;
                var repayment = Math.Min(Round2(planRepayment * share), Round2(loan.Balance + interest));
                loan.Balance = Round2(loan.Balance + interest - repayment);
                Record("Student loans", loan.Plan.ToString(), interest, repayment, loan.Balance);
            }
        }

        foreach (var mortgage in user.Mortgages)
        {
            if (mortgage.OutstandingBalance <= 0 || mortgage.TermMonthsRemaining <= 0)
                continue;
            var rate = EffectiveRate(mortgage, date);
            var payment = MortgageCalculator.MonthlyPayment(
                mortgage.OutstandingBalance, rate, mortgage.TermMonthsRemaining);
            var interest = Round2(mortgage.OutstandingBalance * rate / 100m / 12m);
            payment = Math.Min(payment, Round2(mortgage.OutstandingBalance + interest));
            mortgage.OutstandingBalance = Round2(mortgage.OutstandingBalance + interest - payment);
            mortgage.TermMonthsRemaining -= 1;
            Record("Mortgages", mortgage.Name, interest, payment, mortgage.OutstandingBalance);
        }

        return events;
    }

    /// <summary>Rate a mortgage is actually paying on a date: the follow-on rate once a fix has ended.</summary>
    public static decimal EffectiveRate(Mortgage m, DateOnly date) =>
        MortgageCalculator.IsFixedPeriodOver(m, date) && m.FollowOnRatePercent is { } followOn
            ? followOn
            : m.AnnualInterestRatePercent;

    /// <summary>Monthly student loan repayment per plan, derived from the user's income.</summary>
    public static Dictionary<StudentLoanPlan, decimal> MonthlyLoanRepayments(User user)
    {
        if (user.Income is null || user.TaxSettings is null)
            return [];
        var breakdown = TaxCalculator.Calculate(
            user.Income, user.TaxSettings,
            user.StudentLoans.Select(l => l.Plan),
            user.StudentLoanPlanSettings);
        return breakdown.StudentLoanRepayments
            .ToDictionary(r => r.Plan, r => Round2(r.AnnualRepayment / 12m));
    }

    public static NetWorthSnapshot TakeSnapshot(User user, DateOnly date)
    {
        var property = user.Properties.Sum(p => p.EstimatedValue);
        var savings = user.SavingsAccounts.Sum(s => s.Balance);
        var investments = user.Investments.Sum(i => i.CurrentValue);
        var mortgages = user.Mortgages.Sum(m => m.OutstandingBalance);
        var studentLoans = user.StudentLoans.Sum(l => l.Balance);
        var assets = property + savings + investments;
        var liabilities = mortgages + studentLoans;
        return new NetWorthSnapshot
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Date = date,
            NetWorth = assets - liabilities,
            TotalAssets = assets,
            TotalLiabilities = liabilities,
            Property = property,
            Savings = savings,
            Investments = investments,
            Mortgages = mortgages,
            StudentLoans = studentLoans,
        };
    }

    /// <summary>
    /// Runs every payday due for one loaded user (catching up any missed while
    /// the server was off), writing events and a snapshot per payday.
    /// </summary>
    public int ApplyDueAccruals(User user, DateOnly today)
    {
        if (user.Income is not { AutomationEnabled: true } income)
            return 0;

        var applied = 0;
        var last = income.LastAccrualDate ?? today;
        var due = NextPayday(last, income.PaydayDayOfMonth);
        while (due <= today)
        {
            db.AccrualEvents.AddRange(ApplyMonthlyStep(user, due));
            ReplaceSnapshotForDate(user.Id, due);
            db.NetWorthSnapshots.Add(TakeSnapshot(user, due));
            income.LastAccrualDate = due;
            applied++;
            due = NextPayday(due, income.PaydayDayOfMonth);
        }
        return applied;
    }

    private void ReplaceSnapshotForDate(Guid userId, DateOnly date)
    {
        var existing = db.NetWorthSnapshots.Where(s => s.UserId == userId && s.Date == date);
        db.NetWorthSnapshots.RemoveRange(existing);
    }

    public async Task<int> RunForAllDueUsersAsync(DateOnly today, CancellationToken ct = default)
    {
        var users = await db.Users
            .Include(u => u.Income)
            .Include(u => u.TaxSettings)
            .Include(u => u.StudentLoanPlanSettings)
            .Include(u => u.StudentLoans)
            .Include(u => u.SavingsAccounts)
            .Include(u => u.Mortgages)
            .Include(u => u.Properties)
            .Include(u => u.Investments)
            .Where(u => u.Income != null && u.Income.AutomationEnabled)
            .ToListAsync(ct);

        var totalApplied = 0;
        foreach (var user in users)
        {
            var applied = ApplyDueAccruals(user, today);
            if (applied > 0)
            {
                totalApplied += applied;
                logger.LogInformation(
                    "Applied {Count} payday accrual(s) for user {UserId}", applied, user.Id);
            }
        }
        if (totalApplied > 0)
            await db.SaveChangesAsync(ct);
        return totalApplied;
    }

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
