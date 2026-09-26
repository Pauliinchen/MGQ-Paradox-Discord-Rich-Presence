//----------------------------------------------------------------
//  NWPatchChecksum.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// The checksum on line 1 of Patch/Patch.rb, without which the game refuses to start.
/// </summary>
internal static class NWPatchChecksum
{
    /// <summary>
    /// Finds the declared checksum in line 1.
    /// </summary>
    private static readonly Regex ChecksumLinePattern = new(@"^#\s*([0-9]+)");

    /// <summary>
    /// Reports whether line 1 holds the correct checksum of the rest.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns><see langword="true"/> when the game would accept the file.</returns>
    public static bool IsValid(string patch)
    {
        SplitFirstLine(patch, out var firstLine, out var body, out _);

        var match = ChecksumLinePattern.Match(firstLine);

        return match.Success
            && long.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var declared)
            && declared == Calculate(body);
    }

    /// <summary>
    /// Replaces line 1 with the checksum of the rest.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns>The content with a line 1 of <c># &lt;checksum&gt;</c>.</returns>
    public static string WithChecksumLine(string patch)
    {
        SplitFirstLine(patch, out _, out var body, out var newLine);

        return "# " + Calculate(body).ToString(CultureInfo.InvariantCulture) + newLine + body;
    }

    /// <summary>
    /// Splits off line 1.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <param name="firstLine">Line 1, without its line break.</param>
    /// <param name="body">Everything after line 1.</param>
    /// <param name="newLine">The line break line 1 ends with.</param>
    private static void SplitFirstLine(string patch, out string firstLine, out string body, out string newLine)
    {
        var end = patch.IndexOf('\n');

        firstLine = end >= 0 ? patch.Substring(0, end) : patch;
        body = end >= 0 ? patch.Substring(end + 1) : string.Empty;
        newLine = end > 0 && patch[end - 1] == '\r' ? "\r\n" : "\n";
    }

    /// <summary>
    /// Calculates the checksum of everything after line 1.
    /// </summary>
    /// <param name="body">Everything after line 1.</param>
    /// <returns>The sum of all code points, times the last digit of its square root as Ruby prints it.</returns>
    private static long Calculate(string body)
    {
        long sum = 0;

        for (var index = 0; index < body.Length; index++)
        {
            var character = body[index];

            if (IsSkipped(character))
            {
                continue;
            }

            if (char.IsHighSurrogate(character) && index + 1 < body.Length && char.IsLowSurrogate(body[index + 1]))
            {
                sum += char.ConvertToUtf32(character, body[index + 1]);
                index++;
            }
            else
            {
                sum += character;
            }
        }

        var root = FormatLikeRuby(Math.Sqrt(sum));
        var lastDigit = root[root.Length - 1] - '0';

        return sum * (lastDigit is >= 0 and <= 9 ? lastDigit : 0);
    }

    /// <summary>
    /// Reports whether the checksum skips a character.
    /// </summary>
    /// <param name="character">The character.</param>
    /// <returns><see langword="true"/> for an ASCII digit or ASCII whitespace.</returns>
    /// <remarks>
    /// Listed by hand because Ruby 1.9's <c>\d</c> and <c>\s</c> only match ASCII, unlike .NET's.
    /// </remarks>
    private static bool IsSkipped(char character) =>
        character is >= '0' and <= '9' or ' ' or '\t' or '\r' or '\n' or '\f' or '\v';

    /// <summary>
    /// Formats a number the way Ruby's <c>Float#to_s</c> does.
    /// </summary>
    /// <param name="value">The number.</param>
    /// <returns>The shortest decimal that reads back as the same number, always with a fraction.</returns>
    /// <remarks>
    /// .NET's "R" format ends in a different last digit, and the checksum depends on exactly that digit.
    /// </remarks>
    private static string FormatLikeRuby(double value)
    {
        var culture = CultureInfo.InvariantCulture;
        string? shortest = null;

        for (var precision = 1; precision <= 17 && shortest == null; precision++)
        {
            var candidate = value.ToString("G" + precision, culture);

            if (double.Parse(candidate, culture) == value)
            {
                shortest = candidate;
            }
        }

        var formatted = shortest ?? value.ToString("R", culture);

        if (formatted.IndexOfAny(new[] { 'E', 'e' }) >= 0)
        {
            formatted = value.ToString("0.#################", culture);
        }

        return formatted.Contains(".") ? formatted : formatted + ".0";
    }
}
