using OpenWealth.Api.Contracts.Responses;
using OpenWealth.Api.Models;

namespace OpenWealth.Api.Extensions;

public static class WealthSummaryExtensions
{
    /// <summary>
    /// Shapes a wealth summary down to what a given visibility tier allows,
    /// alongside whose it is. Shared by the public profile endpoint and the
    /// household summary — each field a tier permits is genuinely absent
    /// from the response at a lower tier, not just hidden by the UI.
    /// Returns <see cref="object"/> rather than a common base type on purpose:
    /// each branch's concrete record type only carries its own tier's fields,
    /// and the caller passes the result straight to Results.Ok/Json, whose
    /// JSON serialization is driven by the runtime type when the static type
    /// is object — serializing through a shared base type here would silently
    /// drop every field the base type doesn't declare.
    /// </summary>
    public static object ToShareView(this WealthSummary s, string displayName, ShareVisibility visibility) =>
        visibility switch
        {
            ShareVisibility.NetWorthOnly => new NetWorthOnlyShareView(displayName, visibility, s.NetWorth),
            ShareVisibility.CategoryTotals => new CategoryTotalsShareView(
                displayName, visibility, s.NetWorth, s.TotalAssets, s.TotalLiabilities, s.AssetTotals, s.LiabilityTotals),
            _ => new FullBreakdownShareView(
                displayName, visibility, s.NetWorth, s.TotalAssets, s.TotalLiabilities, s.AssetTotals, s.LiabilityTotals, s.Items),
        };
}
