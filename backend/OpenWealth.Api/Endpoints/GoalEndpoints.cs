using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class GoalEndpoints
{
    public static void MapGoalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/goals").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var goals = await db.Goals.AsNoTracking()
                .Where(g => g.UserId == userId).OrderBy(g => g.TargetDate).ToListAsync();
            if (goals.Count == 0) return Results.Ok(Array.Empty<GoalResponse>());

            var user = await db.Users.AsNoTracking().WithWealthData().SingleAsync(u => u.Id == userId);
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var months = goals.Max(g => GoalService.MonthsUntil(today, g.TargetDate));
            var points = ProjectionService.Project(user, today, months);

            return Results.Ok(goals.Select(g => g.ToResponse(points)));
        });

        group.MapPost("/", async (GoalRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var goal = new Goal { Id = Guid.NewGuid(), UserId = p.UserId(), Name = req.Name };
            Apply(goal, req);
            db.Goals.Add(goal);
            await db.SaveChangesAsync();
            return Results.Created($"/api/goals/{goal.Id}", goal);
        });

        group.MapPut("/{id:guid}", async (Guid id, GoalRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var goal = await db.Goals.SingleOrDefaultAsync(g => g.Id == id && g.UserId == p.UserId());
            if (goal is null) return Results.NotFound();
            Apply(goal, req);
            await db.SaveChangesAsync();
            return Results.Ok(goal);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.Goals.Where(g => g.Id == id && g.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static void Apply(Goal g, GoalRequest req)
    {
        g.Name = req.Name;
        g.Metric = req.Metric;
        g.TargetAmount = req.TargetAmount;
        g.TargetDate = req.TargetDate;
    }
}
