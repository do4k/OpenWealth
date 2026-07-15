using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

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

        automation.MapPut("/", async (AutomationSettingsRequest req, ClaimsPrincipal p, AutomationService automationService) =>
        {
            try
            {
                var income = await automationService.SetAsync(p.UserId(), req.Enabled, req.PaydayDayOfMonth);
                return Results.Ok(new { Enabled = income.AutomationEnabled, income.PaydayDayOfMonth, income.LastAccrualDate });
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
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
        await db.Users.WithWealthData().SingleAsync(u => u.Id == userId);
}
