using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record GoalRequest(
    string Name,
    GoalMetric Metric,
    decimal TargetAmount,
    DateOnly TargetDate);
