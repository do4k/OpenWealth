using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// The pure payday maths: what happens to a user's balances in one month.
/// Savings earn a month of interest and receive their standing-order deposit,
/// student loans accrue their plan rate and get a month's repayment, and
/// mortgages accrue interest and pay their amortised monthly payment. Both the
/// live accrual service and forward projections step through months with this
/// class, so history and forecasts can never disagree.
/// </summary>
public static class MonthlyStepCalculator
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
    /// Applies one month of interest, deposits and repayments to the user's
    /// balances in place and returns the audit events.
    /// </summary>
    public static List<AccrualEvent> ApplyMonthlyStep(User user, DateOnly date)
    {
        var events = new List<AccrualEvent>();

        void Record(string category, string name, decimal interest, decimal payment, decimal newBalance,
            decimal deposit = 0m) =>
            events.Add(new AccrualEvent
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Date = date,
                Category = category,
                ItemName = name,
                InterestAmount = interest,
                PaymentAmount = payment,
                DepositAmount = deposit,
                NewBalance = newBalance,
            });

        foreach (var account in user.SavingsAccounts)
        {
            // Interest is earned on the pre-deposit balance: this month's
            // standing order starts earning from next month.
            var rate = account.AnnualInterestRatePercent ?? 0m;
            var interest = account.Balance > 0 ? (account.Balance * rate / 100m / 12m).RoundToPence() : 0m;
            var deposit = account.MonthlyDeposit;
            if (interest == 0 && deposit == 0)
                continue;
            account.Balance = Math.Max(0m, (account.Balance + interest + deposit).RoundToPence());
            Record("Savings", account.Name, interest, 0m, account.Balance, deposit);
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
                var interest = (loan.Balance * rate / 100m / 12m).RoundToPence();
                var share = totalBalance > 0 ? loan.Balance / totalBalance : 0m;
                var repayment = Math.Min((planRepayment * share).RoundToPence(), (loan.Balance + interest).RoundToPence());
                loan.Balance = (loan.Balance + interest - repayment).RoundToPence();
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
            var interest = (mortgage.OutstandingBalance * rate / 100m / 12m).RoundToPence();
            payment = Math.Min(payment, (mortgage.OutstandingBalance + interest).RoundToPence());
            mortgage.OutstandingBalance = (mortgage.OutstandingBalance + interest - payment).RoundToPence();
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
            .ToDictionary(r => r.Plan, r => (r.AnnualRepayment / 12m).RoundToPence());
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
}
