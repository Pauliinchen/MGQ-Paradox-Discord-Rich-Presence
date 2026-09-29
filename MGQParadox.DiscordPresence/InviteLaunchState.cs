//----------------------------------------------------------------
//  InviteLaunchState.cs
//
//  Changelog:
//      Paulinchen  2026-09-29: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Where a game Discord started for an invite stands, as the game script reads it by number.
/// </summary>
internal enum InviteLaunchState
{
    /// <summary>
    /// Discord did not start the game.
    /// </summary>
    None = 0,

    /// <summary>
    /// Discord started the game, and its join has not come yet.
    /// </summary>
    Waiting = 1,

    /// <summary>
    /// Discord handed over the join.
    /// </summary>
    Joined = 2,

    /// <summary>
    /// No join came in time, so the invite had ended.
    /// </summary>
    Ended = 3,
}
