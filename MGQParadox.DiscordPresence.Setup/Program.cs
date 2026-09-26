//----------------------------------------------------------------
//  Program.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Replaced --install and --uninstall with --silent
//                            - Renamed the executable to Uninstall.exe
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// Entry point of Uninstall.exe.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Argument that uninstalls without asking.
    /// </summary>
    private const string SilentArgument = "--silent";

    /// <summary>
    /// Runs the uninstaller the way the first argument asks for.
    /// </summary>
    /// <param name="args">The command line arguments.</param>
    /// <returns>The exit code of the process.</returns>
    [STAThread]
    private static int Main(string[] args)
    {
        var argument = args.Length > 0 ? args[0].ToLowerInvariant() : string.Empty;

        return Uninstaller.Run(argument == SilentArgument ? SetupMode.Silent : SetupMode.Interactive);
    }
}
