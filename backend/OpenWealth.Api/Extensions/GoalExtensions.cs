using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

namespace OpenWealth.Api.Extensions;

public static class GoalExtensions
{
    /// <summary>
    /// Shapes a goal for API responses with its current value, its projected
    /// value at the target date, and whether that projection is on track —
    /// all computed against the given projection rather than stored.
    /// </summary>
    public static object ToResponse(this Goal g, IReadOnlyList<ProjectionPoint> points)
    {
        var current = GoalService.ValueOf(points[0], g.Metric);
        var atTarget = GoalService.PointAtOrAfter(points, g.TargetDate);
        var projected = GoalService.ValueOf(atTarget, g.Metric);
        return new
        {
            g.Id,
            g.Name,
            g.Metric,
            g.TargetAmount,
            g.TargetDate,
            CurrentValue = current,
            ProjectedValue = projected,
            OnTrack = GoalService.IsOnTrack(g.Metric, projected, g.TargetAmount),
        };
    }
}
