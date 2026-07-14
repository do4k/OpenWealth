namespace OpenWealth.Api.Models;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public IncomeDetails? Income { get; set; }
    public TaxSettings? TaxSettings { get; set; }
    public ShareSettings? ShareSettings { get; set; }
    public List<StudentLoanPlanSetting> StudentLoanPlanSettings { get; set; } = [];
    public List<StudentLoan> StudentLoans { get; set; } = [];
    public List<Property> Properties { get; set; } = [];
    public List<Mortgage> Mortgages { get; set; } = [];
    public List<SavingsAccount> SavingsAccounts { get; set; } = [];
    public List<Investment> Investments { get; set; } = [];
    public List<CustomAsset> CustomAssets { get; set; } = [];
    public List<CustomDebt> CustomDebts { get; set; } = [];
}
