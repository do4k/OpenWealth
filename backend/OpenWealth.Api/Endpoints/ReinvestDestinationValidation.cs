using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Data;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Endpoints;

/// <summary>
/// Shared validation for the "reinvest once paid off" destination configured
/// on mortgages and custom debts.
/// </summary>
internal static class ReinvestDestinationValidation
{
    public static async Task<IResult?> Validate(
        AppDbContext db, Guid userId, ReinvestDestinationType type, Guid? destinationId, decimal? monthlyAmount)
    {
        if (type == ReinvestDestinationType.None)
            return null;
        if (monthlyAmount is not > 0m)
            return Results.BadRequest(new { error = "Set a monthly amount to reinvest once this is paid off." });
        if (destinationId is null)
            return Results.BadRequest(new { error = "Select where to reinvest to." });

        var exists = type == ReinvestDestinationType.Savings
            ? await db.SavingsAccounts.AnyAsync(s => s.Id == destinationId && s.UserId == userId)
            : await db.Investments.AnyAsync(i => i.Id == destinationId && i.UserId == userId);
        return exists ? null : Results.BadRequest(new { error = "Reinvestment destination not found." });
    }
}
