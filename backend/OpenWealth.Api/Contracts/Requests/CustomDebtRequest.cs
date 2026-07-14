namespace OpenWealth.Api.Contracts.Requests;

public record CustomDebtRequest(
    string Name, decimal Balance, decimal? AnnualInterestRatePercent, decimal? MonthlyPayment);
