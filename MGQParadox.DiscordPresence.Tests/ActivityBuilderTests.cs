//----------------------------------------------------------------
//  ActivityBuilderTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using System;
using System.Linq;
using MGQParadox.DiscordPresence.Discord;
using MGQParadox.DiscordPresence.Game;
using static MGQParadox.DiscordPresence.Tests.StatusFactory;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers the activity built from a game status: the first line, the trivia, the picture, its
/// tooltip and the buttons.
/// </summary>
public sealed class ActivityBuilderTests
{
    /// <summary>
    /// Asserts that the title screen shows only where the player is and the game's icon.
    /// </summary>
    [Fact]
    public void TitleScreen_ShowsOnlyTheTitle()
    {
        var activity = Build(Status(("scene", "title"), ("battles", 7)));

        Assert.Equal("At the title screen", activity.Details);
        Assert.Equal(string.Empty, activity.State);
        Assert.Equal("default", activity.LargeImage);
    }

    /// <summary>
    /// Asserts that every activity carries the button to the mod's page.
    /// </summary>
    /// <param name="scene">The scene the game published.</param>
    [Theory]
    [InlineData("title")]
    [InlineData("map")]
    public void Buttons_LinkToTheModPage(string scene)
    {
        var button = Assert.Single(Build(Status(("scene", scene))).Buttons);

        Assert.Equal("Get the mod", button.Label);
        Assert.Equal("https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence", button.Url);
    }

    /// <summary>
    /// Asserts the first line of each scene on a map.
    /// </summary>
    /// <param name="scene">The scene the game published.</param>
    /// <param name="expected">The first line.</param>
    [Theory]
    [InlineData("map", "Iliasville - Exploring . . .")]
    [InlineData("menu", "Iliasville - In menu . . .")]
    [InlineData("battle", "Iliasville - In combat!")]
    public void Details_NameTheMapAndTheScene(string scene, string expected) =>
        Assert.Equal(expected, Build(Status(("scene", scene), ("area", "Iliasville"))).Details);

    /// <summary>
    /// Asserts that the world map names the journey and how the party travels.
    /// </summary>
    /// <param name="vehicle">The vehicle the game published.</param>
    /// <param name="expected">The first line.</param>
    [Theory]
    [InlineData("foot", "Traveling the world - On foot . . .")]
    [InlineData("sea", "Traveling the world - Sailing . . .")]
    [InlineData("air", "Traveling the world - Flying . . .")]
    public void Details_NameTheWayOfTravelling(string vehicle, string expected) =>
        Assert.Equal(expected, Build(Status(("scene", "travel"), ("vehicle", vehicle))).Details);

    /// <summary>
    /// Asserts that a fight on the world map names the journey, since the map's own name is kanji.
    /// </summary>
    [Fact]
    public void Details_NameTheJourneyForAFightOnTheWorldMap() =>
        Assert.Equal("Traveling the world - In combat!", Build(Status(("scene", "battle"), ("overworld", 1), ("area", "世界"))).Details);

    /// <summary>
    /// Asserts that the player shows as idle after a minute without a button press, never before.
    /// </summary>
    /// <param name="secondsAgo">How long ago the last button was pressed.</param>
    /// <param name="expected">The first line.</param>
    [Theory]
    [InlineData(59, "Iliasville - Exploring . . .")]
    [InlineData(60, "Iliasville - Idle . . .")]
    public void Details_ShowIdleAfterAMinute(int secondsAgo, string expected) =>
        Assert.Equal(expected, Build(Status(("area", "Iliasville"), ("last_input", Unix(Now) - secondsAgo))).Details);

    /// <summary>
    /// Asserts that battles never show as idle, since auto-battle plays on without a button press.
    /// </summary>
    [Fact]
    public void Details_NeverShowIdleInBattle() =>
        Assert.Equal("Iliasville - In combat!", Build(Status(("scene", "battle"), ("area", "Iliasville"), ("last_input", 1))).Details);

