//----------------------------------------------------------------
//  PresenceImage.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Decides which picture the presence shows.
/// </summary>
internal static class PresenceImage
{
    /// <summary>
    /// How long Discord gets to answer.
    /// </summary>
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Finds the icon hash in Discord's public application info.
    /// </summary>
    private static readonly Regex IconPattern = new("\"icon\"\\s*:\\s*\"([0-9a-zA-Z_]+)\"");

    /// <summary>
    /// Picks the picture: the one set in Settings.ini, otherwise the application icon.
    /// </summary>
    /// <param name="clientId">The Discord application the presence is shown for.</param>
    /// <returns>An asset key or https address, or <see langword="null"/> when there is none.</returns>
    public static string? Resolve(string clientId)
    {
        var configured = Settings.LargeImage;

        if (!string.IsNullOrEmpty(configured))
        {
            Log.Write($"large_image from Settings.ini: {configured}");
            return configured;
        }

        return LookUpAppIcon(clientId);
    }

    /// <summary>
    /// Looks up the address of the application icon.
    /// </summary>
    /// <param name="clientId">The Discord application.</param>
    /// <returns>The address, or <see langword="null"/> when the application has no icon or Discord is unreachable.</returns>
    private static string? LookUpAppIcon(string clientId)
    {
        try
        {
            var match = IconPattern.Match(Download($"https://discord.com/api/v10/applications/{clientId}/rpc"));

            if (!match.Success)
            {
                Log.Write("app has no icon set - presence will have no image or tooltip");
                return null;
            }

            var url = $"https://cdn.discordapp.com/app-icons/{clientId}/{match.Groups[1].Value}.png?size=512";
            Log.Write($"large_image = app icon {url}");
            return url;
        }
        catch (Exception ex)
        {
            Log.Write($"app icon lookup failed ({ex.Message}) - presence will have no image or tooltip");
            return null;
        }
    }

    /// <summary>
    /// Fetches a text resource.
    /// </summary>
    /// <param name="url">The address to fetch.</param>
    /// <returns>The response body.</returns>
    private static string Download(string url)
    {
        using var client = new HttpClient { Timeout = RequestTimeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("MGQ-Discord-Presence");

        return client.GetStringAsync(url).GetAwaiter().GetResult();
    }
}
