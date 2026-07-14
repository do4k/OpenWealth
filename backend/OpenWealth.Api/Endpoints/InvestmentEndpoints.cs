using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class InvestmentEndpoints
{
    public static void MapInvestmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/investments").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.Investments.AsNoTracking().Where(i => i.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (InvestmentRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var receivesContributions = ReceivesContributions(req);
            if (receivesContributions)
                await ClearOtherPensionContributionFlags(db, userId, exceptId: null);

            var investment = new Investment
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = req.Name,
                Type = req.Type,
                CurrentValue = req.CurrentValue,
                ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent,
                ReceivesIncomePensionContributions = receivesContributions,
            };
            db.Investments.Add(investment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/investments/{investment.Id}", investment);
        });

        group.MapPut("/{id:guid}", async (Guid id, InvestmentRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var investment = await db.Investments.SingleOrDefaultAsync(i => i.Id == id && i.UserId == userId);
            if (investment is null) return Results.NotFound();

            var receivesContributions = ReceivesContributions(req);
            if (receivesContributions)
                await ClearOtherPensionContributionFlags(db, userId, exceptId: id);

            investment.Name = req.Name;
            investment.Type = req.Type;
            investment.CurrentValue = req.CurrentValue;
            investment.ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent;
            investment.ReceivesIncomePensionContributions = receivesContributions;
            await db.SaveChangesAsync();
            return Results.Ok(investment);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.Investments
                .Where(i => i.Id == id && i.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    // Only a pension pot can be linked, regardless of what the client sends.
    private static bool ReceivesContributions(InvestmentRequest req) =>
        req.Type == InvestmentType.PensionPot && req.ReceivesIncomePensionContributions;

    // At most one investment per user receives the income-page pension
    // contributions — enabling it on one clears it from any other.
    private static async Task ClearOtherPensionContributionFlags(AppDbContext db, Guid userId, Guid? exceptId)
    {
        var others = await db.Investments
            .Where(i => i.UserId == userId && i.ReceivesIncomePensionContributions && i.Id != exceptId)
            .ToListAsync();
        foreach (var other in others)
            other.ReceivesIncomePensionContributions = false;
    }
}
