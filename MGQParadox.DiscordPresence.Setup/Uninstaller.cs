//----------------------------------------------------------------
//  Uninstaller.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Turned the setup into the uninstaller, renamed from Installer.cs
//                            - Removed the installation of the earlier versions, their block in Patch.rb last
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Windows.Forms;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// Discord/Uninstall.exe: removing the mod and whatever the earlier versions left behind.
/// </summary>
internal sealed class Uninstaller
{
    /// <summary>
    /// Caption of every dialog.
    /// </summary>
    private const string Title = "Monster Girl Quest! Paradox RPG - Discord Rich Presence";

    /// <summary>
    /// File name of the game script of the earlier versions, which sat in the mod folder.
    /// </summary>
    private const string LegacyScriptFileName = "rpc.rb";

    /// <summary>
    /// How the uninstaller was asked to run.
    /// </summary>
    private readonly SetupMode _mode;

    /// <summary>
    /// Creates the uninstaller.
    /// </summary>
    /// <param name="mode">How the uninstaller was asked to run.</param>
    private Uninstaller(SetupMode mode)
    {
        _mode = mode;
    }

    /// <summary>
    /// Whether the player is asked and told in dialogs.
    /// </summary>
    private bool ShowsDialogs => _mode == SetupMode.Interactive;

    /// <summary>
    /// Full path of the game script of the earlier versions.
    /// </summary>
    private static string LegacyScriptPath => ModFolder.PathOf(LegacyScriptFileName);

    /// <summary>
    /// Runs the uninstaller.
    /// </summary>
    /// <param name="mode">How the uninstaller was asked to run.</param>
    /// <returns>The exit code of the process.</returns>
    public static int Run(SetupMode mode) => new Uninstaller(mode).Run();

    /// <summary>
    /// Checks the game folder, asks the player unless silent, then uninstalls.
    /// </summary>
    /// <returns>The exit code of the process.</returns>
    private int Run()
    {
        try
        {
            var game = GameFolder.Locate(out var problem);

            if (game == null)
            {
                return Fail(problem);
            }

            if (game.IsGameRunning())
            {
                return Fail("Please close Monster Girl Quest! Paradox RPG first, then run this again.");
            }

            var patch = File.Exists(game.PatchPath) ? PatchLoader.Read(game.PatchPath) : string.Empty;
            var hasLegacyLoader = PatchLoader.IsPresent(patch);

            if (!File.Exists(game.ModScriptPath) && !hasLegacyLoader
                && !File.Exists(LegacyScriptPath) && !File.Exists(game.LegacyPatcherPath))
            {
                return Done("Discord Rich Presence is not installed - nothing to do.\n\n" +
                            $"You can delete the {ModFolder.Name} folder.");
            }

            if (hasLegacyLoader && !NWPatchChecksum.IsValid(patch))
            {
                return Fail("Patch\\Patch.rb holds a block of an earlier version of this mod, but the file is already " +
                            "invalid (its checksum does not match), so it is left alone.\n\n" +
                            "Put the community's Patch.rb (\"enable Type 1 mods\" on the MGQ wiki) back into the Patch folder, " +
                            "then run this again.");
            }

            if (ShowsDialogs && !Confirm())
            {
                return 0;
            }

            Uninstall(game, patch, hasLegacyLoader);

            return Done("Discord Rich Presence was uninstalled.\n\n" +
                        $"Delete the {ModFolder.Name} folder to finish. It holds this uninstaller and the per-save statistics.");
        }
        catch (Exception ex)
        {
            return Fail($"Something went wrong:\n\n{ex.GetType().Name}: {ex.Message}\n\n" +
                        "Patch\\Patch.rb was not changed. Run Uninstall.exe again to finish.");
        }
    }

    /// <summary>
    /// Asks the player whether to uninstall.
    /// </summary>
    /// <returns><see langword="true"/> when the player agreed.</returns>
    private static bool Confirm()
    {
        var answer = MessageBox.Show(
            "Uninstall Discord Rich Presence for Monster Girl Quest! Paradox RPG?\n\n" +
            $"This deletes Patch\\{GameFolder.ModScriptFileName}. " +
            "The mod loader in Patch\\Patch.rb stays for your other mods.",
            Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        return answer == DialogResult.Yes;
    }

    /// <summary>
    /// Deletes the mod's script and the earlier versions' files, then removes their block from Patch.rb.
    /// </summary>
    /// <param name="game">The game folder.</param>
    /// <param name="patch">The current content of Patch.rb, empty when there is none.</param>
    /// <param name="hasLegacyLoader">Whether Patch.rb holds the block of the earlier versions.</param>
    /// <remarks>
    /// Patch.rb is written last, so a failure before never leaves it changed. The backup the earlier
    /// versions kept goes once it holds nothing Patch.rb does not.
    /// </remarks>
    private static void Uninstall(GameFolder game, string patch, bool hasLegacyLoader)
    {
        DeleteIfPresent(game.ModScriptPath);
        DeleteIfPresent(LegacyScriptPath);
        DeleteIfPresent(game.LegacyPatcherPath);

        if (!hasLegacyLoader)
        {
            return;
        }

        var cleaned = PatchLoader.WithoutLoader(patch);

        if (File.Exists(game.PatchBackupPath) && WithoutTrailingLineBreaks(PatchLoader.Read(game.PatchBackupPath)) == WithoutTrailingLineBreaks(cleaned))
        {
            File.Delete(game.PatchBackupPath);
        }

        PatchLoader.Write(game.PatchPath, cleaned);
    }

    /// <summary>
    /// Cuts the line breaks off the end of a text.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The text without trailing line breaks.</returns>
    /// <remarks>
    /// Adding the block may have added a line break in front of it, which removing it leaves behind.
    /// </remarks>
    private static string WithoutTrailingLineBreaks(string text) => text.TrimEnd('\r', '\n');

    /// <summary>
    /// Deletes a file, when it exists.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Reports a problem.
    /// </summary>
    /// <param name="message">What went wrong and what to do about it.</param>
    /// <returns>The exit code of a failed uninstall.</returns>
    private int Fail(string message) => Report(message, MessageBoxIcon.Warning, 1);

    /// <summary>
    /// Reports a success.
    /// </summary>
    /// <param name="message">What was done.</param>
    /// <returns>The exit code of a successful uninstall.</returns>
    private int Done(string message) => Report(message, MessageBoxIcon.Information, 0);

    /// <summary>
    /// Logs an outcome and shows it, unless the uninstaller runs silently.
    /// </summary>
    /// <param name="message">The outcome.</param>
    /// <param name="icon">The icon of the dialog.</param>
    /// <param name="exitCode">The exit code to return.</param>
    /// <returns><paramref name="exitCode"/>.</returns>
    private int Report(string message, MessageBoxIcon icon, int exitCode)
    {
        Log.Write($"uninstall: {message.Replace("\n", " ")}");

        if (ShowsDialogs)
        {
            MessageBox.Show(message, Title, MessageBoxButtons.OK, icon);
        }

        return exitCode;
    }
}
