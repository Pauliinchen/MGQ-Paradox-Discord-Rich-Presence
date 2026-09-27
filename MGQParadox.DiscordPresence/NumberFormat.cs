//----------------------------------------------------------------
//  NumberFormat.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using System.Globalization;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Numbers the way the presence writes them.
/// </summary>
internal static class NumberFormat
{
    /// <summary>
    /// Groups the thousands with commas: 1234567 becomes "1,234,567".
    /// </summary>
    /// <param name="number">The number.</param>
    /// <returns>The grouped number.</returns>
    public static string Grouped(long number) => number.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>
    /// Writes a count together with its noun: "1 battle", "2,345 battles".
    /// </summary>
    /// <param name="number">The count.</param>
    /// <param name="singular">The noun for exactly one.</param>
    /// <param name="plural">The noun for any other count, the singular with an "s" when left out.</param>
    /// <returns>The count with its noun.</returns>
    public static string Counted(long number, string singular, string? plural = null) =>
        $"{Grouped(number)} {(number == 1 ? singular : plural ?? singular + "s")}";

    /// <summary>
    /// Writes a count as an ordinal: 1st, 2nd, 3rd, 4th, 11th, 1,021st.
    /// </summary>
    /// <param name="number">The count.</param>
    /// <returns>The ordinal, with thousands grouped.</returns>
    public static string Ordinal(long number)
    {
        var suffix = (number % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            },
        };

        return Grouped(number) + suffix;
    }
}
