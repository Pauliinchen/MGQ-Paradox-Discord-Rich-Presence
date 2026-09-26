//----------------------------------------------------------------
//  Installer.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Windows.Forms;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// What DiscordPatcher.bat opens: installing, repairing or removing the loader in Patch/Patch.rb.
/// </summary>
internal sealed class Installer
{
    /// <summary>
    /// Caption of every dialog.
    /// </summary>
    private const string Title = "Monster Girl Quest! Paradox RPG - Discord Rich Presence";

    /// <summary>
    /// How the setup was asked to run.
    /// </summary>
    private readonly SetupMode _mode;

    /// <summary>
    /// Creates the setup.
    /// </summary>
    /// <param name="mode">How the setup was asked to run.</param>
    private Installer(SetupMode mode)
    {
        _mode = mode;
    }

    /// <summary>
    /// Whether the player is asked and told in dialogs.
    /// </summary>
    private bool ShowsDialogs => _mode == SetupMode.Interactive;

    /// <summary>
    /// Runs the setup.
    /// </summary>
    /// <param name="mode">How the setup was asked to run.</param>
    /// <returns>The exit code of the process.</returns>
    public static int Run(SetupMode mode) => new Installer(mode).Run();

    /// <summary>
    /// Checks the game folder, then does what the mode or the player asks for.
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

            var patch = PatchLoader.Read(game.PatchPath);
            var isInstalled = PatchLoader.IsInstalled(patch);

            return _mode switch
            {
                SetupMode.Install => Install(game, patch, isInstalled),
                SetupMode.Uninstall => Uninstall(game, patch, isInstalled),
                _ when isInstalled => AskRepairOrUninstall(game, patch),
                _ => AskInstall(game, patch),
            };
        }
        catch (Exception ex)
        {
            return Fail($"Something went wrong:\n\n{ex.GetType().Name}: {ex.Message}\n\nPatch\\Patch.rb was not changed.");
        }
    }

    /// <summary>
    /// Offers to install the loader.
    /// </summary>
    /// <param name="game">The game folder.</param>
    /// <param name="patch">The current content of Patch.rb.</param>
    /// <returns>The exit code of the process.</returns>
    private int AskInstall(GameFolder game, string patch)
    {
        var answer = MessageBox.Show(
            "Install Discord Rich Presence for Monster Girl Quest! Paradox RPG?\n\n" +
            $"This adds a small loader to Patch\\Patch.rb (a backup is kept as {GameFolder.PatchBackupFileName}).\n" +
            "Nothing else of the game or the translation is changed.",
            Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        return answer == DialogResult.Yes ? Install(game, patch, false) : 0;
    }

    /// <summary>
    /// Offers to repair or remove an installed loader.
    /// </summary>
    /// <param name="game">The game folder.</param>
    /// <param name="patch">The current content of Patch.rb.</param>
    /// <returns>The exit code of the process.</returns>
    private int AskRepairOrUninstall(GameFolder game, string patch)
    {
        var answer = MessageBox.Show(
            "Discord Rich Presence is installed.\n\n" +
            "Yes  =  Reinstall (repair)\n" +
            "No  =  Uninstall\n" +
            "Cancel  =  do nothing",
            Title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

        return answer switch
        {
            DialogResult.Yes => Install(game, patch, true),
            DialogResult.No => Uninstall(game, patch, true),
            _ => 0,
        };
    }

    /// <summary>
    /// Adds the loader to Patch.rb, or renews it.
    /// </summary>
    /// <param name="game">The game folder.</param>
    /// <param name="patch">The current content of Patch.rb.</param>
    /// <param name="isInstalled">Whether the loader is already in Patch.rb.</param>
    /// <returns>The exit code of the process.</returns>
    /// <remarks>
    /// A Patch.rb the game already rejects is left alone, fixing its checksum would hide a broken
    /// download. The backup is only written while the loader is absent, so it holds the translation's
    /// own file.
    /// </remarks>
    private int Install(GameFolder game, string patch, bool isInstalled)
    {
        if (!NWPatchChecksum.IsValid(patch))
        {
            return Fail("Patch\\Patch.rb from the translation is already invalid (its checksum does not match), " +
                        "so the game would not start with it anyway.\n\nPlease re-download the translation, then run this again.");
        }

        if (PatchLoader.ContainsLegacyVersion(patch))
        {
            return Fail("Patch\\Patch.rb contains an older, built-in version of this mod.\n\n" +
                        "Restore the translation's original Patch.rb (or re-install the translation), then run this again.");
        }

        if (!isInstalled)
        {
            PatchLoader.WriteBackup(game.PatchBackupPath, patch);
        }

        PatchLoader.Write(game.PatchPath, PatchLoader.WithLoader(patch));

        return Done("Discord Rich Presence is installed.\n\n" +
                    "Start the game as usual - Discord just needs to be running.\n\n" +
                    "After updating the translation, run DiscordPatcher.bat again (the update removes the loader).");
    }

    /// <summary>
    /// Removes the loader from Patch.rb.
    /// </summary>
    /// <param name="game">The game folder.</param>
    /// <param name="patch">The current content of Patch.rb.</param>
    /// <param name="isInstalled">Whether the loader is in Patch.rb.</param>
    /// <returns>The exit code of the process.</returns>
    private int Uninstall(GameFolder game, string patch, bool isInstalled)
    {
        if (!isInstalled)
        {
            return Done("Discord Rich Presence is not installed - nothing to do.");
        }

        PatchLoader.Write(game.PatchPath, PatchLoader.WithoutLoader(patch));

        return Done("Discord Rich Presence was uninstalled.\n\n" +
                    $"You can now delete the {ModFolder.Name} folder and DiscordPatcher.bat.");
    }

    /// <summary>
    /// Reports a problem.
    /// </summary>
    /// <param name="message">What went wrong and what to do about it.</param>
    /// <returns>The exit code of a failed setup.</returns>
    private int Fail(string message) => Report(message, MessageBoxIcon.Warning, 1);

    /// <summary>
    /// Reports a success.
    /// </summary>
    /// <param name="message">What was done.</param>
    /// <returns>The exit code of a successful setup.</returns>
    private int Done(string message) => Report(message, MessageBoxIcon.Information, 0);

    /// <summary>
    /// Logs an outcome and shows it, unless the setup runs without dialogs.
    /// </summary>
    /// <param name="message">The outcome.</param>
    /// <param name="icon">The icon of the dialog.</param>
    /// <param name="exitCode">The exit code to return.</param>
    /// <returns><paramref name="exitCode"/>.</returns>
    private int Report(string message, MessageBoxIcon icon, int exitCode)
    {
        Log.Write($"setup: {message.Replace("\n", " ")}");

        if (ShowsDialogs)
        {
            MessageBox.Show(message, Title, MessageBoxButtons.OK, icon);
        }

        return exitCode;
    }
}
