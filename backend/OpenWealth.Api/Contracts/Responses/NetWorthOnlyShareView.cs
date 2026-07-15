using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.NetWorthOnly"/> tier of <see cref="WealthSummaryExtensions.ToShareView"/>.</summary>
public record NetWorthOnlyShareView(string DisplayName, ShareVisibility Visibility, decimal NetWorth);
