using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class CustomAssetEndpoints
{
    public static void MapCustomAssetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/custom-assets").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.CustomAssets.AsNoTracking().Where(a => a.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (CustomAssetRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var asset = new CustomAsset
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                Value = req.Value,
                ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent,
            };
            db.CustomAssets.Add(asset);
            await db.SaveChangesAsync();
            return Results.Created($"/api/custom-assets/{asset.Id}", asset);
        });

        group.MapPut("/{id:guid}", async (Guid id, CustomAssetRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var asset = await db.CustomAssets.SingleOrDefaultAsync(a => a.Id == id && a.UserId == p.UserId());
            if (asset is null) return Results.NotFound();
            asset.Name = req.Name;
            asset.Value = req.Value;
            asset.ExpectedAnnualGrowthPercent = req.ExpectedAnnualGrowthPercent;
            await db.SaveChangesAsync();
            return Results.Ok(asset);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.CustomAssets
                .Where(a => a.Id == id && a.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }
}