    /// <summary>
    /// Asserts that the camp music shows the party setting up camp.
    /// </summary>
    [Fact]
    public void Details_ShowTheCampWhileItsMusicPlays()
    {
        Assert.Equal("Forest - Setting up for Camp . . .", Build(Status(("area", "Forest"), ("camping", 1))).Details);
        Assert.Equal("Traveling the world - Setting up for Camp . . .", Build(Status(("scene", "travel"), ("camping", 1))).Details);
    }

    /// <summary>
    /// Asserts that a conversation names who the player is talking to.
    /// </summary>
    [Fact]
    public void Details_NameWhoThePlayerTalksTo() =>
        Assert.Equal("Iliasville - Talking to Alice . . .", Build(Status(("area", "Iliasville"), ("talking_to", "Alice"))).Details);

    /// <summary>
    /// Asserts that the Pocket Castle shows as a whole, with one of its own lines.
    /// </summary>
    [Fact]
    public void Details_ShowThePocketCastleWithItsOwnLines() =>
        Assert.Equal("Pocket Castle - Not knowing what to do next...", Build(Status(("area", "Pocket Castle Lobby"))).Details);

    /// <summary>
    /// Asserts the Labyrinth of Chaos line, with the biome except at the entrance.
    /// </summary>
    [Fact]
    public void Details_ShowTheLabyrinthRun()
    {
        (string, object)[] run = { ("loc_floor", 10), ("loc_type", "Carnage"), ("loc_rare", 12345) };

        Assert.Equal("Labyrinth of Chaos Carnage (Forest) - Floor 10 | 12,345 Rare Points!",
                     Build(Status(run.Append(("area", "Forest")).ToArray())).Details);
        Assert.Equal("Labyrinth of Chaos Carnage - Floor 10 | 12,345 Rare Points!",
                     Build(Status(run.Append(("area", "Labyrinth of Chaos")).ToArray())).Details);
    }

    /// <summary>
    /// Asserts the running request, defeat scene and battle fuck, with how often they happened.
    /// </summary>
    [Fact]
    public void Details_NameTheNsfwScenes()
    {
        Assert.Equal("Pocket Castle - In a request with Alice for the 3rd time!",
                     Build(Status(("scene", "request"), ("area", "Pocket Castle Hall"), ("request_with", "Alice"), ("request_count", 3))).Details);
        Assert.Equal("Traveling the world - Raped by Rabbit Girl for the 1st time!",
                     Build(Status(("scene", "defeat_scene"), ("overworld", 1), ("raped_by", "Rabbit Girl"), ("raped_count", 1))).Details);
        Assert.Equal("Sabasa - Currently Battlefucking Sara!",
                     Build(Status(("scene", "battlefuck"), ("area", "Sabasa"), ("battlefuck_with", "Sara"))).Details);
    }

    /// <summary>
    /// Asserts that hidden spoilers leave out the map and name nobody on the first line.
    /// </summary>
    [Fact]
    public void Details_HideNamesAndMapsWhileSpoilersAreHidden()
    {
        Assert.Equal("Talking to someone . . .",
                     Build(Status(("hide_spoilers", 1), ("area", "Hall of Pride"), ("talking_to", "Lazarus"))).Details);
        Assert.Equal("Exploring . . .", Build(Status(("hide_spoilers", 1), ("area", "Hall of Pride"))).Details);
        Assert.Equal("Pocket Castle - In a request with a companion for the 2nd time!",
                     Build(Status(("hide_spoilers", 1), ("scene", "request"), ("area", "Pocket Castle Hall"), ("request_with", "Eden"), ("request_count", 2))).Details);
        Assert.Equal("Raped by a monster girl for the 1st time!",
                     Build(Status(("hide_spoilers", 1), ("scene", "defeat_scene"), ("area", "Hall of Pride"), ("raped_by", "X"), ("raped_count", 1))).Details);
        Assert.Equal("Currently Battlefucking a battlefucker!",
                     Build(Status(("hide_spoilers", 1), ("scene", "battlefuck"), ("area", "Hall of Pride"), ("battlefuck_with", "X"))).Details);
    }

