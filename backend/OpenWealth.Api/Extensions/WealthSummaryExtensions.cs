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
    /// </summary>
    public static object ToShareView(this WealthSummary s, string displayName, ShareVisibility visibility) =>
        visibility switch
        {
            ShareVisibility.NetWorthOnly => new
            {
                DisplayName = displayName,
                Visibility = visibility,
                s.NetWorth,
            },
            ShareVisibility.CategoryTotals => new
            {
                DisplayName = displayName,
                Visibility = visibility,
                s.NetWorth,
                s.TotalAssets,
                s.TotalLiabilities,
                s.AssetTotals,
                s.LiabilityTotals,
            },
            _ => new
            {
                DisplayName = displayName,
                Visibility = visibility,
                s.NetWorth,
                s.TotalAssets,
                s.TotalLiabilities,
                s.AssetTotals,
                s.LiabilityTotals,
                s.Items,
            },
        };
}
