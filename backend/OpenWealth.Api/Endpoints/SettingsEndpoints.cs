using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public record TaxSettingsRequest(
    string TaxYearLabel,
    decimal PersonalAllowance,
    decimal PersonalAllowanceTaperThreshold,
    decimal BasicRateLimit,
    decimal HigherRateLimit,
    decimal BasicRatePercent,
    decimal HigherRatePercent,
    decimal AdditionalRatePercent,
    decimal NiPrimaryThresholdAnnual,
    decimal NiUpperEarningsLimitAnnual,
    decimal NiMainRatePercent,
    decimal NiUpperRatePercent);

public record LoanPlanSettingRequest(
    StudentLoanPlan Plan,
    decimal AnnualRepaymentThreshold,
    decimal RepaymentRatePercent,
    decimal InterestRatePercent);

public static class SettingsEndpoints
{
    public static void MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/settings").RequireAuthorization();

        group.MapGet("/tax", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.TaxSettings.AsNoTracking().SingleAsync(t => t.UserId == p.UserId()));

        group.MapPut("/tax", async (TaxSettingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var settings = await db.TaxSettings.SingleAsync(t => t.UserId == p.UserId());
            settings.TaxYearLabel = req.TaxYearLabel;
            settings.PersonalAllowance = req.PersonalAllowance;
            settings.PersonalAllowanceTaperThreshold = req.PersonalAllowanceTaperThreshold;
            settings.BasicRateLimit = req.BasicRateLimit;
            settings.HigherRateLimit = req.HigherRateLimit;
            settings.BasicRatePercent = req.BasicRatePercent;
            settings.HigherRatePercent = req.HigherRatePercent;
            settings.AdditionalRatePercent = req.AdditionalRatePercent;
            settings.NiPrimaryThresholdAnnual = req.NiPrimaryThresholdAnnual;
            settings.NiUpperEarningsLimitAnnual = req.NiUpperEarningsLimitAnnual;
            settings.NiMainRatePercent = req.NiMainRatePercent;
            settings.NiUpperRatePercent = req.NiUpperRatePercent;
            await db.SaveChangesAsync();
            return Results.Ok(settings);
        });

        group.MapGet("/student-loan-plans", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.StudentLoanPlanSettings.AsNoTracking()
                .Where(s => s.UserId == p.UserId())
                .OrderBy(s => s.Plan)
                .ToListAsync());

        // Global student loan configuration: one row per plan, so an interest
        // rate change applies to every loan of that plan at once.
        group.MapPut("/student-loan-plans", async (List<LoanPlanSettingRequest> reqs, ClaimsPrincipal p, AppDbContext db) =>
        {
            var settings = await db.StudentLoanPlanSettings
                .Where(s => s.UserId == p.UserId()).ToListAsync();
            foreach (var req in reqs)
            {
                var setting = settings.SingleOrDefault(s => s.Plan == req.Plan);
                if (setting is null) continue;
                setting.AnnualRepaymentThreshold = req.AnnualRepaymentThreshold;
                setting.RepaymentRatePercent = req.RepaymentRatePercent;
                setting.InterestRatePercent = req.InterestRatePercent;
            }
            await db.SaveChangesAsync();
            return Results.Ok(settings.OrderBy(s => s.Plan));
        });
    }
}
