//----------------------------------------------------------------
//  ActivityParty.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// The party an activity invites to, which Discord shows as "1 of 2" and offers to join.
/// </summary>
/// <param name="Id">Names the party, the same for everyone in it.</param>
/// <param name="Size">How many are in it.</param>
/// <param name="Max">How many fit in.</param>
internal sealed record ActivityParty(string Id, int Size, int Max);
