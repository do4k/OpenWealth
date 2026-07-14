namespace OpenWealth.Api.Models;

/// <summary>
/// Where a debt's monthly payment gets redirected once it's fully paid off.
/// Shared between <see cref="Mortgage"/> and <see cref="CustomDebt"/>.
/// </summary>
public enum ReinvestDestinationType
{
    None = 0,
    Savings = 1,
    Investment = 2,
}
