//----------------------------------------------------------------
//  ActivityButton.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// A link button under the rich presence, which everyone but the player sees.
/// </summary>
/// <param name="Label">Text on the button.</param>
/// <param name="Url">Address the button opens.</param>
internal sealed record ActivityButton(string Label, string Url);
