namespace OpenWealth.Api.Models;

/// <summary>
/// A manually recorded one-off cash movement against a savings account,
/// investment or custom asset — a lump-sum injection or payout applied
/// immediately, rather than through payday automation. Kept as an audit
/// trail: deleting the entry reverses its effect on the account's balance.
/// </summary>
public class LedgerEntry
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly Date { get; set; }
    public required string Description { get; set; }

    /// <summary>Positive for a cash injection (deposit), negative for a payout (withdrawal).</summary>
    public decimal Amount { get; set; }
    public LedgerAccountType AccountType { get; set; }
    public Guid AccountId { get; set; }
}
