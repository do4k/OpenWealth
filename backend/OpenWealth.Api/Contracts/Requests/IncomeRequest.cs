using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record IncomeRequest(
    decimal AnnualSalary,
    decimal AnnualBonus,
    decimal EmployeePensionPercent,
    decimal EmployerPensionPercent,
    PensionMethod PensionMethod,
    bool PensionOnBonus,
    int ChildrenReceivingChildBenefit);
