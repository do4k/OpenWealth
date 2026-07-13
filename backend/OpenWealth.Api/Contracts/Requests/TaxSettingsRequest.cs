namespace OpenWealth.Api.Contracts.Requests;

public record TaxSettingsRequest(
    string TaxYearLabel,
    decimal PersonalAllowance,
    decimal PersonalAllowanceTaperThreshold,
    decimal BasicRateLimit,
    decimal HigherRateLimit,
    decimal BasicRatePercent,
    decimal HigherRatePercent,
    decimal AdditionalRatePercent,
    decimal NiPrimaryThresholdAnnual,
    decimal NiUpperEarningsLimitAnnual,
    decimal NiMainRatePercent,
    decimal NiUpperRatePercent,
    decimal ChildcareIncomeLimit,
    decimal HicbcLowerThreshold,
    decimal HicbcUpperThreshold,
    decimal ChildBenefitFirstChildWeekly,
    decimal ChildBenefitAdditionalChildWeekly);
