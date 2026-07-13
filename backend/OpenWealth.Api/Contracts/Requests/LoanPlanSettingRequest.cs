using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record LoanPlanSettingRequest(
    StudentLoanPlan Plan,
    decimal AnnualRepaymentThreshold,
    decimal RepaymentRatePercent,
    decimal InterestRatePercent);
