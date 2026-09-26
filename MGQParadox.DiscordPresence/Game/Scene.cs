//----------------------------------------------------------------
//  Scene.cs
//
//  Changelog:
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Game;

/// <summary>
/// What the game is showing, as rpc.rb reports it.
/// </summary>
internal enum Scene
{
    /// <summary>
    /// Walking around a map, and the fallback for anything unknown.
    /// </summary>
    Map,

    /// <summary>
    /// The title or load screen, before a save is loaded.
    /// </summary>
    Title,

    /// <summary>
    /// A fight.
    /// </summary>
    Battle,

    /// <summary>
    /// The world map, including a menu opened there.
    /// </summary>
    Travel,

    /// <summary>
    /// Any menu outside the world map.
    /// </summary>
    Menu,
}
