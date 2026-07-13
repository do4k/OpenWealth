using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

public static class MortgageCalculator
{
    /// <summary>Standard repayment-mortgage amortisation payment.</summary>
    public static decimal MonthlyPayment(decimal balance, decimal annualRatePercent, int termMonths)
    {
        if (balance <= 0 || termMonths <= 0)
            return 0m;
        if (annualRatePercent == 0)
            return (balance / termMonths).RoundToPence();

        var r = (double)(annualRatePercent / 100m / 12m);
        var payment = (double)balance * r / (1 - Math.Pow(1 + r, -termMonths));
        return ((decimal)payment).RoundToPence();
    }

    public static decimal MonthlyPayment(Mortgage m) =>
        MonthlyPayment(m.OutstandingBalance, m.AnnualInterestRatePercent, m.TermMonthsRemaining);

    /// <summary>True when a fixed deal has already ended, i.e. the mortgage is now effectively variable.</summary>
    public static bool IsFixedPeriodOver(Mortgage m, DateOnly today) =>
        m.RateType == MortgageRateType.Fixed && m.FixedRateEndDate is { } end && end <= today;
}
