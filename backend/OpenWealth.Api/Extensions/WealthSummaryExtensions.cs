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
    /// A <see cref="ShareTierView"/> (OneOf) rather than a common base type:
    /// each branch's concrete record type only carries its own tier's fields,
    /// and OneOfJsonConverterFactory serializes whichever case is set by its
    /// own runtime type — serializing through a shared base type instead
    /// would silently drop every field the base type doesn't declare.
    /// </summary>
    public static ShareTierView ToShareView(this WealthSummary s, string displayName, ShareVisibility visibility) =>
        visibility switch
        {
            ShareVisibility.NetWorthOnly => new NetWorthOnlyShareView(displayName, visibility, s.NetWorth),
            ShareVisibility.CategoryTotals => new CategoryTotalsShareView(
                displayName, visibility, s.NetWorth, s.TotalAssets, s.TotalLiabilities, s.AssetTotals, s.LiabilityTotals),
            _ => new FullBreakdownShareView(
                displayName, visibility, s.NetWorth, s.TotalAssets, s.TotalLiabilities, s.AssetTotals, s.LiabilityTotals, s.Items),
        };
}
