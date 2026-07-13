using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record SavingsRequest(
    string Name,
    SavingsAccountType Type,
    decimal Balance,
    decimal? AnnualInterestRatePercent,
    decimal MonthlyDeposit = 0m);
