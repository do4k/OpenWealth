using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class MortgageEndpoints
{
    public static void MapMortgageEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mortgages").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var mortgages = await db.Mortgages.AsNoTracking()
                .Where(m => m.UserId == p.UserId()).ToListAsync();
            return Results.Ok(mortgages.Select(m => m.ToResponse()));
        });

        group.MapPost("/", async (MortgageRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var error = await ValidatePropertyLink(req.PropertyId, userId, db)
                ?? await ReinvestDestinationValidation.Validate(
                    db, userId, req.ReinvestDestinationType, req.ReinvestDestinationId, req.ReinvestMonthlyAmount);
            if (error is not null) return error;

            var mortgage = new Mortgage { Id = Guid.NewGuid(), UserId = userId, Name = req.Name };
            Apply(mortgage, req);
            db.Mortgages.Add(mortgage);
            await db.SaveChangesAsync();
            return Results.Created($"/api/mortgages/{mortgage.Id}", mortgage.ToResponse());
        });

        group.MapPut("/{id:guid}", async (Guid id, MortgageRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var userId = p.UserId();
            var mortgage = await db.Mortgages.SingleOrDefaultAsync(m => m.Id == id && m.UserId == userId);
            if (mortgage is null) return Results.NotFound();
            var error = await ValidatePropertyLink(req.PropertyId, userId, db)
                ?? await ReinvestDestinationValidation.Validate(
                    db, userId, req.ReinvestDestinationType, req.ReinvestDestinationId, req.ReinvestMonthlyAmount);
            if (error is not null) return error;
            Apply(mortgage, req);
            await db.SaveChangesAsync();
            return Results.Ok(mortgage.ToResponse());
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.Mortgages
                .Where(m => m.Id == id && m.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static async Task<IResult?> ValidatePropertyLink(Guid? propertyId, Guid userId, AppDbContext db)
    {
        if (propertyId is null) return null;
        var owned = await db.Properties.AnyAsync(x => x.Id == propertyId && x.UserId == userId);
        return owned ? null : Results.BadRequest(new { error = "Linked property not found." });
    }

    private static void Apply(Mortgage m, MortgageRequest req)
    {
        m.Name = req.Name;
        m.PropertyId = req.PropertyId;
        m.OutstandingBalance = req.OutstandingBalance;
        m.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
        m.RateType = req.RateType;
        m.FixedRateEndDate = req.RateType == MortgageRateType.Fixed ? req.FixedRateEndDate : null;
        m.FollowOnRatePercent = req.RateType == MortgageRateType.Fixed ? req.FollowOnRatePercent : null;
        m.TermMonthsRemaining = req.TermMonthsRemaining;
        m.ReinvestDestinationType = req.ReinvestDestinationType;
        m.ReinvestDestinationId = req.ReinvestDestinationType == ReinvestDestinationType.None ? null : req.ReinvestDestinationId;
        m.ReinvestMonthlyAmount = req.ReinvestDestinationType == ReinvestDestinationType.None ? null : req.ReinvestMonthlyAmount;
    }
}
