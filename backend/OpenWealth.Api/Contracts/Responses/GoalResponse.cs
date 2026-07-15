using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

public record GoalResponse(
    Guid Id,
    string Name,
    GoalMetric Metric,
    decimal TargetAmount,
    DateOnly TargetDate,
    decimal CurrentValue,
    decimal ProjectedValue,
    bool OnTrack);
