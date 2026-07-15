using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Owns new-account creation: validates the registration fields, ensures the
/// email isn't already taken, and seeds the UK tax-year defaults every
/// account starts with.
/// </summary>
public class AuthService(AppDbContext db, IPasswordHasher<User> hasher, TokenService tokens)
{
    public async Task<AuthResponse> RegisterAsync(string rawEmail, string password, string displayName, CancellationToken ct = default)
    {
        var email = rawEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new DomainException("A valid email address is required.");
        if (password.Length < 10)
            throw new DomainException("Password must be at least 10 characters.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("A display name is required.");
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new DomainException("An account with that email already exists.", StatusCodes.Status409Conflict);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName.Trim(),
            PasswordHash = "",
            CreatedAtUtc = DateTime.UtcNow,
        };
        user.PasswordHash = hasher.HashPassword(user, password);

        // Every account starts with editable UK tax-year defaults.
        db.Users.Add(user);
        db.TaxSettings.Add(UkDefaults.NewTaxSettings(user.Id));
        db.StudentLoanPlanSettings.AddRange(UkDefaults.NewStudentLoanPlanSettings(user.Id));
        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokens.CreateToken(user), user.Email, user.DisplayName);
    }
}
