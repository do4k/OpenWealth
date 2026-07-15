using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.NetWorthOnly"/> tier of the public profile response.</summary>
public record PublicProfileNetWorthOnlyView(
    string DisplayName, ShareVisibility Visibility, decimal NetWorth,
    IEnumerable<object> History, IEnumerable<object> Projection)
    : NetWorthOnlyShareView(DisplayName, Visibility, NetWorth);
