namespace OpenWealth.Api.Extensions;

public static class DecimalExtensions
{
    /// <summary>
    /// The app-wide money rounding rule: two decimal places (pence), midpoints
    /// away from zero.
    /// </summary>
    public static decimal RoundToPence(this decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
