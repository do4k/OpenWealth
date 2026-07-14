namespace OpenWealth.Api.Models;

/// <summary>
/// Which kind of account a one-off <see cref="LedgerEntry"/> was applied to.
/// </summary>
public enum LedgerAccountType
{
    Savings = 0,
    Investment = 1,
    CustomAsset = 2,
}
