namespace OpenWealth.Api.Models;

/// <summary>
/// UK income tax and National Insurance parameters, stored per user so they can
/// be updated each tax year. Seeded with 2025/26 values.
/// </summary>
public class TaxSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TaxYearLabel { get; set; }

    public decimal PersonalAllowance { get; set; }
    /// <summary>Income above this tapers the personal allowance (£1 lost per £2 over).</summary>
    public decimal PersonalAllowanceTaperThreshold { get; set; }
    /// <summary>Taxable income up to this is charged at the basic rate.</summary>
    public decimal BasicRateLimit { get; set; }
    /// <summary>Taxable income up to this (and above the basic limit) is charged at the higher rate.</summary>
    public decimal HigherRateLimit { get; set; }
    public decimal BasicRatePercent { get; set; }
    public decimal HigherRatePercent { get; set; }
    public decimal AdditionalRatePercent { get; set; }

    public decimal NiPrimaryThresholdAnnual { get; set; }
    public decimal NiUpperEarningsLimitAnnual { get; set; }
    public decimal NiMainRatePercent { get; set; }
    public decimal NiUpperRatePercent { get; set; }
}
