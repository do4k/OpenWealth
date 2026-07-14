using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class CustomDebtEndpoints
{
    public static void MapCustomDebtEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/custom-debts").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var debts = await db.CustomDebts.AsNoTracking().Where(d => d.UserId == p.UserId()).ToListAsync();
            return Results.Ok(debts.Select(d => d.ToResponse()));
        });

        group.MapPost("/", async (CustomDebtRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var error = await ReinvestDestinationValidation.Validate(
                db, userId, req.ReinvestDestinationType, req.ReinvestDestinationId, req.ReinvestMonthlyAmount);
            if (error is not null) return error;

            var debt = new CustomDebt { Id = Guid.NewGuid(), UserId = userId, Name = req.Name };
            Apply(debt, req);
            db.CustomDebts.Add(debt);
            await db.SaveChangesAsync();
            return Results.Created($"/api/custom-debts/{debt.Id}", debt.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomDebtRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var debt = await db.CustomDebts.SingleOrDefaultAsync(d => d.Id == id && d.UserId == userId);
            if (debt is null) return Results.NotFound();
            var error = await ReinvestDestinationValidation.Validate(
                db, userId, req.ReinvestDestinationType, req.ReinvestDestinationId, req.ReinvestMonthlyAmount);
            if (error is not null) return error;
            Apply(debt, req);
            await db.SaveChangesAsync();
            return Results.Ok(debt.ToResponse());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.CustomDebts
                .Where(d => d.Id == id && d.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static void Apply(CustomDebt d, CustomDebtRequest req)
    {
        d.Name = req.Name;
        d.Balance = req.Balance;
        d.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
        d.MonthlyPayment = req.MonthlyPayment;
        d.ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent;
        d.ReinvestDestinationType = req.ReinvestDestinationType;
        d.ReinvestDestinationId = req.ReinvestDestinationType == ReinvestDestinationType.None ? null : req.ReinvestDestinationId;
        d.ReinvestMonthlyAmount = req.ReinvestDestinationType == ReinvestDestinationType.None ? null : req.ReinvestMonthlyAmount;
    }
}
