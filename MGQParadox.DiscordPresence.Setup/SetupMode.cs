//----------------------------------------------------------------
//  SetupMode.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// How the setup was asked to run.
/// </summary>
internal enum SetupMode
{
    /// <summary>
    /// Ask the player what to do, and report the outcome in a dialog.
    /// </summary>
    Interactive,

    /// <summary>
    /// Install or repair the loader without any dialog.
    /// </summary>
    Install,

    /// <summary>
    /// Remove the loader without any dialog.
    /// </summary>
    Uninstall,
}
