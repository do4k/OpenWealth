namespace OpenWealth.Api.Contracts.Responses;

/// <summary>
/// <see cref="Members"/> holds one visibility-shaped view per active member —
/// a <see cref="ShareTierView"/> per entry, since each is one of
/// WealthSummaryExtensions.ToShareView's tiers. See the note there.
/// </summary>
public record HouseholdSummaryView(decimal TotalNetWorth, IReadOnlyList<ShareTierView> Members);
