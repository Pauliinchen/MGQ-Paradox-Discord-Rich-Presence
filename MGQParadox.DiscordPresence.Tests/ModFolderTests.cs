//----------------------------------------------------------------
//  ModFolderTests.cs
//
//  Changelog:
//      Paulinchen  2026-10-06: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers the paths of the mod's log files.
/// </summary>
public sealed class ModFolderTests
{
    /// <summary>
    /// Asserts that a log lands in the game folder's Logs folder, one folder above the mod folder.
    /// </summary>
    [Fact]
    public void LogPathOf_IsInTheGameFoldersLogsFolder()
    {
        Assert.Equal(@"C:\Games\MGQ\Logs\DiscordPresence.log", ModFolder.LogPathOf("DiscordPresence.log", @"C:\Games\MGQ\Discord"));
    }
}
