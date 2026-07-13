using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public record AutomationSettingsRequest(bool Enabled, int PaydayDayOfMonth);

public static class AutomationEndpoints
{
    public static void MapAutomationEndpoints(this IEndpointRouteBuilder app)
    {
        var automation = app.MapGroup("/api/automation").RequireAuthorization();

        automation.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var income = await db.IncomeDetails.AsNoTracking()
                .SingleOrDefaultAsync(i => i.UserId == p.UserId());
            return Results.Ok(new
            {
                Enabled = income?.AutomationEnabled ?? false,
                PaydayDayOfMonth = income?.PaydayDayOfMonth ?? 1,
                LastAccrualDate = income?.LastAccrualDate,
            });
        });

        automation.MapPut("/", async (AutomationSettingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            if (req.PaydayDayOfMonth is < 1 or > 31)
                return Results.BadRequest(new { error = "Payday must be a day of the month (1–31)." });

            var user = await LoadUser(p.UserId(), db);
            if (user.Income is null)
                return Results.BadRequest(new { error = "Set up your income before enabling automation." });

            var enabling = req.Enabled && !user.Income.AutomationEnabled;
            user.Income.AutomationEnabled = req.Enabled;
            user.Income.PaydayDayOfMonth = req.PaydayDayOfMonth;
            if (enabling)
            {
                // Start tracking from today: record the opening snapshot and
                // only apply paydays that happen from now on.
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                user.Income.LastAccrualDate ??= today;
                if (!await db.NetWorthSnapshots.AnyAsync(s => s.UserId == user.Id && s.Date == today))
                    db.NetWorthSnapshots.Add(AccrualService.TakeSnapshot(user, today));
            }
            await db.SaveChangesAsync();
            return Results.Ok(new
            {
                Enabled = user.Income.AutomationEnabled,
                user.Income.PaydayDayOfMonth,
                user.Income.LastAccrualDate,
            });
        });

        // Applies any due paydays immediately instead of waiting for the worker.
        automation.MapPost("/run-now", async (ClaimsPrincipal p, AppDbContext db, AccrualService accruals) =>
        {
            var user = await LoadUser(p.UserId(), db);
            if (user.Income is not { AutomationEnabled: true })
                return Results.BadRequest(new { error = "Enable automation first." });
            var applied = accruals.ApplyDueAccruals(user, DateOnly.FromDateTime(DateTime.UtcNow));
            if (applied > 0)
                await db.SaveChangesAsync();
            return Results.Ok(new { PaydaysApplied = applied });
        });

        var history = app.MapGroup("/api/history").RequireAuthorization();

        history.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var snapshots = await db.NetWorthSnapshots.AsNoTracking()
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.Date)
                .ToListAsync();
            var events = await db.AccrualEvents.AsNoTracking()
                .Where(e => e.UserId == userId)
                .OrderByDescending(e => e.Date).ThenBy(e => e.Category)
                .Take(200)
                .ToListAsync();
            return Results.Ok(new { snapshots, events });
        });

        app.MapGet("/api/projections", async (int? months, ClaimsPrincipal p, AppDbContext db) =>
        {
            var horizon = Math.Clamp(months ?? 120, 1, 600);
            var user = await LoadUser(p.UserId(), db);
            var points = ProjectionService.Project(
                user, DateOnly.FromDateTime(DateTime.UtcNow), horizon);
            return Results.Ok(points);
        }).RequireAuthorization();
    }

    private static async Task<Models.User> LoadUser(Guid userId, AppDbContext db) =>
        await db.Users
            .Include(u => u.Income)
            .Include(u => u.TaxSettings)
            .Include(u => u.StudentLoanPlanSettings)
            .Include(u => u.StudentLoans)
            .Include(u => u.SavingsAccounts)
            .Include(u => u.Mortgages)
            .Include(u => u.Properties)
            .Include(u => u.Investments)
            .SingleAsync(u => u.Id == userId);
}
