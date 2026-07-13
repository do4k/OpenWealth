using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Requests;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Extensions;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, AppDbContext db,
            IPasswordHasher<User> hasher, TokenService tokens) =>
        {
            var email = req.Email.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
                return Results.BadRequest(new { error = "A valid email address is required." });
            if (req.Password.Length < 10)
                return Results.BadRequest(new { error = "Password must be at least 10 characters." });
            if (string.IsNullOrWhiteSpace(req.DisplayName))
                return Results.BadRequest(new { error = "A display name is required." });
            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.Conflict(new { error = "An account with that email already exists." });

            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = req.DisplayName.Trim(),
                PasswordHash = "",
                CreatedAtUtc = DateTime.UtcNow,
            };
            user.PasswordHash = hasher.HashPassword(user, req.Password);

            // Every account starts with editable UK tax-year defaults.
            db.Users.Add(user);
            db.TaxSettings.Add(UkDefaults.NewTaxSettings(user.Id));
            db.StudentLoanPlanSettings.AddRange(UkDefaults.NewStudentLoanPlanSettings(user.Id));
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Email, user.DisplayName));
        });

        group.MapPost("/login", async (LoginRequest req, AppDbContext db,
            IPasswordHasher<User> hasher, TokenService tokens) =>
        {
            var email = req.Email.Trim().ToLowerInvariant();
            var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
            if (user is null ||
                hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password) == PasswordVerificationResult.Failed)
            {
                return Results.Unauthorized();
            }
            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Email, user.DisplayName));
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, AppDbContext db) =>
        {
            var user = await db.Users.AsNoTracking().SingleAsync(u => u.Id == principal.UserId());
            return Results.Ok(new { user.Email, user.DisplayName });
        }).RequireAuthorization();
    }
}
