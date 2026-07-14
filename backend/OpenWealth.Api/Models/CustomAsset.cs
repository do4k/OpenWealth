namespace OpenWealth.Api.Models;

/// <summary>
/// Anything of value that isn't property, savings or an investment — a car,
/// jewellery, a business stake. Counts toward assets; the optional growth
/// rate (negative for depreciating things like cars) is used by projections
/// only, never applied to the real value automatically.
/// </summary>
public class CustomAsset
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal Value { get; set; }
    public decimal? ExpectedAnnualGrowthPercent { get; set; }
}
