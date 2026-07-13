using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;

namespace OpenWealth.Api.Endpoints;

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
            settings.ChildcareIncomeLimit = req.ChildcareIncomeLimit;
            settings.HicbcLowerThreshold = req.HicbcLowerThreshold;
            settings.HicbcUpperThreshold = req.HicbcUpperThreshold;
            settings.ChildBenefitFirstChildWeekly = req.ChildBenefitFirstChildWeekly;
            settings.ChildBenefitAdditionalChildWeekly = req.ChildBenefitAdditionalChildWeekly;
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
