//----------------------------------------------------------------
//  Program.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// Entry point of DiscordPresenceSetup.exe.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Argument that installs the loader without asking.
    /// </summary>
    private const string InstallArgument = "--install";

    /// <summary>
    /// Argument that removes the loader without asking.
    /// </summary>
    private const string UninstallArgument = "--uninstall";

    /// <summary>
    /// Runs the setup the first argument asks for.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code of the process.</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        var argument = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

        return Installer.Run(SetupModeOf(argument));
    }

    /// <summary>
    /// Maps a command line argument to the setup it asks for.
    /// </summary>
    /// <param name="argument">The first argument, lower case, or empty.</param>
    /// <returns>The requested mode, the dialog for anything unknown.</returns>
    private static SetupMode SetupModeOf(string argument) => argument switch
    {
        InstallArgument => SetupMode.Install,
        UninstallArgument => SetupMode.Uninstall,
        _ => SetupMode.Interactive,
    };
}
