using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Tests;

public class GoalServiceTests
{
    private static ProjectionPoint Point(DateOnly date, decimal netWorth = 0m, decimal totalAssets = 0m,
        decimal totalLiabilities = 0m, decimal savings = 0m, decimal investments = 0m) => new(
        date, netWorth, totalAssets, totalLiabilities, Property: 0m, savings, investments,
        OtherAssets: 0m, Mortgages: 0m, StudentLoans: 0m, OtherDebts: 0m);

    [Fact]
    public void ValueOfReadsTheFieldMatchingEachMetric()
    {
        var p = Point(new DateOnly(2026, 1, 1),
            netWorth: 100m, totalAssets: 200m, totalLiabilities: 50m, savings: 30m, investments: 70m);

        Assert.Equal(100m, GoalService.ValueOf(p, GoalMetric.NetWorth));
        Assert.Equal(30m, GoalService.ValueOf(p, GoalMetric.Savings));
        Assert.Equal(70m, GoalService.ValueOf(p, GoalMetric.Investments));
        Assert.Equal(200m, GoalService.ValueOf(p, GoalMetric.TotalAssets));
        Assert.Equal(50m, GoalService.ValueOf(p, GoalMetric.TotalLiabilities));
    }

    [Fact]
    public void AssetMetricIsOnTrackWhenProjectedValueMeetsOrExceedsTarget()
    {
        Assert.True(GoalService.IsOnTrack(GoalMetric.NetWorth, projectedValue: 50_000m, targetAmount: 50_000m));
        Assert.True(GoalService.IsOnTrack(GoalMetric.NetWorth, projectedValue: 60_000m, targetAmount: 50_000m));
        Assert.False(GoalService.IsOnTrack(GoalMetric.NetWorth, projectedValue: 40_000m, targetAmount: 50_000m));
    }

    [Fact]
    public void LiabilityMetricIsOnTrackWhenProjectedValueIsAtOrBelowTarget()
    {
        // A "debt-free by date" goal targets £0 of total liabilities: on track
        // means the projected balance has fallen to (or below) that target.
        Assert.True(GoalService.IsOnTrack(GoalMetric.TotalLiabilities, projectedValue: 0m, targetAmount: 0m));
        Assert.True(GoalService.IsOnTrack(GoalMetric.TotalLiabilities, projectedValue: -100m, targetAmount: 0m));
        Assert.False(GoalService.IsOnTrack(GoalMetric.TotalLiabilities, projectedValue: 5_000m, targetAmount: 0m));
    }

    [Fact]
    public void MonthsUntilCountsWholeMonthsAndRoundsDownForAnEarlierDayOfMonth()
    {
        var from = new DateOnly(2026, 7, 14);

        Assert.Equal(0, GoalService.MonthsUntil(from, from));
        Assert.Equal(6, GoalService.MonthsUntil(from, new DateOnly(2027, 1, 14)));
        // One day short of 6 full months: still only 5 whole months have elapsed.
        Assert.Equal(5, GoalService.MonthsUntil(from, new DateOnly(2027, 1, 13)));
    }

    [Fact]
    public void MonthsUntilClampsToTheSupportedProjectionRange()
    {
        var from = new DateOnly(2026, 7, 14);

        // A target date in the past clamps to 0, not a negative count.
        Assert.Equal(0, GoalService.MonthsUntil(from, new DateOnly(2020, 1, 1)));
        // A target decades away clamps to the 600-month ceiling ProjectionService supports.
        Assert.Equal(600, GoalService.MonthsUntil(from, new DateOnly(2100, 1, 1)));
    }

    [Fact]
    public void PointAtOrAfterReturnsTheFirstPointOnOrAfterTheTargetDate()
    {
        var points = new List<ProjectionPoint>
        {
            Point(new DateOnly(2026, 1, 1), netWorth: 1m),
            Point(new DateOnly(2026, 2, 1), netWorth: 2m),
            Point(new DateOnly(2026, 3, 1), netWorth: 3m),
        };

        var result = GoalService.PointAtOrAfter(points, new DateOnly(2026, 1, 15));

        Assert.Equal(2m, result.NetWorth);
    }

    [Fact]
    public void PointAtOrAfterFallsBackToTheLastPointWhenTheTargetDateIsBeyondTheSeries()
    {
        var points = new List<ProjectionPoint>
        {
            Point(new DateOnly(2026, 1, 1), netWorth: 1m),
            Point(new DateOnly(2026, 2, 1), netWorth: 2m),
        };

        var result = GoalService.PointAtOrAfter(points, new DateOnly(2030, 1, 1));

        Assert.Equal(2m, result.NetWorth);
    }
}
