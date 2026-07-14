using OpenWealth.Api.Models;

namespace OpenWealth.Api.Contracts.Requests;

public record LedgerEntryRequest(
    DateOnly Date,
    string Description,
    decimal Amount,
    LedgerAccountType AccountType,
    Guid AccountId);
