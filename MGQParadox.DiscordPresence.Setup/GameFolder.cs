//----------------------------------------------------------------
//  GameFolder.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Diagnostics;
using System.IO;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// The Monster Girl Quest! Paradox RPG folder the mod was extracted into.
/// </summary>
internal sealed class GameFolder
{
    /// <summary>
    /// File name of the translation's patch script.
    /// </summary>
    public const string PatchFileName = "Patch.rb";

    /// <summary>
    /// File name of the copy kept of the translation's own patch script.
    /// </summary>
    public const string PatchBackupFileName = "Patch.rb.backup";

    /// <summary>
    /// Creates the folder.
    /// </summary>
    /// <param name="root">Full path of the game folder.</param>
    private GameFolder(string root)
    {
        Root = root;
    }

    /// <summary>
    /// Full path of the game folder.
    /// </summary>
    public string Root { get; }

    /// <summary>
    /// Full path of Game.exe.
    /// </summary>
    public string ExecutablePath => Path.Combine(Root, "Game.exe");

    /// <summary>
    /// Full path of the translation's patch script.
    /// </summary>
    public string PatchPath => Path.Combine(Root, "Patch", PatchFileName);

    /// <summary>
    /// Full path of the copy kept of the translation's own patch script.
    /// </summary>
    public string PatchBackupPath => Path.Combine(Root, "Patch", PatchBackupFileName);

    /// <summary>
    /// Finds the game folder the mod folder sits in.
    /// </summary>
    /// <param name="problem">What the player has to fix, when the folder is not usable.</param>
    /// <returns>The game folder, or <see langword="null"/> when the layout is wrong.</returns>
    public static GameFolder? Locate(out string problem)
    {
        var folderName = Path.GetFileName(ModFolder.Root);

        if (!string.Equals(folderName, ModFolder.Name, StringComparison.OrdinalIgnoreCase))
        {
            problem = $"This folder must be named \"{ModFolder.Name}\" (it is \"{folderName}\").";
            return null;
        }

        if (!File.Exists(ModFolder.PathOf(ModFolder.GameScriptFileName)))
        {
            problem = $"{ModFolder.GameScriptFileName} is missing from the {ModFolder.Name} folder. Please extract the whole download again.";
            return null;
        }

        var game = new GameFolder(Path.GetDirectoryName(ModFolder.Root)!);

        if (!File.Exists(game.ExecutablePath))
        {
            problem = $"Game.exe was not found next to the {ModFolder.Name} folder.\n\n" +
                      $"Extract the whole download (the {ModFolder.Name} folder and DiscordPatcher.bat) into your " +
                      "Monster Girl Quest! Paradox RPG folder (the one containing Game.exe) and run DiscordPatcher.bat again.";
            return null;
        }

        if (!File.Exists(game.PatchPath))
        {
            problem = "Patch\\Patch.rb was not found. Please install the English translation first.";
            return null;
        }

        problem = string.Empty;
        return game;
    }

    /// <summary>
    /// Reports whether this folder's Game.exe is running.
    /// </summary>
    /// <returns><see langword="true"/> when it is, or when that cannot be told.</returns>
    /// <remarks>
    /// A process whose path cannot be read counts as running, patching under a running game is worse.
    /// </remarks>
    public bool IsGameRunning()
    {
        foreach (var process in Process.GetProcessesByName("Game"))
        {
            try
            {
                if (string.Equals(process.MainModule.FileName, ExecutablePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch
            {
                return true;
            }
        }

        return false;
    }
}
