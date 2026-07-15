using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Owns the payday-automation enable/disable workflow: validates the payday
/// day-of-month, requires income to already be set up, and seeds an opening
/// snapshot so history starts from today when automation is first enabled.
/// </summary>
public class AutomationService(AppDbContext db)
{
    public async Task<IncomeDetails> SetAsync(Guid userId, bool enabled, int paydayDayOfMonth, CancellationToken ct = default)
    {
        if (paydayDayOfMonth is < 1 or > 31)
            throw new DomainException("Payday must be a day of the month (1–31).");

        var user = await db.Users.WithWealthData().SingleAsync(u => u.Id == userId, ct);
        if (user.Income is null)
            throw new DomainException("Set up your income before enabling automation.");

        var enabling = enabled && !user.Income.AutomationEnabled;
        user.Income.AutomationEnabled = enabled;
        user.Income.PaydayDayOfMonth = paydayDayOfMonth;
        if (enabling)
        {
            // Start tracking from today: record the opening snapshot and
            // only apply paydays that happen from now on.
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            user.Income.LastAccrualDate ??= today;
            if (!await db.NetWorthSnapshots.AnyAsync(s => s.UserId == user.Id && s.Date == today, ct))
                db.NetWorthSnapshots.Add(MonthlyStepCalculator.TakeSnapshot(user, today));
        }
        await db.SaveChangesAsync(ct);
        return user.Income;
    }
}
