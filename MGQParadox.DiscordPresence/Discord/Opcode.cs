//----------------------------------------------------------------
//  Opcode.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// The kind of a frame on Discord's IPC pipe, sent ahead of its length and body.
/// </summary>
internal enum Opcode
{
    /// <summary>
    /// First frame of a connection, naming the application.
    /// </summary>
    Handshake = 0,

    /// <summary>
    /// A command, or Discord's answer to one.
    /// </summary>
    Frame = 1,

    /// <summary>
    /// Discord ends the connection.
    /// </summary>
    Close = 2,

    /// <summary>
    /// Discord checks that the client is still there.
    /// </summary>
    Ping = 3,

    /// <summary>
    /// The answer to a ping, echoing its body.
    /// </summary>
    Pong = 4,
}
