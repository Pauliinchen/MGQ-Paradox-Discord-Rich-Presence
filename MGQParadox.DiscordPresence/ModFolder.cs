//----------------------------------------------------------------
//  ModFolder.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Left the uninstaller out of the folder's contents, it is gone
//      Paulinchen  2026-09-26: Removed the game script's file name, the script lives in the Patch folder now
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// The Discord folder next to Game.exe, which holds the DLL and everything it reads or writes.
/// </summary>
internal static class ModFolder
{
    /// <summary>
    /// Name the folder must have inside the game folder.
    /// </summary>
    /// <remarks>
    /// The game script finds the DLL by this path, so a renamed folder is never used.
    /// </remarks>
    public const string Name = "Discord";

    /// <summary>
    /// Full path of the folder, without a trailing separator.
    /// </summary>
    public static string Root { get; private set; } = WithoutTrailingSeparator(AppContext.BaseDirectory);

    /// <summary>
    /// Points the folder somewhere else.
    /// </summary>
    /// <param name="root">Full path of the folder.</param>
    public static void SetRoot(string root) => Root = WithoutTrailingSeparator(root);

    /// <summary>
    /// Builds the path of a file inside the folder.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The full path of the file.</returns>
    public static string PathOf(string fileName) => Path.Combine(Root, fileName);

    /// <summary>
    /// Removes a trailing separator from a folder path.
    /// </summary>
    /// <param name="path">The folder path.</param>
    /// <returns>The path without a trailing separator.</returns>
    private static string WithoutTrailingSeparator(string path) => path.TrimEnd('\\', '/');
}
