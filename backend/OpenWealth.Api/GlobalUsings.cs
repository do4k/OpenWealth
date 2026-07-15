// Named aliases for the discriminated unions used across the ShareVisibility
// tiering (WealthSummaryExtensions/TrendPointExtensions.ToShareView,
// ShareEndpoints.BuildPublicView) so those signatures read as three named
// shapes instead of a wall of generic arguments repeated per file.
global using ShareTierView = OneOf.OneOf<
    OpenWealth.Api.Contracts.Responses.NetWorthOnlyShareView,
    OpenWealth.Api.Contracts.Responses.CategoryTotalsShareView,
    OpenWealth.Api.Contracts.Responses.FullBreakdownShareView>;
global using TrendPointTierView = OneOf.OneOf<
    OpenWealth.Api.Contracts.Responses.TrendPointNetWorthOnlyView,
    OpenWealth.Api.Contracts.Responses.TrendPointCategoryTotalsView,
    OpenWealth.Api.Contracts.Responses.TrendPointFullBreakdownView>;
global using PublicProfileTierView = OneOf.OneOf<
    OpenWealth.Api.Contracts.Responses.PublicProfileNetWorthOnlyView,
    OpenWealth.Api.Contracts.Responses.PublicProfileCategoryTotalsView,
    OpenWealth.Api.Contracts.Responses.PublicProfileFullBreakdownView>;
