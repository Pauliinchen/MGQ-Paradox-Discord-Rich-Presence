//----------------------------------------------------------------
//  PatchLoader.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace MGQParadox.DiscordPresence.Setup;

/// <summary>
/// The marked loader block that installing appends to Patch/Patch.rb.
/// </summary>
/// <remarks>
/// The loaded script is not checksummed, so updating the mod never touches Patch.rb again.
/// </remarks>
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
    /// The module of the very first version, which was pasted into Patch.rb itself.
    /// </summary>
    private const string LegacyModuleDeclaration = "module MGQ_Discord";

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
    /// The lines of the block, markers included.
    /// </summary>
    private static readonly string[] LoaderLines =
    {
        BeginMarker + " (do not edit - managed by DiscordPatcher.bat)",
        "begin",
        $"  __mgq_rpc = \"{ModFolder.Name}/{ModFolder.GameScriptFileName}\"",
        "  if File.exist?(__mgq_rpc)",
        "    eval(File.open(__mgq_rpc, \"rb\") { |f| f.read }.force_encoding(\"UTF-8\"), TOPLEVEL_BINDING, __mgq_rpc)",
        "  end",
        "rescue Exception",
        "end",
        EndMarker,
    };

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
    /// Keeps a copy of the translation's own Patch.rb.
    /// </summary>
    /// <param name="path">The path of the backup.</param>
    /// <param name="patch">The content to keep.</param>
    public static void WriteBackup(string path, string patch) => File.WriteAllText(path, patch, Utf8WithoutBom);

    /// <summary>
    /// Reports whether Patch.rb holds the block.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns><see langword="true"/> when the block is present.</returns>
    public static bool IsInstalled(string patch) => LoaderPattern.IsMatch(patch);

    /// <summary>
    /// Reports whether Patch.rb holds the very first version of the mod, pasted in directly.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns><see langword="true"/> when the old module is present outside the block.</returns>
    public static bool ContainsLegacyVersion(string patch) => StripLoader(patch).Contains(LegacyModuleDeclaration);

    /// <summary>
    /// Appends the block, replacing any earlier one.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns>The new content, with the checksum updated.</returns>
    public static string WithLoader(string patch)
    {
        var newLine = patch.Contains("\r\n") ? "\r\n" : "\n";
        var stripped = StripLoader(patch);

        if (stripped.Length > 0 && !stripped.EndsWith("\n"))
        {
            stripped += newLine;
        }

        return NWPatchChecksum.WithChecksumLine(stripped + string.Join(newLine, LoaderLines) + newLine);
    }

    /// <summary>
    /// Removes the block.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns>The new content, with the checksum updated.</returns>
    public static string WithoutLoader(string patch) => NWPatchChecksum.WithChecksumLine(StripLoader(patch));

    /// <summary>
    /// Cuts the block out, leaving the checksum as it was.
    /// </summary>
    /// <param name="patch">The content of Patch.rb.</param>
    /// <returns>The content without the block.</returns>
    private static string StripLoader(string patch) => LoaderPattern.Replace(patch, string.Empty);
}
