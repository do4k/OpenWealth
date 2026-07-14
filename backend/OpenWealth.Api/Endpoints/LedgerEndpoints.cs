using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class LedgerEndpoints
{
    public static void MapLedgerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ledger").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var entries = await db.LedgerEntries.AsNoTracking()
                .Where(e => e.UserId == p.UserId())
                .OrderByDescending(e => e.Date).ThenByDescending(e => e.Id)
                .ToListAsync();
            return Results.Ok(entries);
        });

        group.MapPost("/", async (LedgerEntryRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            if (string.IsNullOrWhiteSpace(req.Description))
                return Results.BadRequest(new { error = "Enter a description." });
            if (req.Amount == 0m)
                return Results.BadRequest(new { error = "Enter a non-zero amount." });

            var error = await Apply(db, userId, req.AccountType, req.AccountId, req.Amount);
            if (error is not null) return error;

            var entry = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Date = req.Date,
                Description = req.Description,
                Amount = req.Amount,
                AccountType = req.AccountType,
                AccountId = req.AccountId,
            };
            db.LedgerEntries.Add(entry);
            await db.SaveChangesAsync();
            return Results.Created($"/api/ledger/{entry.Id}", entry);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var entry = await db.LedgerEntries.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);
            if (entry is null) return Results.NotFound();

            // Best-effort reversal: if the target account was deleted since, there's
            // nothing left to reverse, but the stale ledger row should still go.
            await Apply(db, userId, entry.AccountType, entry.AccountId, -entry.Amount);
            db.LedgerEntries.Remove(entry);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }

    private static async Task<IResult?> Apply(
        AppDbContext db, Guid userId, LedgerAccountType type, Guid accountId, decimal amount)
    {
        switch (type)
        {
            case LedgerAccountType.Savings:
                var savings = await db.SavingsAccounts.SingleOrDefaultAsync(x => x.Id == accountId && x.UserId == userId);
                if (savings is null) return Results.BadRequest(new { error = "Savings account not found." });
                savings.Balance = (savings.Balance + amount).RoundToPence();
                return null;
            case LedgerAccountType.Investment:
                var investment = await db.Investments.SingleOrDefaultAsync(x => x.Id == accountId && x.UserId == userId);
                if (investment is null) return Results.BadRequest(new { error = "Investment not found." });
                investment.CurrentValue = (investment.CurrentValue + amount).RoundToPence();
                return null;
            case LedgerAccountType.CustomAsset:
                var asset = await db.CustomAssets.SingleOrDefaultAsync(x => x.Id == accountId && x.UserId == userId);
                if (asset is null) return Results.BadRequest(new { error = "Asset not found." });
                asset.Value = (asset.Value + amount).RoundToPence();
                return null;
            default:
                return Results.BadRequest(new { error = "Unknown account type." });
        }
    }
}
