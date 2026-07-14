using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Tests;

public class MonthlyStepCalculatorTests
{
    private static User NewUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "t@example.com",
            DisplayName = "T",
            PasswordHash = "x",
        };
        user.TaxSettings = UkDefaults.NewTaxSettings(user.Id);
        user.StudentLoanPlanSettings = UkDefaults.NewStudentLoanPlanSettings(user.Id);
        return user;
    }

    private static readonly DateOnly Date = new(2026, 7, 25);

    [Fact]
    public void NextPayday_SameMonthWhenBeforePayday()
    {
        Assert.Equal(new DateOnly(2026, 7, 25), MonthlyStepCalculator.NextPayday(new DateOnly(2026, 7, 10), 25));
    }

    [Fact]
    public void NextPayday_RollsToNextMonthOnOrAfterPayday()
    {
        Assert.Equal(new DateOnly(2026, 8, 25), MonthlyStepCalculator.NextPayday(new DateOnly(2026, 7, 25), 25));
    }

    [Fact]
    public void NextPayday_ClampsShortMonths()
    {
        // Payday on the 31st lands on 28 Feb
        Assert.Equal(new DateOnly(2027, 2, 28), MonthlyStepCalculator.NextPayday(new DateOnly(2027, 1, 31), 31));
    }

    [Fact]
    public void SavingsGainMonthlyInterest()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Saver", Type = SavingsAccountType.EasyAccess,
            Balance = 12_000m, AnnualInterestRatePercent = 4.8m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // 12,000 * 4.8% / 12 = 48
        Assert.Equal(12_048m, user.SavingsAccounts[0].Balance);
        var e = Assert.Single(events);
        Assert.Equal(48m, e.InterestAmount);
        Assert.Equal("Savings", e.Category);
    }

    [Fact]
    public void MonthlyDepositIsAddedAfterInterest()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Regular saver", Type = SavingsAccountType.EasyAccess,
            Balance = 12_000m, AnnualInterestRatePercent = 4.8m, MonthlyDeposit = 500m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // Interest on the pre-deposit balance only: 12,000 * 4.8%/12 = 48;
        // the deposit starts earning next month.
        var e = Assert.Single(events);
        Assert.Equal(48m, e.InterestAmount);
        Assert.Equal(500m, e.DepositAmount);
        Assert.Equal(12_548m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void DepositAppliesEvenWithoutAnInterestRate()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Pot", Type = SavingsAccountType.Other,
            Balance = 100m, AnnualInterestRatePercent = null, MonthlyDeposit = 250m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(350m, user.SavingsAccounts[0].Balance);
        Assert.Equal(250m, Assert.Single(events).DepositAmount);
    }

    [Fact]
    public void NegativeDepositIsAStandingWithdrawalFlooredAtZero()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Drawdown", Type = SavingsAccountType.EasyAccess,
            Balance = 150m, AnnualInterestRatePercent = null, MonthlyDeposit = -200m,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(0m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void SavingsWithoutRateAreUntouched()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Current", Type = SavingsAccountType.CurrentAccount,
            Balance = 2_000m, AnnualInterestRatePercent = null,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(2_000m, user.SavingsAccounts[0].Balance);
        Assert.Empty(events);
    }

    [Fact]
    public void StudentLoanAccruesInterestAndRepayment()
    {
        var user = NewUser();
        user.Income = new IncomeDetails { AnnualSalary = 40_000m };
        user.StudentLoans.Add(new StudentLoan
        {
            Id = Guid.NewGuid(), Plan = StudentLoanPlan.Plan2, Balance = 30_000m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // Interest: 30,000 * 7.3% / 12 = 182.50
        // Repayment: (40,000 - 28,470) * 9% / 12 = 86.48 (annual 1,037.70 rounds to /12 = 86.48)
        var e = Assert.Single(events);
        Assert.Equal(182.50m, e.InterestAmount);
        Assert.Equal(86.48m, e.PaymentAmount);
        Assert.Equal(30_000m + 182.50m - 86.48m, user.StudentLoans[0].Balance);
    }

    [Fact]
    public void StudentLoanWithoutIncomeOnlyAccruesInterest()
    {
        var user = NewUser();
        user.StudentLoans.Add(new StudentLoan
        {
            Id = Guid.NewGuid(), Plan = StudentLoanPlan.Plan5, Balance = 10_000m,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // 10,000 * 4.3% / 12 = 35.83
        Assert.Equal(10_035.83m, user.StudentLoans[0].Balance);
    }

    [Fact]
    public void RepaymentNeverOverpaysLoan()
    {
        var user = NewUser();
        user.Income = new IncomeDetails { AnnualSalary = 80_000m };
        user.StudentLoans.Add(new StudentLoan
        {
            Id = Guid.NewGuid(), Plan = StudentLoanPlan.Plan2, Balance = 50m,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(0m, user.StudentLoans[0].Balance);
    }

    [Fact]
    public void MortgageAmortisesMonthly()
    {
        var user = NewUser();
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 200_000m,
            AnnualInterestRatePercent = 5m, RateType = MortgageRateType.Variable,
            TermMonthsRemaining = 300,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // Payment 1,169.18; interest 200,000 * 5%/12 = 833.33
        var e = Assert.Single(events);
        Assert.Equal(833.33m, e.InterestAmount);
        Assert.Equal(1_169.18m, e.PaymentAmount);
        Assert.Equal(199_664.15m, user.Mortgages[0].OutstandingBalance);
        Assert.Equal(299, user.Mortgages[0].TermMonthsRemaining);
    }

    [Fact]
    public void MortgageUsesFollowOnRateAfterFixEnds()
    {
        var user = NewUser();
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 100_000m,
            AnnualInterestRatePercent = 2m, RateType = MortgageRateType.Fixed,
            FixedRateEndDate = new DateOnly(2026, 1, 1), FollowOnRatePercent = 6m,
            TermMonthsRemaining = 120,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // Fix ended before the accrual date, so interest is at 6%: 100,000 * 6%/12 = 500
        Assert.Equal(500m, events[0].InterestAmount);
    }

    [Fact]
    public void PaidOffMortgageReinvestsIntoSavingsAccount()
    {
        var user = NewUser();
        var savings = new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "ISA", Type = SavingsAccountType.CashIsa, Balance = 5_000m,
        };
        user.SavingsAccounts.Add(savings);
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 0m,
            AnnualInterestRatePercent = 4m, RateType = MortgageRateType.Variable, TermMonthsRemaining = 0,
            ReinvestDestinationType = ReinvestDestinationType.Savings,
            ReinvestDestinationId = savings.Id, ReinvestMonthlyAmount = 1_200m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        var e = Assert.Single(events);
        Assert.Equal("Savings", e.Category);
        Assert.Equal("ISA", e.ItemName);
        Assert.Equal(1_200m, e.DepositAmount);
        Assert.Equal(6_200m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void PaidOffMortgageReinvestsIntoInvestment()
    {
        var user = NewUser();
        var investment = new Investment
        {
            Id = Guid.NewGuid(), Name = "S&S ISA", Type = InvestmentType.StocksAndSharesIsa, CurrentValue = 10_000m,
        };
        user.Investments.Add(investment);
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 0m,
            AnnualInterestRatePercent = 4m, RateType = MortgageRateType.Variable, TermMonthsRemaining = 0,
            ReinvestDestinationType = ReinvestDestinationType.Investment,
            ReinvestDestinationId = investment.Id, ReinvestMonthlyAmount = 900m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        var e = Assert.Single(events);
        Assert.Equal("Investments", e.Category);
        Assert.Equal(900m, e.DepositAmount);
        Assert.Equal(10_900m, user.Investments[0].CurrentValue);
    }

    [Fact]
    public void MortgageDoesNotReinvestInTheSameMonthItIsPaidOff()
    {
        var user = NewUser();
        var savings = new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "ISA", Type = SavingsAccountType.CashIsa, Balance = 5_000m,
        };
        user.SavingsAccounts.Add(savings);
        user.Mortgages.Add(new Mortgage
        {
            // Sized so the single remaining payment exactly clears the balance this month.
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 1_000m,
            AnnualInterestRatePercent = 6m, RateType = MortgageRateType.Variable, TermMonthsRemaining = 1,
            ReinvestDestinationType = ReinvestDestinationType.Savings,
            ReinvestDestinationId = savings.Id, ReinvestMonthlyAmount = 1_005m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(0m, user.Mortgages[0].OutstandingBalance);
        var e = Assert.Single(events);
        Assert.Equal("Mortgages", e.Category);
        // No reinvestment yet — the savings balance is untouched this month.
        Assert.Equal(5_000m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void PaidOffMortgageWithoutDestinationConfiguredDoesNothing()
    {
        var user = NewUser();
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "Home", OutstandingBalance = 0m,
            AnnualInterestRatePercent = 4m, RateType = MortgageRateType.Variable, TermMonthsRemaining = 0,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Empty(events);
    }

    [Fact]
    public void CustomDebtAccruesInterestAndTakesConfiguredPayment()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Credit card", Balance = 2_000m,
            AnnualInterestRatePercent = 24m, MonthlyPayment = 150m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // Interest: 2,000 * 24% / 12 = 40
        var e = Assert.Single(events);
        Assert.Equal("Other debts", e.Category);
        Assert.Equal(40m, e.InterestAmount);
        Assert.Equal(150m, e.PaymentAmount);
        Assert.Equal(2_000m + 40m - 150m, user.CustomDebts[0].Balance);
    }

    [Fact]
    public void CustomDebtPaymentNeverOverpaysBalance()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Almost gone", Balance = 30m,
            AnnualInterestRatePercent = 20m, MonthlyPayment = 500m,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(0m, user.CustomDebts[0].Balance);
    }

    [Fact]
    public void CustomDebtWithNoRateOrPaymentIsUntouched()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Interest-free tracker", Balance = 500m,
            AnnualInterestRatePercent = null, MonthlyPayment = null,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(500m, user.CustomDebts[0].Balance);
        Assert.Empty(events);
    }

    [Fact]
    public void PaidOffCustomDebtReinvestsIntoSavingsAccount()
    {
        var user = NewUser();
        var savings = new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "Rainy day fund", Type = SavingsAccountType.EasyAccess, Balance = 1_000m,
        };
        user.SavingsAccounts.Add(savings);
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Paid off card", Balance = 0m,
            ReinvestDestinationType = ReinvestDestinationType.Savings,
            ReinvestDestinationId = savings.Id, ReinvestMonthlyAmount = 150m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        var e = Assert.Single(events);
        Assert.Equal("Savings", e.Category);
        Assert.Equal(150m, e.DepositAmount);
        Assert.Equal(1_150m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void ReinvestmentSkipsAMissingDestinationWithoutCrashing()
    {
        // Defensive: the endpoint enforces the destination exists, but the
        // calculator itself shouldn't blow up if that invariant is broken.
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Orphaned link", Balance = 0m,
            ReinvestDestinationType = ReinvestDestinationType.Savings,
            ReinvestDestinationId = Guid.NewGuid(), ReinvestMonthlyAmount = 100m,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Empty(events);
    }

    [Fact]
    public void CustomDebtWithOnlyInterestAccruesButIsNotPaidDown()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Growing balance", Balance = 1_000m,
            AnnualInterestRatePercent = 12m, MonthlyPayment = null,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // 1,000 * 12% / 12 = 10, no payment configured
        Assert.Equal(1_010m, user.CustomDebts[0].Balance);
    }

    [Fact]
    public void SnapshotIncludesCustomAssetsAndDebts()
    {
        var user = NewUser();
        user.CustomAssets.Add(new CustomAsset { Id = Guid.NewGuid(), Name = "Car", Value = 8_000m });
        user.CustomDebts.Add(new CustomDebt { Id = Guid.NewGuid(), Name = "Card", Balance = 1_200m });

        var snap = MonthlyStepCalculator.TakeSnapshot(user, Date);

        Assert.Equal(8_000m, snap.OtherAssets);
        Assert.Equal(1_200m, snap.OtherDebts);
        Assert.Equal(8_000m, snap.TotalAssets);
        Assert.Equal(1_200m, snap.TotalLiabilities);
        Assert.Equal(6_800m, snap.NetWorth);
    }

    [Fact]
    public void LinkedPensionPotReceivesEmployeeAndEmployerContribution()
    {
        var user = NewUser();
        user.Income = new IncomeDetails
        {
            AnnualSalary = 60_000m, EmployeePensionPercent = 5m, EmployerPensionPercent = 3m,
        };
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "Workplace pension", Type = InvestmentType.PensionPot,
            CurrentValue = 10_000m, ReceivesIncomePensionContributions = true,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        // (60,000 * 5% + 60,000 * 3%) / 12 = (3,000 + 1,800) / 12 = 400
        var e = Assert.Single(events);
        Assert.Equal("Investments", e.Category);
        Assert.Equal(400m, e.DepositAmount);
        Assert.Equal(0m, e.InterestAmount);
        Assert.Equal(10_400m, user.Investments[0].CurrentValue);
    }

    [Fact]
    public void UnlinkedPensionPotIsUntouchedByIncomeContribution()
    {
        var user = NewUser();
        user.Income = new IncomeDetails { AnnualSalary = 60_000m, EmployeePensionPercent = 5m };
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "Workplace pension", Type = InvestmentType.PensionPot,
            CurrentValue = 10_000m, ReceivesIncomePensionContributions = false,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Empty(events);
        Assert.Equal(10_000m, user.Investments[0].CurrentValue);
    }

    [Fact]
    public void NonPensionPotInvestmentIgnoresContributionFlagEvenIfSet()
    {
        // Defensive: the endpoint only allows the flag on PensionPot investments,
        // but the calculator should not apply a contribution even if that
        // invariant were ever bypassed.
        var user = NewUser();
        user.Income = new IncomeDetails { AnnualSalary = 60_000m, EmployeePensionPercent = 5m };
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "ISA", Type = InvestmentType.StocksAndSharesIsa,
            CurrentValue = 10_000m, ReceivesIncomePensionContributions = true,
        });

        MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Equal(10_000m, user.Investments[0].CurrentValue);
    }

    [Fact]
    public void PensionContributionIsZeroWithoutIncome()
    {
        var user = NewUser();
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "Workplace pension", Type = InvestmentType.PensionPot,
            CurrentValue = 10_000m, ReceivesIncomePensionContributions = true,
        });

        var events = MonthlyStepCalculator.ApplyMonthlyStep(user, Date);

        Assert.Empty(events);
        Assert.Equal(10_000m, user.Investments[0].CurrentValue);
        Assert.Equal(0m, MonthlyStepCalculator.MonthlyPensionContribution(user));
    }

    [Fact]
    public void CatchUpAppliesEachMissedPayday()
    {
        var user = NewUser();
        user.Income = new IncomeDetails
        {
            AnnualSalary = 30_000m,
            AutomationEnabled = true,
            PaydayDayOfMonth = 15,
            LastAccrualDate = new DateOnly(2026, 3, 15),
        };
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess,
            Balance = 10_000m, AnnualInterestRatePercent = 12m,
        });

        // Simulate the catch-up loop without a database
        var applied = 0;
        var due = MonthlyStepCalculator.NextPayday(user.Income.LastAccrualDate.Value, 15);
        var today = new DateOnly(2026, 7, 20);
        while (due <= today)
        {
            MonthlyStepCalculator.ApplyMonthlyStep(user, due);
            applied++;
            due = MonthlyStepCalculator.NextPayday(due, 15);
        }

        // April, May, June, July paydays = 4 months at 1%/month compounding
        Assert.Equal(4, applied);
        Assert.Equal(10_406.04m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void SnapshotSumsCategories()
    {
        var user = NewUser();
        user.Properties.Add(new Property { Id = Guid.NewGuid(), Name = "H", EstimatedValue = 300_000m });
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "M", OutstandingBalance = 180_000m, TermMonthsRemaining = 200,
        });
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess, Balance = 5_000m,
        });

        var snap = MonthlyStepCalculator.TakeSnapshot(user, Date);

        Assert.Equal(305_000m, snap.TotalAssets);
        Assert.Equal(180_000m, snap.TotalLiabilities);
        Assert.Equal(125_000m, snap.NetWorth);
    }
}

