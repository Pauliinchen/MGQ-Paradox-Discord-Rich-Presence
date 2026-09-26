//----------------------------------------------------------------
//  ActivityBuilder.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Showed who the player is talking to during a conversation
//                            - Showed a running battle fuck, with the monster girl who challenged Luka
//                            - Showed the player setting up for camp while the camp music plays
//                            - Showed a running request or defeat scene, with who plays it and how often it happened
//                            - Showed the player as idle after a minute without a button press
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using MGQParadox.DiscordPresence.Discord;
using MGQParadox.DiscordPresence.Game;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Turns a game status into the activity shown on Discord.
/// </summary>
internal static class ActivityBuilder
{
    /// <summary>
    /// Name of the dungeon, as it appears in map names.
    /// </summary>
    private const string LabyrinthName = "Labyrinth of Chaos";

    /// <summary>
    /// Name of the home base, as it appears in map names.
    /// </summary>
    private const string PocketCastleName = "Pocket Castle";

    /// <summary>
    /// What the world map is called, since its own name is untranslated kanji.
    /// </summary>
    private const string WorldMapName = "Traveling the world";

    /// <summary>
    /// What the player is shown doing after <see cref="IdleAfter"/> without a button press.
    /// </summary>
    private const string IdleText = "Idle . . .";

    /// <summary>
    /// What the player is shown doing while the camp music plays.
    /// </summary>
    private const string CampText = "Setting up for Camp . . .";

    /// <summary>
    /// Time without a button press after which the player counts as idle.
    /// </summary>
    private static readonly TimeSpan IdleAfter = TimeSpan.FromMinutes(1);

    /// <summary>
    /// What the player is shown doing in the Pocket Castle, one at a time.
    /// </summary>
    private static readonly string[] PocketCastleLines =
    {
        "Not knowing what to do next...",
        "Rearranging the party...",
        "Wondering how to build a character...",
        "Currently in crafting hell...",
        "Trying to buy out Vanilla's stock...",
        "Befriending some Companions...",
        "Getting lost...",
    };

    /// <summary>
    /// Number of Pocket Castle lines to rotate through.
    /// </summary>
    public static int PocketCastleLineCount => PocketCastleLines.Length;

    /// <summary>
    /// Builds the activity for a game status.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="largeImage">The picture the tooltip hangs off, if there is one.</param>
    /// <param name="triviaIndex">Which trivia line to show, wrapping around.</param>
    /// <param name="pocketCastleIndex">Which Pocket Castle line to show, wrapping around.</param>
    /// <returns>The activity.</returns>
    public static Activity Build(GameStatus status, string? largeImage, int triviaIndex, int pocketCastleIndex)
    {
        if (status.Scene == Scene.Title)
        {
            return new Activity
            {
                Details = "At the title screen",
                StartedAt = status.StartedAt,
                LargeImage = largeImage,
            };
        }

        return new Activity
        {
            Details = DetailsOf(status, pocketCastleIndex),
            State = TriviaAt(status.Trivia, triviaIndex),
            StartedAt = status.StartedAt,
            LargeImage = largeImage,
            LargeText = TooltipOf(status),
        };
    }

    /// <summary>
    /// Builds the first line: where the player is and what they are doing.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="pocketCastleIndex">Which Pocket Castle line to show, wrapping around.</param>
    /// <returns>The first line.</returns>
    /// <remarks>
    /// The world map's own name is untranslated kanji, so a fight there names the journey instead.
    /// Battles, the Labyrinth, the Pocket Castle, camp and conversations never show idle, since
    /// auto-battle and their own lines say more than it would.
    /// </remarks>
    private static string DetailsOf(GameStatus status, int pocketCastleIndex)
    {
        if (status.Scene == Scene.Request)
        {
            return WithPlace(status, $"In a request with {status.RequestCharacter} for the {Ordinal(status.RequestCount)} time!");
        }

        if (status.Scene == Scene.DefeatScene)
        {
            return WithPlace(status, $"Raped by {status.RapedBy} for the {Ordinal(status.RapedCount)} time!");
        }

        if (status.Scene == Scene.Battlefuck)
        {
            return WithPlace(status, $"Currently Battlefucking {status.BattlefuckPartner}!");
        }

        if (status.IsInLabyrinth)
        {
            return LabyrinthDetailsOf(status);
        }

        if (status.IsCamping && status.Scene != Scene.Battle && !Mentions(status.Area, PocketCastleName))
        {
            var place = status.Scene == Scene.Travel ? WorldMapName : status.Area;

            return place.Length > 0 ? $"{place} - {CampText}" : CampText;
        }

        if (status.ConversationPartner.Length > 0)
        {
            return WithPlace(status, $"Talking to {status.ConversationPartner} . . .");
        }

        if (status.Scene == Scene.Travel)
        {
            return $"{WorldMapName} - {(IsIdle(status) ? IdleText : TravelText(status.Vehicle))}";
        }

        if (status.Scene == Scene.Battle && status.IsOnWorldMap)
        {
            return $"{WorldMapName} - {StateText(Scene.Battle)}";
        }

        if (status.Scene != Scene.Battle && Mentions(status.Area, PocketCastleName))
        {
            return $"{PocketCastleName} - {PocketCastleLines[pocketCastleIndex % PocketCastleLines.Length]}";
        }

        var state = status.Scene != Scene.Battle && IsIdle(status) ? IdleText : StateText(status.Scene);

        return status.Area.Length > 0 ? $"{status.Area} - {state}" : state;
    }

