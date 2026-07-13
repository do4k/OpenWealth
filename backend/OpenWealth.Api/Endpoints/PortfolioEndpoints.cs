using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public record StudentLoanRequest(StudentLoanPlan Plan, decimal Balance, string? Notes);
public record PropertyRequest(string Name, decimal EstimatedValue);
public record MortgageRequest(
    string Name,
    Guid? PropertyId,
    decimal OutstandingBalance,
    decimal AnnualInterestRatePercent,
    MortgageRateType RateType,
    DateOnly? FixedRateEndDate,
    decimal? FollowOnRatePercent,
    int TermMonthsRemaining);
public record SavingsRequest(
    string Name,
    SavingsAccountType Type,
    decimal Balance,
    decimal? AnnualInterestRatePercent,
    decimal MonthlyDeposit = 0m);
public record InvestmentRequest(
    string Name, InvestmentType Type, decimal CurrentValue, decimal? ExpectedAnnualGrowthPercent);
public record IncomeRequest(
    decimal AnnualSalary,
    decimal AnnualBonus,
    decimal EmployeePensionPercent,
    decimal EmployerPensionPercent,
    PensionMethod PensionMethod,
    bool PensionOnBonus,
    int ChildrenReceivingChildBenefit);

public static class PortfolioEndpoints
{
    public static void MapPortfolioEndpoints(this IEndpointRouteBuilder app)
    {
        MapStudentLoans(app);
        MapProperties(app);
        MapMortgages(app);
        MapSavings(app);
        MapInvestments(app);
        MapIncome(app);

        app.MapGet("/api/summary", async (ClaimsPrincipal principal, SummaryService summaries) =>
            Results.Ok(await summaries.BuildAsync(principal.UserId())))
            .RequireAuthorization();
    }

    private static void MapStudentLoans(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/student-loans").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.StudentLoans.AsNoTracking().Where(l => l.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (StudentLoanRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var loan = new StudentLoan
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Plan = req.Plan,
                Balance = req.Balance,
                Notes = req.Notes,
            };
            db.StudentLoans.Add(loan);
            await db.SaveChangesAsync();
            return Results.Created($"/api/student-loans/{loan.Id}", loan);
        });

        group.MapPut("/{id:guid}", async (Guid id, StudentLoanRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var loan = await db.StudentLoans.SingleOrDefaultAsync(l => l.Id == id && l.UserId == p.UserId());
            if (loan is null) return Results.NotFound();
            loan.Plan = req.Plan;
            loan.Balance = req.Balance;
            loan.Notes = req.Notes;
            await db.SaveChangesAsync();
            return Results.Ok(loan);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.StudentLoans
                .Where(l => l.Id == id && l.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static void MapProperties(IEndpointRouteBuilder app)
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

    private static void MapMortgages(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/mortgages").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
        {
            var mortgages = await db.Mortgages.AsNoTracking()
                .Where(m => m.UserId == p.UserId()).ToListAsync();
            return Results.Ok(mortgages.Select(ToResponse));
        });

        group.MapPost("/", async (MortgageRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var error = await ValidatePropertyLink(req.PropertyId, p.UserId(), db);
            if (error is not null) return error;

            var mortgage = new Mortgage { Id = Guid.NewGuid(), UserId = p.UserId(), Name = req.Name };
            Apply(mortgage, req);
            db.Mortgages.Add(mortgage);
            await db.SaveChangesAsync();
            return Results.Created($"/api/mortgages/{mortgage.Id}", ToResponse(mortgage));
        });

        group.MapPut("/{id:guid}", async (Guid id, MortgageRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var mortgage = await db.Mortgages.SingleOrDefaultAsync(m => m.Id == id && m.UserId == p.UserId());
            if (mortgage is null) return Results.NotFound();
            var error = await ValidatePropertyLink(req.PropertyId, p.UserId(), db);
            if (error is not null) return error;
            Apply(mortgage, req);
            await db.SaveChangesAsync();
            return Results.Ok(ToResponse(mortgage));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.Mortgages
                .Where(m => m.Id == id && m.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });

        static async Task<IResult?> ValidatePropertyLink(Guid? propertyId, Guid userId, AppDbContext db)
        {
            if (propertyId is null) return null;
            var owned = await db.Properties.AnyAsync(x => x.Id == propertyId && x.UserId == userId);
            return owned ? null : Results.BadRequest(new { error = "Linked property not found." });
        }

        static void Apply(Mortgage m, MortgageRequest req)
        {
            m.Name = req.Name;
            m.PropertyId = req.PropertyId;
            m.OutstandingBalance = req.OutstandingBalance;
            m.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
            m.RateType = req.RateType;
            m.FixedRateEndDate = req.RateType == MortgageRateType.Fixed ? req.FixedRateEndDate : null;
            m.FollowOnRatePercent = req.RateType == MortgageRateType.Fixed ? req.FollowOnRatePercent : null;
            m.TermMonthsRemaining = req.TermMonthsRemaining;
        }

        static object ToResponse(Mortgage m) => new
        {
            m.Id,
            m.Name,
            m.PropertyId,
            m.OutstandingBalance,
            m.AnnualInterestRatePercent,
            m.RateType,
            m.FixedRateEndDate,
            m.FollowOnRatePercent,
            m.TermMonthsRemaining,
            MonthlyPayment = MortgageCalculator.MonthlyPayment(m),
            IsFixedPeriodOver = MortgageCalculator.IsFixedPeriodOver(m, DateOnly.FromDateTime(DateTime.UtcNow)),
        };
    }

    private static void MapSavings(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/savings").RequireAuthorization();

        group.MapGet("/", async (ClaimsPrincipal p, AppDbContext db) =>
            await db.SavingsAccounts.AsNoTracking().Where(s => s.UserId == p.UserId()).ToListAsync());

        group.MapPost("/", async (SavingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var account = new SavingsAccount
            {
                Id = Guid.NewGuid(),
                UserId = p.UserId(),
                Name = req.Name,
                Type = req.Type,
                Balance = req.Balance,
                AnnualInterestRatePercent = req.AnnualInterestRatePercent,
                MonthlyDeposit = req.MonthlyDeposit,
            };
            db.SavingsAccounts.Add(account);
            await db.SaveChangesAsync();
            return Results.Created($"/api/savings/{account.Id}", account);
        });

        group.MapPut("/{id:guid}", async (Guid id, SavingsRequest req, ClaimsPrincipal p, AppDbContext db) =>
        {
            var account = await db.SavingsAccounts.SingleOrDefaultAsync(s => s.Id == id && s.UserId == p.UserId());
            if (account is null) return Results.NotFound();
            account.Name = req.Name;
            account.Type = req.Type;
            account.Balance = req.Balance;
            account.AnnualInterestRatePercent = req.AnnualInterestRatePercent;
            account.MonthlyDeposit = req.MonthlyDeposit;
            await db.SaveChangesAsync();
            return Results.Ok(account);
        });

        group.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal p, AppDbContext db) =>
        {
            var deleted = await db.SavingsAccounts
                .Where(s => s.Id == id && s.UserId == p.UserId()).ExecuteDeleteAsync();
            return deleted > 0 ? Results.NoContent() : Results.NotFound();
        });
    }

    private static void MapInvestments(IEndpointRouteBuilder app)
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

    private static void MapIncome(IEndpointRouteBuilder app)
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
