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

        // Once a mortgage or custom debt is fully paid off, its configured
        // monthly amount is redirected into a savings account or investment
        // instead of just disappearing, so paying off a debt keeps growing
        // wealth rather than stalling it.
        void ApplyReinvestment(ReinvestDestinationType type, Guid? destinationId, decimal? monthlyAmount)
        {
            if (type == ReinvestDestinationType.None || destinationId is not { } id || monthlyAmount is not > 0m)
                return;
            if (type == ReinvestDestinationType.Savings)
            {
                var account = user.SavingsAccounts.SingleOrDefault(s => s.Id == id);
                if (account is null) return;
                account.Balance = (account.Balance + monthlyAmount.Value).RoundToPence();
                Record("Savings", account.Name, 0m, 0m, account.Balance, monthlyAmount.Value);
            }
            else if (type == ReinvestDestinationType.Investment)
            {
                var investment = user.Investments.SingleOrDefault(i => i.Id == id);
                if (investment is null) return;
                investment.CurrentValue = (investment.CurrentValue + monthlyAmount.Value).RoundToPence();
                Record("Investments", investment.Name, 0m, 0m, investment.CurrentValue, monthlyAmount.Value);
            }
        }

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

        // At most one investment can be linked to receive the income page's
        // pension contributions (enforced by the endpoint); apply a month of
        // employee + employer contributions to it, same as a standing deposit.
        var monthlyPensionContribution = MonthlyPensionContribution(user);
        if (monthlyPensionContribution > 0)
        {
            foreach (var investment in user.Investments.Where(
                i => i.Type == InvestmentType.PensionPot && i.ReceivesIncomePensionContributions))
            {
                investment.CurrentValue = (investment.CurrentValue + monthlyPensionContribution).RoundToPence();
                Record("Investments", investment.Name, 0m, 0m, investment.CurrentValue, monthlyPensionContribution);
            }
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
            if (mortgage.OutstandingBalance <= 0)
            {
                // Already paid off coming into this payday: redirect from
                // this month, not the month the final payment lands.
                ApplyReinvestment(mortgage.ReinvestDestinationType, mortgage.ReinvestDestinationId,
                    mortgage.ReinvestMonthlyAmount);
                continue;
            }
            if (mortgage.TermMonthsRemaining <= 0)
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

        // Custom debts (credit cards, personal loans, car finance, ...) have
        // no fixed term or amortisation schedule: interest accrues on the
        // configured rate (if any) and the configured monthly payment (if
        // any) is applied, never overpaying the balance.
        foreach (var debt in user.CustomDebts)
        {
            if (debt.Balance <= 0)
            {
                ApplyReinvestment(debt.ReinvestDestinationType, debt.ReinvestDestinationId, debt.ReinvestMonthlyAmount);
                continue;
            }
            var rate = debt.AnnualInterestRatePercent ?? 0m;
            var interest = (debt.Balance * rate / 100m / 12m).RoundToPence();
            var payment = Math.Min(debt.MonthlyPayment ?? 0m, (debt.Balance + interest).RoundToPence());
            if (interest == 0 && payment == 0)
                continue;
            debt.Balance = (debt.Balance + interest - payment).RoundToPence();
            Record("Other debts", debt.Name, interest, payment, debt.Balance);
        }

        return events;
    }

    /// <summary>Rate a mortgage is actually paying on a date: the follow-on rate once a fix has ended.</summary>
    public static decimal EffectiveRate(Mortgage m, DateOnly date) =>
        MortgageCalculator.IsFixedPeriodOver(m, date) && m.FollowOnRatePercent is { } followOn
            ? followOn
            : m.AnnualInterestRatePercent;

    /// <summary>Monthly employee + employer pension contribution from the income page, or 0 without income set up.</summary>
    public static decimal MonthlyPensionContribution(User user)
    {
        if (user.Income is null)
            return 0m;
        var (employee, employer) = TaxCalculator.AnnualPensionContributions(user.Income);
        return ((employee + employer) / 12m).RoundToPence();
    }

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
        var otherAssets = user.CustomAssets.Sum(a => a.Value);
        var mortgages = user.Mortgages.Sum(m => m.OutstandingBalance);
        var studentLoans = user.StudentLoans.Sum(l => l.Balance);
        var otherDebts = user.CustomDebts.Sum(d => d.Balance);
        var assets = property + savings + investments + otherAssets;
        var liabilities = mortgages + studentLoans + otherDebts;
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
            OtherAssets = otherAssets,
            Mortgages = mortgages,
            StudentLoans = studentLoans,
            OtherDebts = otherDebts,
        };
    }
}
