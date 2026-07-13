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
            var investment = new Investment
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                Type = req.Type,
                CurrentValue = req.CurrentValue,
                ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent,
            };
            db.Investments.Add(investment);
            await db.SaveChangesAsync();
            return Results.Created($"/api/investments/{investment.Id}", investment);
        });

        group.MapPut("/{id:guid}", async (Guid id, InvestmentRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var investment = await db.Investments.SingleOrDefaultAsync(i => i.Id == id && i.UserId == p.UserId());
            if (investment is null) return Results.NotFound();
            investment.Name = req.Name;
            investment.Type = req.Type;
            investment.CurrentValue = req.CurrentValue;
            investment.ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent;
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
}
