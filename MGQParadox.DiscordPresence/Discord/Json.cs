//----------------------------------------------------------------
//  Json.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Described the writer as part of the DLL, not an executable
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// The little JSON the IPC payloads need, written by hand so the NativeAOT DLL needs no serializer.
/// </summary>
internal static class Json
{
    /// <summary>
    /// Writes a string as a JSON string literal.
    /// </summary>
    /// <param name="value">The text to quote, <see langword="null"/> reads as empty.</param>
    /// <returns>The escaped text, including the surrounding quotes.</returns>
    public static string Quote(string? value)
    {
        var literal = new StringBuilder("\"");

        foreach (var character in value ?? string.Empty)
        {
            switch (character)
            {
                case '"':
                    literal.Append("\\\"");
                    break;
                case '\\':
                    literal.Append("\\\\");
                    break;
                case '\n':
                    literal.Append("\\n");
                    break;
                case '\r':
                    literal.Append("\\r");
                    break;
                case '\t':
                    literal.Append("\\t");
                    break;
                case < ' ':
                    literal.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    break;
                default:
                    literal.Append(character);
                    break;
            }
        }

        return literal.Append('"').ToString();
    }
}
