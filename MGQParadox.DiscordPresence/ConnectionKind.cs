//----------------------------------------------------------------
//  ConnectionKind.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Where the player's connection with a friend stands, as another mod reports it.
/// </summary>
internal enum ConnectionKind
{
    /// <summary>
    /// No connection, the activity shows the trivia.
    /// </summary>
    None,

    /// <summary>
    /// The player hosts and waits for a friend, who can be invited.
    /// </summary>
    Hosting,

    /// <summary>
    /// The player plays with a friend.
    /// </summary>
    Connected,
}
