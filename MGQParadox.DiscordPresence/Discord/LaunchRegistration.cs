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
/// The URL scheme sits under the current user, which needs no admin rights, and belongs to the game
/// that started last, which matters when several game folders are installed.
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
