using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record StudentLoanRequest(StudentLoanPlan Plan, decimal Balance, string? Notes);
