//----------------------------------------------------------------
//  Exports.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// The functions GameScript/rpc.rb calls through Win32API.
/// </summary>
/// <remarks>
/// Nothing may throw out of these. An exception crossing into the game ends it.
/// </remarks>
internal static unsafe class Exports
{
    /// <summary>
    /// Starts the presence. Calling it again does nothing.
    /// </summary>
    /// <returns>1 when started, 0 when it failed.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_start", CallConvs = [typeof(CallConvStdcall)])]
    public static int Start()
    {
        try
        {
            if (NativeMethods.FileOfModuleContaining((nint)(delegate* unmanaged[Stdcall]<int>)&Start) is { } dll)
            {
                ModFolder.SetRoot(Path.GetDirectoryName(dll)!);
            }

            PresenceLoop.Start();
            return 1;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_start failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Hands over the game's latest status.
    /// </summary>
    /// <param name="status">The <c>key=value</c> lines, UTF-8 and null-terminated.</param>
    /// <returns>1 when taken, 0 when it failed.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_update", CallConvs = [typeof(CallConvStdcall)])]
    public static int Update(byte* status)
    {
        try
        {
            PresenceLoop.Submit(Marshal.PtrToStringUTF8((nint)status) ?? string.Empty);
            return 1;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_update failed: {ex}");
            return 0;
        }
    }
}