    /// <summary>
    /// Reports whether the player has pressed no button for <see cref="IdleAfter"/>.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns><see langword="true"/> when idle.</returns>
    /// <remarks>
    /// Measured against the DLL's own clock, which keeps running while the game is frozen in the
    /// background and publishes nothing.
    /// </remarks>
    private static bool IsIdle(GameStatus status) =>
        status.LastInputAt is { } lastInputAt &&
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastInputAt >= IdleAfter.TotalSeconds;

    /// <summary>
    /// Builds the first line inside the Labyrinth of Chaos.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>Kind of run, biome, floor and rare points.</returns>
    /// <remarks>
    /// The biome is left out at the entrance, which is itself called Labyrinth of Chaos.
    /// </remarks>
    private static string LabyrinthDetailsOf(GameStatus status)
    {
        var biome = status.Area.Length > 0 && !Mentions(status.Area, LabyrinthName)
            ? $" ({status.Area})"
            : string.Empty;

        return $"{LabyrinthName} {status.LabyrinthType}{biome} - Floor {status.LabyrinthFloor} | {status.LabyrinthRarePoints} Rare Points!";
    }

    /// <summary>
    /// Puts the place in front of what happens there.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="happening">What happens.</param>
    /// <returns>The first line.</returns>
    /// <remarks>
    /// Every room of the Pocket Castle has a name of its own, so the place is the castle as a whole.
    /// </remarks>
    private static string WithPlace(GameStatus status, string happening)
    {
        var place = status.IsOnWorldMap || status.Scene == Scene.Travel ? WorldMapName
            : Mentions(status.Area, PocketCastleName) ? PocketCastleName
            : status.Area;

        return place.Length > 0 ? $"{place} - {happening}" : happening;
    }

    /// <summary>
    /// Picks the trivia line for the second line.
    /// </summary>
    /// <param name="trivia">The trivia lines that currently apply.</param>
    /// <param name="index">Which line to show, wrapping around.</param>
    /// <returns>The line, or an empty string when there is none.</returns>
    private static string TriviaAt(IReadOnlyList<string> trivia, int index) =>
        trivia.Count > 0 ? trivia[index % trivia.Count] : string.Empty;

    /// <summary>
    /// Builds the tooltip on the picture: leader, race and class with their levels.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The tooltip, leaving out whatever is unknown.</returns>
    private static string TooltipOf(GameStatus status)
    {
        var parts = new List<string>();

        if (status.LeaderName.Length > 0)
        {
            parts.Add($"Party leader: {WithLevel(status.LeaderName, status.LeaderLevel)}");
        }

        if (status.RaceName.Length > 0)
        {
            parts.Add($"Race: {WithLevel(status.RaceName, status.RaceLevel)}");
        }

        if (status.ClassName.Length > 0)
        {
            parts.Add($"Class: {WithLevel(status.ClassName, status.ClassLevel)}");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Describes what the player is doing on a map.
    /// </summary>
    /// <param name="scene">What the game is showing.</param>
    /// <returns>The description.</returns>
    private static string StateText(Scene scene) => scene switch
    {
        Scene.Battle => "In combat!",
        Scene.Menu => "In menu . . .",
        _ => "Exploring . . .",
    };

    /// <summary>
    /// Describes how the party crosses the world map.
    /// </summary>
    /// <param name="vehicle">The way of travelling.</param>
    /// <returns>The description.</returns>
    private static string TravelText(Vehicle vehicle) => vehicle switch
    {
        Vehicle.Sea => "Sailing . . .",
        Vehicle.Air => "Flying . . .",
        _ => "On foot . . .",
    };

    /// <summary>
    /// Appends a level to a name, when there is one.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="level">The level, possibly empty.</param>
    /// <returns>The name with its level.</returns>
    private static string WithLevel(string name, string level) =>
        level.Length > 0 ? $"{name} Lv {level}" : name;

    /// <summary>
    /// Writes a count as an ordinal: 1st, 2nd, 3rd, 4th, 11th, 1,021st.
    /// </summary>
    /// <param name="number">The count.</param>
    /// <returns>The ordinal, with thousands grouped like the trivia.</returns>
    private static string Ordinal(int number)
    {
        var suffix = (number % 100) switch
        {
            11 or 12 or 13 => "th",
            _ => (number % 10) switch
            {
                1 => "st",
                2 => "nd",
                3 => "rd",
                _ => "th",
            },
        };

        return number.ToString("N0", CultureInfo.InvariantCulture) + suffix;
    }

    /// <summary>
    /// Reports whether a map name contains a place name, ignoring case.
    /// </summary>
    /// <param name="area">The map name.</param>
    /// <param name="place">The place name to look for.</param>
    /// <returns><see langword="true"/> when the place is part of the map name.</returns>
    private static bool Mentions(string area, string place) =>
        area.IndexOf(place, StringComparison.OrdinalIgnoreCase) >= 0;
}
