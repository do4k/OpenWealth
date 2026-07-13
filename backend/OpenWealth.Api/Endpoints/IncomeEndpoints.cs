using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class IncomeEndpoints
{
    public static void MapIncomeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/income").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var income = await db.IncomeDetails.AsNoTracking()
                .SingleOrDefaultAsync(i => i.UserId == p.UserId());
            return income is null ? Results.NoContent() : Results.Ok(income);
        });

        group.MapPut("/", async (IncomeRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var income = await db.IncomeDetails.SingleOrDefaultAsync(i => i.UserId == p.UserId());
            if (income is null)
            {
                income = new IncomeDetails { Id = Guid.NewGuid(), UserId = p.UserId() };
                db.IncomeDetails.Add(income);
            }
            income.AnnualSalary = req.AnnualSalary;
            income.AnnualBonus = req.AnnualBonus;
            income.EmployeePensionPercent = req.EmployeePensionPercent;
            income.EmployerPensionPercent = req.EmployerPensionPercent;
            income.PensionMethod = req.PensionMethod;
            income.PensionOnBonus = req.PensionOnBonus;
            income.ChildrenReceivingChildBenefit = Math.Max(0, req.ChildrenReceivingChildBenefit);
            await db.SaveChangesAsync();
            return Results.Ok(income);
        });
    }
}
