//----------------------------------------------------------------
//  Exports.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Let another mod report its connection with a friend, take invites and read the player's name
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
/// Nothing may throw out of these, since an exception crossing into the game ends it.
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

    /// <summary>
    /// Takes the connection with a friend that another mod reports, which the activity shows and invites to.
    /// </summary>
    /// <param name="kind">"hosting", "connected", or anything else for none, UTF-8 and null-terminated.</param>
    /// <param name="partyId">Names the party, the same for both players, UTF-8 and null-terminated.</param>
    /// <param name="joinSecret">What a friend who joins gets, while hosting, UTF-8 and null-terminated.</param>
    /// <param name="friend">The friend's name, while connected, UTF-8 and null-terminated.</param>
    /// <returns>1 when taken, 0 when it failed.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_set_connection", CallConvs = [typeof(CallConvStdcall)])]
    public static int SetConnection(byte* kind, byte* partyId, byte* joinSecret, byte* friend)
    {
        try
        {
            Connection.Current.Report(Text(kind), Text(partyId), Text(joinSecret), Text(friend));
            return 1;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_set_connection failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Hands out the join secret of an invite the player accepted in Discord and takes it, unless the
    /// buffer is too small for it.
    /// </summary>
    /// <param name="buffer">Receives the join secret, UTF-8 and null-terminated.</param>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>The join secret's length, or its length negated when the buffer is too small, 0 while none waits.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_take_invite", CallConvs = [typeof(CallConvStdcall)])]
    public static int TakeInvite(byte* buffer, int size)
    {
        try
        {
            if (Connection.Current.PeekInvite() is not { } invite)
            {
                return 0;
            }

            var length = Copy(invite, buffer, size);

            if (length > 0)
            {
                Connection.Current.TakeInvite();
            }

            return length;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_take_invite failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Hands out the player's name on Discord.
    /// </summary>
    /// <param name="buffer">Receives the name, UTF-8 and null-terminated.</param>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>The name's length, 0 while Discord has not told it or it does not fit.</returns>
    [UnmanagedCallersOnly(EntryPoint = "presence_player_name", CallConvs = [typeof(CallConvStdcall)])]
    public static int PlayerName(byte* buffer, int size)
    {
        try
        {
            return Connection.Current.PlayerName is { Length: > 0 } name ? Math.Max(Copy(name, buffer, size), 0) : 0;
        }
        catch (Exception ex)
        {
            Log.Write($"presence_player_name failed: {ex}");
            return 0;
        }
    }

    /// <summary>
    /// Writes a text into a buffer of the game script.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="buffer">Receives the text, UTF-8 and null-terminated.</param>
    /// <param name="size">The size of the buffer in bytes.</param>
    /// <returns>The text's length in bytes, or its length negated when the buffer is too small.</returns>
    private static int Copy(string text, byte* buffer, int size)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        if (bytes.Length >= size)
        {
            return -bytes.Length;
        }

        bytes.CopyTo(new Span<byte>(buffer, size));
        buffer[bytes.Length] = 0;
        return bytes.Length;
    }

    /// <summary>
    /// Reads a text the game script handed over.
    /// </summary>
    /// <param name="text">The text, UTF-8 and null-terminated.</param>
    /// <returns>The text, empty for a null pointer.</returns>
    private static string Text(byte* text) => Marshal.PtrToStringUTF8((nint)text) ?? string.Empty;
}