public class ProjectionServiceTests
{
    private static User NewUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "p@example.com",
            DisplayName = "P",
            PasswordHash = "x",
        };
        user.TaxSettings = UkDefaults.NewTaxSettings(user.Id);
        user.StudentLoanPlanSettings = UkDefaults.NewStudentLoanPlanSettings(user.Id);
        return user;
    }

    [Fact]
    public void SavingsTrendUpwardAndMortgagesDownward()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess,
            Balance = 10_000m, AnnualInterestRatePercent = 4m,
        });
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "M", OutstandingBalance = 150_000m,
            AnnualInterestRatePercent = 4.5m, RateType = MortgageRateType.Variable,
            TermMonthsRemaining = 240,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 24);

        Assert.Equal(25, points.Count);
        Assert.True(points[^1].Savings > points[0].Savings);
        Assert.True(points[^1].Mortgages < points[0].Mortgages);
        Assert.True(points[^1].NetWorth > points[0].NetWorth);
    }

    [Fact]
    public void MortgageIsPaidOffAtEndOfTerm()
    {
        var user = NewUser();
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "M", OutstandingBalance = 50_000m,
            AnnualInterestRatePercent = 5m, RateType = MortgageRateType.Variable,
            TermMonthsRemaining = 24,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 30);

        Assert.Equal(0m, points[^1].Mortgages);
    }

    [Fact]
    public void PayingOffAMortgageRedirectsItsPaymentIntoSavingsInProjection()
    {
        var user = NewUser();
        var savings = new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess, Balance = 0m,
        };
        user.SavingsAccounts.Add(savings);
        var monthlyPayment = MortgageCalculator.MonthlyPayment(50_000m, 5m, 24);
        user.Mortgages.Add(new Mortgage
        {
            Id = Guid.NewGuid(), Name = "M", OutstandingBalance = 50_000m,
            AnnualInterestRatePercent = 5m, RateType = MortgageRateType.Variable,
            TermMonthsRemaining = 24,
            ReinvestDestinationType = ReinvestDestinationType.Savings,
            ReinvestDestinationId = savings.Id, ReinvestMonthlyAmount = monthlyPayment,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 30);

        // Paid off exactly at month 24: no reinvestment has landed yet.
        Assert.Equal(0m, points[24].Mortgages);
        Assert.Equal(0m, points[24].Savings);
        // From month 25 the former mortgage payment lands in savings instead —
        // 6 more paydays (25..30) at the same monthly amount, no compounding
        // since this savings account carries no interest rate of its own.
        Assert.Equal(6 * monthlyPayment, points[30].Savings);
        Assert.True(points[30].NetWorth > points[24].NetWorth);
    }

    [Fact]
    public void ProjectionDoesNotMutateRealBalances()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess,
            Balance = 10_000m, AnnualInterestRatePercent = 4m,
        });

        ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        Assert.Equal(10_000m, user.SavingsAccounts[0].Balance);
    }

    [Fact]
    public void InvestmentGrowthOnlyAppliesWhenConfigured()
    {
        var user = NewUser();
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "ISA", Type = InvestmentType.StocksAndSharesIsa,
            CurrentValue = 20_000m, ExpectedAnnualGrowthPercent = 6m,
        });
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "Flat", Type = InvestmentType.Other,
            CurrentValue = 5_000m, ExpectedAnnualGrowthPercent = null,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // 20,000 growing at 0.5%/month for 12 months ≈ 21,233.56; flat one unchanged
        Assert.Equal(26_233.56m, points[^1].Investments);
    }

    [Fact]
    public void ProjectionCompoundsMonthlyDeposits()
    {
        var user = NewUser();
        user.SavingsAccounts.Add(new SavingsAccount
        {
            Id = Guid.NewGuid(), Name = "S", Type = SavingsAccountType.EasyAccess,
            Balance = 0m, AnnualInterestRatePercent = 12m, MonthlyDeposit = 100m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // Future value of £100/month deposited after 1%/month interest:
        // 100 * ((1.01^12 - 1) / 0.01) = 1,268.25
        Assert.Equal(1_268.25m, points[^1].Savings);
    }

    [Fact]
    public void LinkedPensionPotAccumulatesContributionsInProjection()
    {
        var user = NewUser();
        user.Income = new IncomeDetails
        {
            AnnualSalary = 60_000m, EmployeePensionPercent = 5m, EmployerPensionPercent = 3m,
        };
        user.Investments.Add(new Investment
        {
            Id = Guid.NewGuid(), Name = "Workplace pension", Type = InvestmentType.PensionPot,
            CurrentValue = 10_000m, ReceivesIncomePensionContributions = true,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // No growth rate configured, so it's pure accumulation: 10,000 + 400 * 12 = 14,800
        Assert.Equal(14_800m, points[^1].Investments);

        // Real investment is untouched
        Assert.Equal(10_000m, user.Investments[0].CurrentValue);
    }

    [Fact]
    public void StudentLoanCanBeFullyRepaidInProjection()
    {
        var user = NewUser();
        user.Income = new IncomeDetails { AnnualSalary = 60_000m };
        user.StudentLoans.Add(new StudentLoan
        {
            Id = Guid.NewGuid(), Plan = StudentLoanPlan.Plan1, Balance = 2_000m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        Assert.Equal(0m, points[^1].StudentLoans);
        // Balance never goes negative on the way down
        Assert.All(points, pt => Assert.True(pt.StudentLoans >= 0m));
    }

    [Fact]
    public void CustomAssetGrowsAndCustomDebtAmortisesInProjection()
    {
        var user = NewUser();
        user.CustomAssets.Add(new CustomAsset
        {
            Id = Guid.NewGuid(), Name = "Car", Value = 10_000m, ExpectedAnnualGrowthPercent = -12m,
        });
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Card", Balance = 500m,
            AnnualInterestRatePercent = 20m, MonthlyPayment = 100m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // Depreciating asset trends down, never negative
        Assert.True(points[^1].OtherAssets < points[0].OtherAssets);
        Assert.All(points, pt => Assert.True(pt.OtherAssets >= 0m));
        // Debt paid off well within 12 months at £100/mo on a £500 balance
        Assert.Equal(0m, points[^1].OtherDebts);
    }

    [Fact]
    public void CustomAssetAndDebtDoNotMutateRealBalances()
    {
        var user = NewUser();
        user.CustomAssets.Add(new CustomAsset
        {
            Id = Guid.NewGuid(), Name = "Car", Value = 10_000m, ExpectedAnnualGrowthPercent = 5m,
        });
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Card", Balance = 500m, AnnualInterestRatePercent = 20m,
        });

        ProjectionService.Project(user, new DateOnly(2026, 7, 13), 6);

        Assert.Equal(10_000m, user.CustomAssets[0].Value);
        Assert.Equal(500m, user.CustomDebts[0].Balance);
    }

    [Fact]
    public void PropertyGrowsInProjectionWhenConfigured()
    {
        var user = NewUser();
        user.Properties.Add(new Property
        {
            Id = Guid.NewGuid(), Name = "Home", EstimatedValue = 300_000m,
            ExpectedAnnualGrowthPercent = 3m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // 300,000 growing at 0.25%/month for 12 months
        Assert.Equal(309_124.77m, points[^1].Property);
    }

    [Fact]
    public void PropertyWithoutGrowthRateStaysFlatInProjection()
    {
        var user = NewUser();
        user.Properties.Add(new Property
        {
            Id = Guid.NewGuid(), Name = "Home", EstimatedValue = 300_000m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        Assert.Equal(300_000m, points[^1].Property);
        // Real property is never touched, matching every other growth-rate item
        Assert.Equal(300_000m, user.Properties[0].EstimatedValue);
    }

    [Fact]
    public void CustomDebtGrowthOnlyAppliesWhenConfigured()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Growing card", Balance = 2_000m,
            ExpectedAnnualGrowthPercent = 8m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 12);

        // No interest rate or payment configured, so this is pure growth:
        // 2,000 growing at ~0.667%/month for 12 months
        Assert.Equal(2_165.98m, points[^1].OtherDebts);
    }

    [Fact]
    public void CustomDebtExpectedGrowthLayersOnTopOfRealAccrualInProjection()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Card", Balance = 1_000m,
            AnnualInterestRatePercent = 24m, MonthlyPayment = 50m,
            ExpectedAnnualGrowthPercent = 12m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 6);

        // Real interest/payment alone would leave this at 810.76 after 6 months;
        // the extra 12%/yr expected growth is layered on top every month, so the
        // projected balance shrinks more slowly than the real accrual implies.
        Assert.Equal(868.61m, points[^1].OtherDebts);
        Assert.True(points[^1].OtherDebts > 810.76m);
    }

    [Fact]
    public void PaidOffCustomDebtWithGrowthRateStaysAtZero()
    {
        var user = NewUser();
        user.CustomDebts.Add(new CustomDebt
        {
            Id = Guid.NewGuid(), Name = "Card", Balance = 100m, MonthlyPayment = 100m,
            ExpectedAnnualGrowthPercent = 10m,
        });

        var points = ProjectionService.Project(user, new DateOnly(2026, 7, 13), 3);

        // Paid off in month 1; an expected-growth rate must never resurrect a
        // cleared balance (0 * any growth factor is still 0).
        Assert.All(points.Skip(1), pt => Assert.Equal(0m, pt.OtherDebts));
    }
}
