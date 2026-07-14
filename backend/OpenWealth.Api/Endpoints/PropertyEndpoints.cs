using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class PropertyEndpoints
{
    public static void MapPropertyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/properties").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.Properties.AsNoTracking().Where(x => x.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (PropertyRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var property = new Property
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                EstimatedValue = req.EstimatedValue,
                ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent,
            };
            db.Properties.Add(property);
            await db.SaveChangesAsync();
            return Results.Created($"/api/properties/{property.Id}", property);
        });

        group.MapPut("/{id:guid}", async (Guid id, PropertyRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var property = await db.Properties.SingleOrDefaultAsync(x => x.Id == id && x.UserId == p.UserId());
            if (property is null) return Results.NotFound();
            property.Name = req.Name;
            property.EstimatedValue = req.EstimatedValue;
            property.ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent;
            await db.SaveChangesAsync();
            return Results.Ok(property);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.Properties
                .Where(x => x.Id == id && x.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
