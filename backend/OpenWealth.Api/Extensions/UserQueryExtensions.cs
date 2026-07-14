using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class UserQueryExtensions
{
    /// <summary>
    /// Includes every collection needed to run payday accrual, projections or
    /// the wealth summary for a user — the "full aggregate" shape shared by
    /// AutomationEndpoints, ShareEndpoints and SummaryService.
    /// </summary>
    public static IQueryable<User> WithWealthData(this IQueryable<User> users) => users
        .Include(u => u.Income)
        .Include(u => u.TaxSettings)
        .Include(u => u.StudentLoanPlanSettings)
        .Include(u => u.StudentLoans)
        .Include(u => u.Properties)
        .Include(u => u.Mortgages)
        .Include(u => u.SavingsAccounts)
        .Include(u => u.Investments)
        .Include(u => u.CustomAssets)
        .Include(u => u.CustomDebts);
}
