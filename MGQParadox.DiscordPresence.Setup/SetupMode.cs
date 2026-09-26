//----------------------------------------------------------------
//  SetupMode.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Replaced Install and Uninstall with Silent
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// How the uninstaller was asked to run.
/// </summary>
internal enum SetupMode
{
    /// <summary>
    /// Ask the player first, and report the outcome in a dialog.
    /// </summary>
    Interactive,

    /// <summary>
    /// Uninstall without any dialog.
    /// </summary>
    Silent,
}
