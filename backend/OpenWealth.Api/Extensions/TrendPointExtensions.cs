using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class TrendPointExtensions
{
    public static TrendPoint ToTrendPoint(this NetWorthSnapshot s) => new(
        s.Date, s.NetWorth, s.TotalAssets, s.TotalLiabilities, s.Property,
        s.Savings, s.Investments, s.OtherAssets, s.Mortgages, s.StudentLoans, s.OtherDebts);

    public static TrendPoint ToTrendPoint(this ProjectionPoint p) => new(
        p.Date, p.NetWorth, p.TotalAssets, p.TotalLiabilities, p.Property,
        p.Savings, p.Investments, p.OtherAssets, p.Mortgages, p.StudentLoans, p.OtherDebts);

    /// <summary>
    /// Shapes a trend point down to what a given public-profile visibility
    /// tier allows. Returns <see cref="object"/> rather than a common base
    /// type — see the equivalent note on <see cref="WealthSummaryExtensions.ToShareView"/>.
    /// </summary>
    public static object ToShareView(this TrendPoint p, ShareVisibility visibility) => visibility switch
    {
        ShareVisibility.NetWorthOnly => new TrendPointNetWorthOnlyView(p.Date, p.NetWorth),
        ShareVisibility.CategoryTotals => new TrendPointCategoryTotalsView(p.Date, p.NetWorth, p.TotalAssets, p.TotalLiabilities),
        _ => new TrendPointFullBreakdownView(
            p.Date, p.NetWorth, p.TotalAssets, p.TotalLiabilities, p.Property,
            p.Savings, p.Investments, p.OtherAssets, p.Mortgages, p.StudentLoans, p.OtherDebts),
    };
}
