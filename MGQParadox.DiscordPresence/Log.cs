//----------------------------------------------------------------
//  Log.cs
//
//  Changelog:
//      Paulinchen  2026-10-06: Wrote the log into the game folder's Logs folder
//      Paulinchen  2026-09-29: Kept the DLL's threads from writing at once
//      Paulinchen  2026-09-27: Named the DLL as the only writer, the uninstaller is gone
//      Paulinchen  2026-09-26: Named the uninstaller as the second writer
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// DiscordPresence.log in the game folder's Logs folder, written by the DLL.
/// </summary>
/// <remarks>
/// Never throws, since a failing log must not take down the code writing it, least of all the game.
/// </remarks>
internal static class Log
{
    /// <summary>
    /// Full path of the log file.
    /// </summary>
    private static string FilePath => ModFolder.LogPathOf("DiscordPresence.log");

    /// <summary>
    /// Keeps the game thread, the presence loop, the pipe reader and the update check from writing at once.
    /// </summary>
    /// <remarks>
    /// Two writes at once fail with the file in use, which would lose the very line that says what went wrong.
    /// </remarks>
    private static readonly object Gate = new();

    /// <summary>
    /// Appends a line, prefixed with the time of day.
    /// </summary>
    /// <param name="message">The line to append.</param>
    public static void Write(string message)
    {
        var time = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);

        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.AppendAllText(FilePath, $"{time}  {message}\r\n", Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Deletes the log once it has grown past a size, so it starts over.
    /// </summary>
    /// <param name="maxBytes">The size the log may reach.</param>
    public static void ClearIfLargerThan(long maxBytes)
    {
        try
        {
            lock (Gate)
            {
                if (File.Exists(FilePath) && new FileInfo(FilePath).Length > maxBytes)
                {
                    File.Delete(FilePath);
                }
            }
        }
        catch
        {
        }
    }
}
