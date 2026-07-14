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
            await db.CustomDebts.AsNoTracking().Where(d => d.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (CustomDebtRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var debt = new CustomDebt
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                Balance = req.Balance,
                AnnualInterestRatePercent = req.AnnualInterestRatePercent,
                MonthlyPayment = req.MonthlyPayment,
            };
            db.CustomDebts.Add(debt);
            await db.SaveChangesAsync();
            return Results.Created($"/api/custom-debts/{debt.Id}", debt);
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomDebtRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var debt = await db.CustomDebts.SingleOrDefaultAsync(d => d.Id == id && d.UserId == p.UserId());
            if (debt is null) return Results.NotFound();
            debt.Name = req.Name;
            debt.Balance = req.Balance;
            debt.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
            debt.MonthlyPayment = req.MonthlyPayment;
            await db.SaveChangesAsync();
            return Results.Ok(debt);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.CustomDebts
                .Where(d => d.Id == id && d.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
