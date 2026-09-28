//----------------------------------------------------------------
//  LaunchRegistration.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

using System;
using Microsoft.Win32;

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// Tells Discord how to start the game, so accepting an invite while it is closed opens it.
/// </summary>
/// <remarks>
/// Discord starts a game by its application's URL scheme, <c>discord-&lt;application id&gt;://</c>,
/// which Windows looks up under the current user without admin rights. The game that started last
/// owns it, which matters when several game folders are installed.
/// </remarks>
internal static class LaunchRegistration
{
    /// <summary>
    /// Points the application's URL scheme at the running Game.exe.
    /// </summary>
    /// <param name="clientId">The Discord application.</param>
    public static void Register(string clientId)
    {
        try
        {
            if (!OperatingSystem.IsWindows() || Environment.ProcessPath is not { } game)
            {
                return;
            }

            using var scheme = Registry.CurrentUser.CreateSubKey($@"Software\Classes\discord-{clientId}");
            scheme.SetValue(null, $"URL:Run game {clientId} protocol");
            scheme.SetValue("URL Protocol", string.Empty);

            using (var icon = scheme.CreateSubKey("DefaultIcon"))
            {
                icon.SetValue(null, game);
            }

            using (var command = scheme.CreateSubKey(@"shell\open\command"))
            {
                command.SetValue(null, $"\"{game}\"");
            }

            Log.Write($"launch registered: {game}");
        }
        catch (Exception ex)
        {
            Log.Write($"launch registration failed: {ex.Message}");
        }
    }
}
