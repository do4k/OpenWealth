using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Services;

/// <summary>
/// Pure logic for reading a goal's metric off a projection point and judging
/// whether it's on track — kept separate from the endpoint so it's testable
/// without a database.
/// </summary>
public static class GoalService
{
    /// <summary>
    /// Whether shrinking toward the target counts as progress. Only
    /// <see cref="GoalMetric.TotalLiabilities"/> works this way (e.g. a
    /// target of £0 for "debt-free by date"); every other metric grows
    /// toward its target.
    /// </summary>
    public static bool IsLowerBetter(GoalMetric metric) => metric == GoalMetric.TotalLiabilities;

    public static decimal ValueOf(ProjectionPoint point, GoalMetric metric) => metric switch
    {
        GoalMetric.NetWorth => point.NetWorth,
        GoalMetric.Savings => point.Savings,
        GoalMetric.Investments => point.Investments,
        GoalMetric.TotalAssets => point.TotalAssets,
        GoalMetric.TotalLiabilities => point.TotalLiabilities,
        _ => 0m,
    };

    public static bool IsOnTrack(GoalMetric metric, decimal projectedValue, decimal targetAmount) =>
        IsLowerBetter(metric) ? projectedValue <= targetAmount : projectedValue >= targetAmount;

    /// <summary>Whole months from <paramref name="from"/> to <paramref name="to"/>, clamped to what projections support.</summary>
    public static int MonthsUntil(DateOnly from, DateOnly to)
    {
        var months = (to.Year - from.Year) * 12 + (to.Month - from.Month) + (to.Day >= from.Day ? 0 : -1);
        return Math.Clamp(months, 0, 600);
    }

    /// <summary>The projection point nearest the target date, from a series that starts at "today".</summary>
    public static ProjectionPoint PointAtOrAfter(IReadOnlyList<ProjectionPoint> points, DateOnly targetDate) =>
        points.FirstOrDefault(p => p.Date >= targetDate) ?? points[^1];
}
