using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Orchestrates payday runs: finds users whose payday is due (catching up any
/// missed while the server was off), applies the monthly step via
/// <see cref="MonthlyStepCalculator"/>, and persists the audit events and
/// net worth snapshots.
/// </summary>
public class AccrualService(AppDbContext db, ILogger<AccrualService> logger)
{
    /// <summary>
    /// Runs every payday due for one loaded user, writing events and a
    /// snapshot per payday.
    /// </summary>
    public int ApplyDueAccruals(User user, DateOnly today)
    {
        if (user.Income is not { AutomationEnabled: true } income)
            return 0;

        var applied = 0;
        var last = income.LastAccrualDate ?? today;
        var due = MonthlyStepCalculator.NextPayday(last, income.PaydayDayOfMonth);
        while (due <= today)
        {
            db.AccrualEvents.AddRange(MonthlyStepCalculator.ApplyMonthlyStep(user, due));
            ReplaceSnapshotForDate(user.Id, due);
            db.NetWorthSnapshots.Add(MonthlyStepCalculator.TakeSnapshot(user, due));
            income.LastAccrualDate = due;
            applied++;
            due = MonthlyStepCalculator.NextPayday(due, income.PaydayDayOfMonth);
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
            .Include(u => u.CustomAssets)
            .Include(u => u.CustomDebts)
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
}
