namespace NotificationAuditService.Expenses;

/// <summary>
/// Helpers for working with money in integer minor units (cents), avoiding binary rounding drift.
/// Assumes 2-decimal currencies (the common case); currencies with other minor units (e.g. JPY with 0)
/// would need per-currency exponents.
/// </summary>
internal static class MoneyMath
{
    /// <summary>Converts a decimal amount to whole minor units (cents), rounding half away from zero.</summary>
    public static long ToMinor(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    /// <summary>Converts whole minor units (cents) back to a decimal amount.</summary>
    public static decimal FromMinor(long minor) => minor / 100m;
}