    /// <summary>
    /// Asserts the tooltip: the part of the story, then leader, race and class with their levels.
    /// </summary>
    [Fact]
    public void Tooltip_ShowsTheStoryAndTheLeader()
    {
        (string, object)[] leader =
        {
            ("leader", "Luka"), ("level", 8), ("race", "Human"), ("race_level", 5), ("class", "Informant"), ("class_level", 4),
        };

        Assert.Equal("Part 1: Alice side | Party leader: Luka Lv 8 | Race: Human Lv 5 | Class: Informant Lv 4",
                     Build(Status(leader.Concat(new (string, object)[] { ("part", 1), ("side", "alice") }).ToArray())).LargeText);
        Assert.Equal("Part 3: Chaos route | Party leader: Luka Lv 8 | Race: Human Lv 5 | Class: Informant Lv 4",
                     Build(Status(leader.Concat(new (string, object)[] { ("part", 3), ("side", "alice"), ("route", "chaos") }).ToArray())).LargeText);
        Assert.Equal("Collaboration Scenario: Act 5 | Party leader: Luka Lv 8 | Race: Human Lv 5 | Class: Informant Lv 4",
                     Build(Status(leader.Concat(new (string, object)[] { ("part", 2), ("collab_act", 5) }).ToArray())).LargeText);
    }

    /// <summary>
    /// Asserts the neutral route names while spoilers are hidden.
    /// </summary>
    /// <param name="route">The route the game published.</param>
    /// <param name="expected">The tooltip.</param>
    [Theory]
    [InlineData("destroyer", "Part 3: Monster route")]
    [InlineData("judgment", "Part 3: Angel route")]
    [InlineData("chaos", "Part 3: Third route")]
    public void Tooltip_UsesNeutralRouteNamesWhileSpoilersAreHidden(string route, string expected) =>
        Assert.Equal(expected, Build(Status(("part", 3), ("route", route), ("hide_spoilers", 1))).LargeText);

    /// <summary>
    /// Asserts that a tooltip too long for Discord drops its labels instead of being cut.
    /// </summary>
    [Fact]
    public void Tooltip_DropsTheLabelsWhenTooLong()
    {
        var name = new string('L', 60);
        var tooltip = Build(Status(("part", 2), ("side", "ilias"), ("leader", name), ("level", 999),
                                   ("race", "Dark Goddess"), ("race_level", 20), ("class", "All-Piercer"), ("class_level", 20))).LargeText;

        Assert.Equal($"Part 2: Ilias side | {name} Lv 999 | Dark Goddess Lv 20 | All-Piercer Lv 20", tooltip);
    }

    /// <summary>
    /// Asserts that the picture the game picked shows, and the game's icon otherwise.
    /// </summary>
    [Fact]
    public void Picture_FallsBackToTheGamesIcon()
    {
        Assert.Equal("alice_sealed", Build(Status(("picture", "alice_sealed"))).LargeImage);
        Assert.Equal("default", Build(Status()).LargeImage);
    }

    /// <summary>
    /// Asserts that the trivia index wraps around the lines that apply.
    /// </summary>
    [Fact]
    public void State_WrapsAroundTheTrivia()
    {
        var status = Status(("battles", 7), ("gold", 100));

        Assert.Equal("Has fought 7 battles!", Build(status, triviaIndex: 0).State);
        Assert.Equal("Currently carrying 100 gold!", Build(status, triviaIndex: 1).State);
        Assert.Equal("Has fought 7 battles!", Build(status, triviaIndex: 2).State);
    }

    /// <summary>
    /// Builds the activity of a status at <see cref="StatusFactory.Now"/>.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="triviaIndex">Which trivia line to show.</param>
    /// <returns>The activity.</returns>
    private static Activity Build(GameStatus status, int triviaIndex = 0) =>
        ActivityBuilder.Build(status, triviaIndex, pocketCastleIndex: 0, Now);
}
