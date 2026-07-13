namespace OpenWealth.Api.Contracts.Responses;

/// <summary>
/// What adjusted net income means for free childcare and child benefit.
/// </summary>
/// <param name="LosesFreeChildcare">True when adjusted net income exceeds the free childcare / Tax-Free Childcare limit.</param>
/// <param name="ChildcareHeadroom">How far below the childcare limit you are (negative when over it).</param>
/// <param name="HicbcPercent">Percentage of child benefit clawed back by the High Income Child Benefit Charge.</param>
public record FamilyBenefits(
    decimal AdjustedNetIncome,
    decimal ChildcareIncomeLimit,
    bool LosesFreeChildcare,
    decimal ChildcareHeadroom,
    int ChildrenReceivingChildBenefit,
    decimal AnnualChildBenefit,
    decimal HicbcPercent,
    decimal HicbcCharge,
    decimal NetChildBenefit);
