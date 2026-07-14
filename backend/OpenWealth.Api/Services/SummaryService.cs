using Microsoft.EntityFrameworkCore;
using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Data;

namespace OpenWealth.Api.Services;

public class SummaryService(AppDbContext db)
{
    public async Task<WealthSummary> BuildAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await db.Users
            .AsNoTracking()
            .Include(u => u.Income)
            .Include(u => u.TaxSettings)
            .Include(u => u.StudentLoanPlanSettings)
            .Include(u => u.StudentLoans)
            .Include(u => u.Properties)
            .Include(u => u.Mortgages)
            .Include(u => u.SavingsAccounts)
            .Include(u => u.Investments)
            .Include(u => u.CustomAssets)
            .Include(u => u.CustomDebts)
            .SingleAsync(u => u.Id == userId, ct);

        var items = new List<NetWorthItem>();
        items.AddRange(user.Properties.Select(p => new NetWorthItem("Property", p.Name, p.EstimatedValue)));
        items.AddRange(user.SavingsAccounts.Select(s => new NetWorthItem("Savings", s.Name, s.Balance)));
        items.AddRange(user.Investments.Select(i => new NetWorthItem("Investments", i.Name, i.CurrentValue)));
        items.AddRange(user.CustomAssets.Select(a => new NetWorthItem("Other assets", a.Name, a.Value)));
        items.AddRange(user.Mortgages.Select(m => new NetWorthItem("Mortgages", m.Name, -m.OutstandingBalance)));
        items.AddRange(user.StudentLoans.Select(l => new NetWorthItem("Student loans", l.Plan.ToString(), -l.Balance)));
        items.AddRange(user.CustomDebts.Select(d => new NetWorthItem("Other debts", d.Name, -d.Balance)));

        var assetTotals = SumByCategory(items.Where(i => i.Value >= 0));
        var liabilityTotals = SumByCategory(items.Where(i => i.Value < 0));

        var totalAssets = assetTotals.Sum(t => t.Total);
        var totalLiabilities = liabilityTotals.Sum(t => t.Total);

        TakeHomeBreakdown? takeHome = null;
        if (user.Income is not null && user.TaxSettings is not null)
        {
            takeHome = TaxCalculator.Calculate(
                user.Income,
                user.TaxSettings,
                user.StudentLoans.Select(l => l.Plan),
                user.StudentLoanPlanSettings);
        }

        return new WealthSummary(
            NetWorth: totalAssets + totalLiabilities,
            TotalAssets: totalAssets,
            TotalLiabilities: totalLiabilities,
            AssetTotals: assetTotals,
            LiabilityTotals: liabilityTotals,
            Items: items,
            TakeHome: takeHome);
    }

    private static List<CategoryTotal> SumByCategory(IEnumerable<NetWorthItem> items) =>
        items.GroupBy(i => i.Category)
            .Select(g => new CategoryTotal(g.Key, g.Sum(i => i.Value)))
            .ToList();
}
