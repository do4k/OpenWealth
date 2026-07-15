using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

/// <summary>The <see cref="ShareVisibility.NetWorthOnly"/> tier of <see cref="TrendPointExtensions.ToShareView"/>.</summary>
public record TrendPointNetWorthOnlyView(DateOnly Date, decimal NetWorth);
