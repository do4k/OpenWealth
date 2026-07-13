using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class SavingsEndpoints
{
    public static void MapSavingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/savings").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.SavingsAccounts.AsNoTracking().Where(s => s.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (SavingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var account = new SavingsAccount
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                Type = req.Type,
                Balance = req.Balance,
                AnnualInterestRatePercent = req.AnnualInterestRatePercent,
                MonthlyDeposit = req.MonthlyDeposit,
            };
            db.SavingsAccounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Created($"/api/savings/{account.Id}", account);
        });

        group.MapPut("/{id:guid}", async (Guid id, SavingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var account = await db.SavingsAccounts.SingleOrDefaultAsync(s => s.Id == id && s.UserId == p.UserId());
            if (account is null) return Results.NotFound();
            account.Name = req.Name;
            account.Type = req.Type;
            account.Balance = req.Balance;
            account.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
            account.MonthlyDeposit = req.MonthlyDeposit;
            await db.SaveChangesAsync();
            return Results.Ok(account);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.SavingsAccounts
                .Where(s => s.Id == id && s.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
