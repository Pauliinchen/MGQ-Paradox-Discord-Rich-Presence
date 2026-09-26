//----------------------------------------------------------------
//  GameFolder.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Added the paths of the Patch folder, the mod's script in it and the old patcher
//                            - Dropped the checks for the game script and Patch.rb, which uninstalling does not need
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
    /// Name of the folder the game loads Patch.rb from, and the community's mod loader loads mods from.
    /// </summary>
    public const string PatchFolderName = "Patch";

    /// <summary>
    /// File name of the translation's patch script.
    /// </summary>
    public const string PatchFileName = "Patch.rb";

    /// <summary>
    /// File name of the copy the earlier versions kept of the translation's own patch script.
    /// </summary>
    public const string PatchBackupFileName = "Patch.rb.backup";

    /// <summary>
    /// File name of the mod's script in the Patch folder.
    /// </summary>
    public const string ModScriptFileName = "Discord_RPC.rb";

    /// <summary>
    /// File name of the setup launcher the earlier versions put next to Game.exe.
    /// </summary>
    private const string LegacyPatcherFileName = "DiscordPatcher.bat";

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
    /// Full path of the Patch folder.
    /// </summary>
    public string PatchFolder => Path.Combine(Root, PatchFolderName);

    /// <summary>
    /// Full path of the translation's patch script.
    /// </summary>
    public string PatchPath => Path.Combine(PatchFolder, PatchFileName);

    /// <summary>
    /// Full path of the copy the earlier versions kept of the translation's own patch script.
    /// </summary>
    public string PatchBackupPath => Path.Combine(PatchFolder, PatchBackupFileName);

    /// <summary>
    /// Full path of the mod's script in the Patch folder.
    /// </summary>
    public string ModScriptPath => Path.Combine(PatchFolder, ModScriptFileName);

    /// <summary>
    /// Full path of the setup launcher the earlier versions put next to Game.exe.
    /// </summary>
    public string LegacyPatcherPath => Path.Combine(Root, LegacyPatcherFileName);

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

        var game = new GameFolder(Path.GetDirectoryName(ModFolder.Root)!);

        if (!File.Exists(game.ExecutablePath))
        {
            problem = $"Game.exe was not found next to the {ModFolder.Name} folder.\n\n" +
                      $"Run this from the {ModFolder.Name} folder inside your Monster Girl Quest! Paradox RPG folder " +
                      "(the one containing Game.exe).";
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
    /// A process whose path cannot be read counts as running, uninstalling under a running game is worse.
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
