namespace OpenWealth.Api.Models;

/// <summary>
/// A target for one of the wealth figures already tracked (net worth,
/// savings, ...) by a given date. Progress is never stored — it's computed
/// on read from the same <see cref="Services.ProjectionService"/> that
/// drives the Trends chart, so a goal always reflects current balances,
/// rates and repayments rather than going stale.
/// </summary>
public class Goal
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public GoalMetric Metric { get; set; }
    public decimal TargetAmount { get; set; }
    public DateOnly TargetDate { get; set; }
}
