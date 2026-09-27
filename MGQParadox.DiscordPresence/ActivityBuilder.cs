//----------------------------------------------------------------
//  ActivityBuilder.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Left the names, map names and route names that spoil Part 3 out while spoilers are hidden
//                            - Showed the default art asset whenever the game picks no picture, instead of the looked-up app icon
//                            - Called the Angelic Dominion and Monster Realm routes Destroyer and Judgment, after their logos
//                            - Put the act of the Collaboration Scenario in front of the tooltip while it is played
//                            - Put the part of the story in front of the tooltip, dropping the labels when it gets too long
//                            - Showed the art asset the Picture option picked instead of the app icon
//      Paulinchen  2026-09-26: Showed who the player is talking to during a conversation
//                            - Showed a running battle fuck with the battlefucker
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
    /// Who the player is talking to while spoilers are hidden.
    /// </summary>
    private const string SpoilerFreeSpeaker = "someone";

    /// <summary>
    /// Who plays a request while spoilers are hidden.
    /// </summary>
    private const string SpoilerFreeCompanion = "a companion";

    /// <summary>
    /// Who plays a defeat scene while spoilers are hidden.
    /// </summary>
    private const string SpoilerFreeMonsterGirl = "a monster girl";

    /// <summary>
    /// Who plays a battle fuck while spoilers are hidden.
    /// </summary>
    private const string SpoilerFreeBattlefucker = "a battlefucker";

    /// <summary>
    /// What stands between the parts of the tooltip.
    /// </summary>
    private const string TooltipSeparator = " | ";

    /// <summary>
    /// Art asset shown whenever the game picks no picture of its own: the game's icon.
    /// </summary>
    private const string DefaultPicture = "default";

    /// <summary>
    /// Time without a button press after which the player counts as idle.
    /// </summary>
    private static readonly TimeSpan IdleAfter = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Names of the sides a playthrough chooses, by the key the game script publishes.
    /// </summary>
    private static readonly Dictionary<string, string> SideNames = new()
    {
        ["ilias"] = "Ilias",
        ["alice"] = "Alice",
    };

    /// <summary>
    /// Names of the final chapter's routes, by the key the game script publishes.
    /// </summary>
    private static readonly Dictionary<string, string> RouteNames = new()
    {
        ["destroyer"] = "Destroyer",
        ["judgment"] = "Judgment",
        ["chaos"] = "Chaos",
    };

    /// <summary>
    /// Names of the final chapter's routes while spoilers are hidden, by the key the game script
    /// publishes.
    /// </summary>
    private static readonly Dictionary<string, string> SpoilerFreeRouteNames = new()
    {
        ["destroyer"] = "Monster",
        ["judgment"] = "Angel",
        ["chaos"] = "Third",
    };

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
    /// <param name="triviaIndex">Which trivia line to show, wrapping around.</param>
    /// <param name="pocketCastleIndex">Which Pocket Castle line to show, wrapping around.</param>
    /// <returns>The activity.</returns>
    public static Activity Build(GameStatus status, int triviaIndex, int pocketCastleIndex)
    {
        if (status.Scene == Scene.Title)
        {
            return new Activity
            {
                Details = "At the title screen",
                StartedAt = status.StartedAt,
                LargeImage = DefaultPicture,
            };
        }

        return new Activity
        {
            Details = DetailsOf(status, pocketCastleIndex),
            State = TriviaAt(status.Trivia, triviaIndex),
            StartedAt = status.StartedAt,
            LargeImage = status.Picture.Length > 0 ? status.Picture : DefaultPicture,
            LargeText = TooltipOf(status),
        };
    }

    /// <summary>
    /// Builds the first line: where the player is and what they are doing.
    /// </summary>
    /// <remarks>
    /// The world map's own name is untranslated kanji, so a fight there names the journey instead.
    /// Battles, the Labyrinth, the Pocket Castle, camp and conversations never show idle, since
    /// auto-battle and their own lines say more than it would.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <param name="pocketCastleIndex">Which Pocket Castle line to show, wrapping around.</param>
    /// <returns>The first line.</returns>
    private static string DetailsOf(GameStatus status, int pocketCastleIndex)
    {
        if (status.Scene == Scene.Request)
        {
            var companion = NameOf(status, status.RequestCharacter, SpoilerFreeCompanion);

            return WithPlace(status, $"In a request with {companion} for the {Ordinal(status.RequestCount)} time!");
        }

        if (status.Scene == Scene.DefeatScene)
        {
            var monsterGirl = NameOf(status, status.RapedBy, SpoilerFreeMonsterGirl);

            return WithPlace(status, $"Raped by {monsterGirl} for the {Ordinal(status.RapedCount)} time!");
        }

        if (status.Scene == Scene.Battlefuck)
        {
            return WithPlace(status, $"Currently Battlefucking {NameOf(status, status.BattlefuckPartner, SpoilerFreeBattlefucker)}!");
        }

        if (status.IsInLabyrinth)
        {
            return LabyrinthDetailsOf(status);
        }

        if (status.IsCamping && status.Scene != Scene.Battle && !Mentions(status.Area, PocketCastleName))
        {
            var place = status.Scene == Scene.Travel ? WorldMapName : AreaOf(status);

            return place.Length > 0 ? $"{place} - {CampText}" : CampText;
        }

        if (status.ConversationPartner.Length > 0)
        {
            return WithPlace(status, $"Talking to {NameOf(status, status.ConversationPartner, SpoilerFreeSpeaker)} . . .");
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

        var area = AreaOf(status);

        return area.Length > 0 ? $"{area} - {state}" : state;
    }

    /// <summary>
    /// Names the current map on the first line, or leaves it out while spoilers are hidden.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The map's name, or an empty string.</returns>
    private static string AreaOf(GameStatus status) =>
        status.HidesSpoilers ? string.Empty : status.Area;

    /// <summary>
    /// Names a character on the first line, or stands in for them while spoilers are hidden.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="name">The character's name.</param>
    /// <param name="spoilerFreeName">What stands in for the name while spoilers are hidden.</param>
    /// <returns>The name, or its stand-in.</returns>
    private static string NameOf(GameStatus status, string name, string spoilerFreeName) =>
        status.HidesSpoilers ? spoilerFreeName : name;

    /// <summary>
    /// Reports whether the player has pressed no button for <see cref="IdleAfter"/>.
    /// </summary>
    /// <remarks>
    /// Measured against the DLL's own clock, which keeps running while the game is frozen in the
    /// background and publishes nothing.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <returns><see langword="true"/> when idle.</returns>
    private static bool IsIdle(GameStatus status) =>
        status.LastInputAt is { } lastInputAt &&
        DateTimeOffset.UtcNow.ToUnixTimeSeconds() - lastInputAt >= IdleAfter.TotalSeconds;

    /// <summary>
    /// Builds the first line inside the Labyrinth of Chaos.
    /// </summary>
    /// <remarks>
    /// The biome is left out at the entrance, which is itself called Labyrinth of Chaos.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <returns>Kind of run, biome, floor and rare points.</returns>
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
    /// <remarks>
    /// Every room of the Pocket Castle has a name of its own, so the place is the castle as a whole.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <param name="happening">What happens.</param>
    /// <returns>The first line.</returns>
    private static string WithPlace(GameStatus status, string happening)
    {
        var place = status.IsOnWorldMap || status.Scene == Scene.Travel ? WorldMapName
            : Mentions(status.Area, PocketCastleName) ? PocketCastleName
            : AreaOf(status);

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
    /// Builds the tooltip on the picture: the part of the story, then leader, race and class with
    /// their levels.
    /// </summary>
    /// <remarks>
    /// Discord wraps the tooltip on its own and cuts it at <see cref="Activity.MaxTextLength"/>, so
    /// a tooltip too long for it drops the labels before <see cref="Activity"/> has to cut it.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <returns>The tooltip, leaving out whatever is unknown.</returns>
    private static string TooltipOf(GameStatus status)
    {
        var tooltip = TooltipOf(status, withLabels: true);

        return tooltip.Length <= Activity.MaxTextLength ? tooltip : TooltipOf(status, withLabels: false);
    }

    /// <summary>
    /// Builds the tooltip on the picture, with or without the labels of leader, race and class.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="withLabels">Whether "Party leader:", "Race:" and "Class:" name the values.</param>
    /// <returns>The tooltip, leaving out whatever is unknown.</returns>
    private static string TooltipOf(GameStatus status, bool withLabels)
    {
        var parts = new List<string>();

        if (StoryOf(status) is { Length: > 0 } story)
        {
            parts.Add(story);
        }

        if (status.LeaderName.Length > 0)
        {
            parts.Add(Labeled("Party leader", WithLevel(status.LeaderName, status.LeaderLevel), withLabels));
        }

        if (status.RaceName.Length > 0)
        {
            parts.Add(Labeled("Race", WithLevel(status.RaceName, status.RaceLevel), withLabels));
        }

        if (status.ClassName.Length > 0)
        {
            parts.Add(Labeled("Class", WithLevel(status.ClassName, status.ClassLevel), withLabels));
        }

        return string.Join(TooltipSeparator, parts);
    }

    /// <summary>
    /// Describes where the story stands: "Part 1: Ilias side", "Part 3: Chaos route" ("Part 3: Third
    /// route" while spoilers are hidden), "Collaboration Scenario: Act 5".
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The act of the Collaboration Scenario while it is played, else the part with the route in the final chapter or with the side, or an empty string when the part is unknown.</returns>
    private static string StoryOf(GameStatus status)
    {
        if (status.CollabAct > 0)
        {
            return $"Collaboration Scenario: Act {status.CollabAct}";
        }

        if (status.Part == 0)
        {
            return string.Empty;
        }

        var part = $"Part {status.Part}";

        var routeNames = status.HidesSpoilers ? SpoilerFreeRouteNames : RouteNames;

        if (routeNames.TryGetValue(status.Route, out var route))
        {
            return $"{part}: {route} route";
        }

        return SideNames.TryGetValue(status.Side, out var side) ? $"{part}: {side} side" : part;
    }

    /// <summary>
    /// Puts a label in front of a value, when wanted.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="value">The value.</param>
    /// <param name="withLabel">Whether to put the label in front.</param>
    /// <returns>The value, labeled or not.</returns>
    private static string Labeled(string label, string value, bool withLabel) =>
        withLabel ? $"{label}: {value}" : value;

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
