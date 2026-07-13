namespace OpenWealth.Api.Contracts.Responses;

public record TakeHomeBreakdown(
    decimal GrossIncome,
    decimal EmployeePensionContribution,
    decimal EmployerPensionContribution,
    decimal AdjustedNetIncome,
    decimal PersonalAllowance,
    decimal TaxableIncome,
    decimal IncomeTax,
    decimal NationalInsurance,
    List<StudentLoanRepayment> StudentLoanRepayments,
    decimal TotalStudentLoanRepayments,
    decimal AnnualTakeHome,
    decimal MonthlyTakeHome,
    FamilyBenefits FamilyBenefits);
