//----------------------------------------------------------------
//  Scene.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Added the running request and the running defeat scene
//                            - Named the game script by its new file name
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Game;

/// <summary>
/// What the game is showing, as Discord_RPC.rb reports it.
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

    /// <summary>
    /// A request playing, reported only with the NSFW option on.
    /// </summary>
    Request,

    /// <summary>
    /// A defeat scene after a lost battle, reported only with the NSFW option on.
    /// </summary>
    DefeatScene,
}
