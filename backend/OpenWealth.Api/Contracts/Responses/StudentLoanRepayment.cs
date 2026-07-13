using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Responses;

public record StudentLoanRepayment(StudentLoanPlan Plan, decimal AnnualRepayment);
