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
    /// <summary>
    /// When true (only meaningful for <see cref="InvestmentType.PensionPot"/>), payday
    /// automation adds a month of the employee + employer pension contributions from
    /// the income page to this investment. At most one investment per user can have
    /// this set — it's enforced by the endpoint, not the database, since it mirrors
    /// the single employment record income currently models.
    /// </summary>
    public bool ReceivesIncomePensionContributions { get; set; }
}
