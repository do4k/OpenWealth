namespace OpenWealth.Api.Models;

public enum InvestmentType
{
    StocksAndSharesIsa = 0,
    GeneralInvestmentAccount = 1,
    PensionPot = 2,
    LifetimeIsa = 3,
    Crypto = 4,
    Other = 5,
}

public class Investment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string Name { get; set; }
    public InvestmentType Type { get; set; }
    public decimal CurrentValue { get; set; }
    /// <summary>Assumed annual growth used only for projections; balances are never changed automatically.</summary>
    public decimal? ExpectedAnnualGrowthPercent { get; set; }
}
