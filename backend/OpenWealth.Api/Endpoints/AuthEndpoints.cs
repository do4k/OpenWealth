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

        group.MapPost("/register", async (RegisterRequest req, AuthService auth) =>
        {
            try
            {
                return Results.Ok(await auth.RegisterAsync(req.Email, req.Password, req.DisplayName));
            }
            catch (DomainException ex)
            {
                return ex.ToResult();
            }
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
