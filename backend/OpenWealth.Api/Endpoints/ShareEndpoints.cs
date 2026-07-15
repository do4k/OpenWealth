using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class ShareEndpoints
{
    public static void MapShareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/share").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var settings = await db.ShareSettings.AsNoTracking()
                .SingleOrDefaultAsync(s => s.UserId == p.UserId());
            return Results.Ok(new
            {
                Enabled = settings?.Enabled ?? false,
                Slug = settings?.Slug,
                Visibility = settings?.Visibility ?? ShareVisibility.NetWorthOnly,
                HasPassphrase = settings?.PassphraseHash is not null,
            });
        });

        group.MapPut("/", async (ShareSettingsRequest req, ClaimsPrincipal p, AppDbContext db,
            IPasswordHasher<User> hasher) =>
        {
            var userId = p.UserId();
            var settings = await db.ShareSettings.SingleOrDefaultAsync(s => s.UserId == userId);
            if (settings is null)
            {
                settings = new ShareSettings
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Slug = NewSlug(),
                };
                db.ShareSettings.Add(settings);
            }

            var user = await db.Users.SingleAsync(u => u.Id == userId);
            if (!string.IsNullOrEmpty(req.Passphrase))
            {
                if (req.Passphrase.Length < 6)
                    return Results.BadRequest(new { error = "Passphrase must be at least 6 characters." });
                settings.PassphraseHash = hasher.HashPassword(user, req.Passphrase);
            }
            if (req.Enabled && settings.PassphraseHash is null)
                return Results.BadRequest(new { error = "Set a passphrase before enabling sharing." });

            settings.Enabled = req.Enabled;
            settings.Visibility = req.Visibility;
            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                settings.Enabled,
                settings.Slug,
                settings.Visibility,
                HasPassphrase = settings.PassphraseHash is not null,
            });
        });

        group.MapPost("/rotate-link", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var settings = await db.ShareSettings.SingleOrDefaultAsync(s => s.UserId == p.UserId());
            if (settings is null) return Results.NotFound();
            settings.Slug = NewSlug();
            await db.SaveChangesAsync();
            return Results.Ok(new { settings.Slug });
        });

        // Public, unauthenticated view. The passphrase is POSTed so it never
        // appears in URLs or server logs.
        app.MapPost("/api/public/{slug}", async (string slug, PublicProfileRequest req,
            AppDbContext db, IPasswordHasher<User> hasher, SummaryService summaries) =>
        {
            var settings = await db.ShareSettings.AsNoTracking()
                .SingleOrDefaultAsync(s => s.Slug == slug && s.Enabled);
            if (settings is null || settings.PassphraseHash is null)
                return Results.NotFound();

            var user = await db.Users.AsNoTracking().WithWealthData().SingleAsync(u => u.Id == settings.UserId);
            if (hasher.VerifyHashedPassword(user, settings.PassphraseHash, req.Passphrase)
                == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }

            var summary = await summaries.BuildAsync(settings.UserId);

            var snapshots = await db.NetWorthSnapshots.AsNoTracking()
                .Where(s => s.UserId == settings.UserId).OrderBy(s => s.Date).ToListAsync();
            var history = snapshots.Select(s => s.ToTrendPoint().ToShareView(settings.Visibility));
            var projection = ProjectionService.Project(user, DateOnly.FromDateTime(DateTime.UtcNow), 300)
                .Select(pt => pt.ToTrendPoint().ToShareView(settings.Visibility));

            return Results.Ok(BuildPublicView(user, settings.Visibility, summary, history, projection));
        });
    }

    // Extends WealthSummaryExtensions.ToShareView's tiers (via record
    // inheritance, not composition of anonymous types) with the
    // History/Projection fields this route adds on top. Returns object, not
    // a common base type — see the note on WealthSummaryExtensions.ToShareView
    // for why: serializing through a shared base type would silently drop
    // every field the base type doesn't declare, regressing the "lower tiers
    // never receive higher-tier fields over the wire" guarantee this route
    // is tested against.
    private static object BuildPublicView(
        User user, ShareVisibility visibility, WealthSummary summary,
        IEnumerable<object> history, IEnumerable<object> projection) =>
        visibility switch
        {
            ShareVisibility.NetWorthOnly => new PublicProfileNetWorthOnlyView(
                user.DisplayName, visibility, summary.NetWorth, history, projection),
            ShareVisibility.CategoryTotals => new PublicProfileCategoryTotalsView(
                user.DisplayName, visibility, summary.NetWorth, summary.TotalAssets, summary.TotalLiabilities,
                summary.AssetTotals, summary.LiabilityTotals, history, projection),
            _ => new PublicProfileFullBreakdownView(
                user.DisplayName, visibility, summary.NetWorth, summary.TotalAssets, summary.TotalLiabilities,
                summary.AssetTotals, summary.LiabilityTotals, summary.Items, history, projection),
        };

    private static string NewSlug() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant();
}
