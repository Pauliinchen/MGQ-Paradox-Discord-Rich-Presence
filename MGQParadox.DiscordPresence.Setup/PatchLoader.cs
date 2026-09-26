//----------------------------------------------------------------
//  PatchLoader.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Stopped adding the loader block, it is only removed now
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// The loader block the earlier versions added to Patch/Patch.rb, which loaded Discord/rpc.rb.
/// </summary>
internal static class PatchLoader
{
    /// <summary>
    /// First line of the block.
    /// </summary>
    private const string BeginMarker = "# >>> MGQ Discord RPC";

    /// <summary>
    /// Last line of the block.
    /// </summary>
    private const string EndMarker = "# <<< MGQ Discord RPC";

    /// <summary>
    /// The byte order mark an editor may have put in front of Patch.rb.
    /// </summary>
    private const char ByteOrderMark = '﻿';

    /// <summary>
    /// UTF-8 without a byte order mark.
    /// </summary>
    /// <remarks>
    /// The game reads the checksum from the very first characters, where a byte order mark breaks it.
    /// </remarks>
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    /// <summary>
    /// Matches the block from its first marker to the end of its last marker line.
    /// </summary>
    private static readonly Regex LoaderPattern = new(
        "^" + Regex.Escape(BeginMarker) + ".*?^" + Regex.Escape(EndMarker) + "[^\\n]*\\n?",
        RegexOptions.Multiline | RegexOptions.Singleline);

    /// <summary>
    /// Reads Patch.rb.
    /// </summary>
    /// <param name="path">The path of Patch.rb.</param>
    /// <returns>The content, without a byte order mark.</returns>
    public static string Read(string path)
    {
        var patch = File.ReadAllText(path, Encoding.UTF8);

        return patch.Length > 0 && patch[0] == ByteOrderMark ? patch.Substring(1) : patch;
    }

    /// <summary>
    /// Replaces Patch.rb, verifying the checksum first.
    /// </summary>
    /// <param name="path">The path of Patch.rb.</param>
    /// <param name="patch">The new content.</param>
    /// <remarks>
    /// Written to a temporary file first, so a failure never leaves a Patch.rb the game rejects.
    /// </remarks>
    public static void Write(string path, string patch)
    {
        if (!NWPatchChecksum.IsValid(patch))
        {
            throw new InvalidOperationException("checksum self-check failed");
        }

        var temporaryPath = path + ".tmp";

        File.WriteAllText(temporaryPath, patch, Utf8WithoutBom);
        File.Replace(temporaryPath, path, null);
    }

    /// <summary>
    /// Reports whether Patch.rb holds the block.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns><see langword="true"/> when the block is present.</returns>
    public static bool IsPresent(string patch) => LoaderPattern.IsMatch(patch);

    /// <summary>
    /// Removes the block.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns>The new content, with the checksum updated.</returns>
    public static string WithoutLoader(string patch) =>
        NWPatchChecksum.WithChecksumLine(LoaderPattern.Replace(patch, string.Empty));
}
