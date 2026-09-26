//----------------------------------------------------------------
//  Vehicle.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Game;

/// <summary>
/// How the party is crossing the world map.
/// </summary>
internal enum Vehicle
{
    /// <summary>
    /// Walking, and the fallback for anything unknown.
    /// </summary>
    Foot,

    /// <summary>
    /// By boat or ship.
    /// </summary>
    Sea,

    /// <summary>
    /// By airship.
    /// </summary>
    Air,
}
