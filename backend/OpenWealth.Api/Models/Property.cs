namespace OpenWealth.Api.Models;

public class Property
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public decimal EstimatedValue { get; set; }

    /// <summary>
    /// Assumed annual house-price growth (or decline, if negative), used only
    /// for projections — <see cref="EstimatedValue"/> is never changed
    /// automatically. Same projection-only pattern as
    /// <see cref="CustomAsset.ExpectedAnnualGrowthPercent"/>.
    /// </summary>
    public decimal? ExpectedAnnualGrowthPercent { get; set; }

    public List<Mortgage> Mortgages { get; set; } = [];
}
