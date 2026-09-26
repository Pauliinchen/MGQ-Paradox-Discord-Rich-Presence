//----------------------------------------------------------------
//  Settings.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Dropped large_image, the picture is always the application icon
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Text;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Settings.ini in the mod folder, the one file a player is meant to edit.
/// </summary>
internal static class Settings
{
    /// <summary>
    /// Full path of the settings file.
    /// </summary>
    private static string FilePath => ModFolder.PathOf("Settings.ini");

    /// <summary>
    /// The Discord application the presence is shown for.
    /// </summary>
    public static string? ClientId => Read("client_id");

    /// <summary>
    /// Looks up one <c>key = value</c> line.
    /// </summary>
    /// <param name="key">The key, compared without regard to case.</param>
    /// <returns>The trimmed value, or <see langword="null"/> when the key is absent.</returns>
    private static string? Read(string key)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return null;
            }

            foreach (var rawLine in File.ReadAllLines(FilePath, Encoding.UTF8))
            {
                var line = rawLine.Trim();
                var separator = line.IndexOf('=');

                if (IsCommentOrSection(line) || separator <= 0)
                {
                    continue;
                }

                if (string.Equals(line.Substring(0, separator).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    return line.Substring(separator + 1).Trim();
                }
            }
        }
        catch
        {
        }

        return null;
    }

    /// <summary>
    /// Reports whether a trimmed line carries no setting.
    /// </summary>
    /// <param name="line">The trimmed line.</param>
    /// <returns><see langword="true"/> for a blank line, a comment or a section header.</returns>
    private static bool IsCommentOrSection(string line) =>
        line.Length == 0 || line.StartsWith("#") || line.StartsWith(";") || line.StartsWith("[");
}
