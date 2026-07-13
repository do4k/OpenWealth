using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

public static class StudentLoanEndpoints
{
    public static void MapStudentLoanEndpoints(this IEndpointRouteBuilder app)
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
}
