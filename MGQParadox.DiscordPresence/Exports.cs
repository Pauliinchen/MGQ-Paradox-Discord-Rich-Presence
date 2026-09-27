//----------------------------------------------------------------
//  Exports.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Let the game script start the update check and read the newer release it found
//      Paulinchen  2026-09-26: Named the game script by its new file name
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// The functions GameScript/Discord_RPC.rb calls through Win32API.
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

    /// <summary>
    /// Starts asking GitHub for a newer release. Calling it again does nothing.
    /// </summary>
    /// <returns>1 when started, 0 when it failed.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_check_for_update", CallConvs = [typeof(CallConvStdcall)])]
    public static int CheckForUpdate()
    {
        try
        {
            UpdateCheck.Start();
            return 1;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_check_for_update failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Hands out the version of a newer release, once the check found one.
    /// </summary>
    /// <param name="buffer">Receives the version, UTF-8 and null-terminated.</param>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>The length of the version, 0 while there is none or it does not fit.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_newer_version", CallConvs = [typeof(CallConvStdcall)])]
    public static int NewerVersion(byte* buffer, int size)
    {
        try
        {
            var version = Encoding.UTF8.GetBytes(UpdateCheck.NewerVersion ?? string.Empty);

            if (version.Length == 0 || version.Length >= size)
            {
                return 0;
            }

            version.CopyTo(new Span<byte>(buffer, size));
            buffer[version.Length] = 0;
            return version.Length;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_newer_version failed: {ex}");
            return 0;
        }
    }
}
