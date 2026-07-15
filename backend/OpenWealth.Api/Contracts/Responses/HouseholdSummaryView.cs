namespace OpenWealth.Api.Contracts.Responses;

/// <summary>
/// <see cref="Members"/> holds one visibility-shaped view per active member —
/// still <see cref="object"/> per entry, since each is one of
/// WealthSummaryExtensions.ToShareView's tiers. See the note there.
/// </summary>
public record HouseholdSummaryView(decimal TotalNetWorth, IReadOnlyList<object> Members);
