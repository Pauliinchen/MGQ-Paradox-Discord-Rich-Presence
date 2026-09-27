//----------------------------------------------------------------
//  TriviaBuilderTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Covered the medals earned
//                            - Created
//
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using MGQParadox.DiscordPresence.Game;
using static MGQParadox.DiscordPresence.Tests.StatusFactory;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers the trivia lines, the second Discord line.
/// </summary>
/// <remarks>
/// The expected texts are the ones the game script wrote before the DLL took the trivia over, most
/// of them copied from DiscordPresence.log, so a changed word shows up here first.
/// </remarks>
public sealed class TriviaBuilderTests
{
    /// <summary>
    /// A point in time that starts both rotations: the top master's (every 3rd step) and the top
    /// stat's (every 8th step), each step 45 seconds.
    /// </summary>
    private static readonly DateTimeOffset RotationStart = DateTimeOffset.FromUnixTimeSeconds(1_755_000_000);

    /// <summary>
    /// Asserts that every line reads like before the move into the DLL.
    /// </summary>
    /// <param name="key">The value the game script published.</param>
    /// <param name="value">The value.</param>
    /// <param name="expected">The line.</param>
    [Theory]
    [InlineData("dead_members", "2", "Currently has 2 dead party members!")]
    [InlineData("dead_members", "1", "Currently has 1 dead party member!")]
    [InlineData("companions", "496", "Has recruited 496 party members this playthrough!")]
    [InlineData("battles", "4605", "Has fought 4,605 battles!")]
    [InlineData("difficulty", "2", "Currently playing on Very Hard - Pain is a choice, and they chose it!")]
    [InlineData("difficulty", "4", "Currently playing on Paradox - Has lost all sense of self-preservation!")]
    [InlineData("difficulty", "-2", "Currently playing on Very Easy - Here for the story, and that's fine!")]
    [InlineData("playtime_hours", "140", "Is 140 hours in. The job and race grind has begun in earnest!")]
    [InlineData("playtime_hours", "3", "Is 3 hours in. Still an Apprentice Hero fresh out of Iliasville!")]
    [InlineData("playtime_hours", "1", "Is 1 hour in. Still an Apprentice Hero fresh out of Iliasville!")]
    [InlineData("playtime_hours", "0", "Is less than an hour in. Still an Apprentice Hero fresh out of Iliasville!")]
    [InlineData("playtime_hours", "54", "Is 54 hours in. The Pocket Castle is starting to feel like home!")]
    [InlineData("playtime_hours", "1000", "Is 1,000 hours in. Has become the true Paradox!")]
    [InlineData("track", "Field 1", "Currently vibing to Field 1!")]
    [InlineData("defeated", "313", "Has defeated 313 enemies!")]
    [InlineData("defeated", "1", "Has defeated 1 enemy!")]
    [InlineData("escaped", "16", "Has run away from 16 battles!")]
    [InlineData("wipeouts", "1", "Has been wiped out 1 time!")]
    [InlineData("wipeouts", "48", "Has been wiped out 48 times!")]
    [InlineData("biggest_hit", "892", "Biggest hit dealt: 892 damage!")]
    [InlineData("biggest_hit", "118.497Qnt.", "Biggest hit dealt: 118.497Qnt. damage!")]
    [InlineData("gold_spent", "4050", "Has spent 4,050 gold in shops!")]
    [InlineData("synthesized", "14", "Has synthesized 14 items!")]
    [InlineData("labyrinth_record", "301", "Has reached floor 301 in the Labyrinth of Chaos!")]
    [InlineData("requests", "3", "Has made 3 requests!")]
    [InlineData("rapes", "1", "Has been raped 1 time!")]
    [InlineData("battlefucks_won", "57", "Has won 57 battlefucks!")]
    [InlineData("battlefucks_won", "1", "Has won 1 battlefuck!")]
    [InlineData("gold", "6994933", "Currently carrying 6,994,933 gold!")]
    [InlineData("gold", "0", "Currently carrying 0 gold!")]
    public void Line_ReadsLikeBefore(string key, string value, string expected) =>
        Assert.Contains(expected, LinesOf(Status((key, value), ("nsfw", 1))));

    /// <summary>
    /// Asserts that counts which have not started yet leave their line out.
    /// </summary>
    /// <param name="key">The count the game script published as 0.</param>
    /// <param name="start">How the line would start.</param>
    [Theory]
    [InlineData("dead_members", "Currently has")]
    [InlineData("defeated", "Has defeated")]
    [InlineData("escaped", "Has run away")]
    [InlineData("wipeouts", "Has been wiped out")]
    [InlineData("gold_spent", "Has spent")]
    [InlineData("synthesized", "Has synthesized")]
    [InlineData("labyrinth_record", "Has reached floor")]
    [InlineData("requests", "Has made")]
    [InlineData("rapes", "Has been raped")]
    [InlineData("battlefucks_won", "Has won")]
    public void Line_IsLeftOutAtZero(string key, string start) =>
        Assert.DoesNotContain(LinesOf(Status((key, 0), ("nsfw", 1))), line => line.StartsWith(start, StringComparison.Ordinal));

    /// <summary>
    /// Asserts that the counts which always show wait for a loaded save, which publishes them.
    /// </summary>
    [Fact]
    public void Lines_AreEmptyBeforeASaveIsLoaded() =>
        Assert.Empty(LinesOf(Status()));

    /// <summary>
    /// Asserts that an unknown difficulty value writes no line rather than a wrong one.
    /// </summary>
    /// <param name="difficulty">A value the difficulty table does not know.</param>
    [Theory]
    [InlineData("9")]
    [InlineData("hard")]
    public void Difficulty_IsLeftOutWhenUnknown(string difficulty) =>
        Assert.DoesNotContain(LinesOf(Status(("difficulty", difficulty))), line => line.StartsWith("Currently playing", StringComparison.Ordinal));

    /// <summary>
    /// Asserts that the companions with the most affection are listed like a sentence.
    /// </summary>
    [Fact]
    public void MostAffection_ListsUpToThreeCompanions()
    {
        var three = Status(("affection_name0", "Alice"), ("affection_love0", 2093),
                           ("affection_name1", "Lucretia"), ("affection_love1", 1612),
                           ("affection_name2", "La Croix"), ("affection_love2", 1611));
        var two = Status(("affection_name0", "Alice"), ("affection_love0", 2093),
                         ("affection_name1", "Melk"), ("affection_love1", 689));
        var one = Status(("affection_name0", "Alice"), ("affection_love0", 2093));

        Assert.Contains("Most affection with Alice (2,093), Lucretia (1,612) and La Croix (1,611)!", LinesOf(three));
        Assert.Contains("Most affection with Alice (2,093) and Melk (689)!", LinesOf(two));
        Assert.Contains("Most affection with Alice (2,093)!", LinesOf(one));
    }

    /// <summary>
    /// Asserts that the medals earned show out of all medals, from the first one.
    /// </summary>
    [Fact]
    public void MedalsEarned_CountOutOfAllMedals()
    {
        Assert.Contains("Has earned 120 out of 394 medals!", LinesOf(Status(("medals", 120), ("medals_total", 394))));
        Assert.DoesNotContain(LinesOf(Status(("medals", 0), ("medals_total", 394))), line => line.Contains("medals", StringComparison.Ordinal));
    }

    /// <summary>
    /// Asserts that a used item shows for 90 seconds and then leaves.
    /// </summary>
    /// <param name="secondsAgo">How long ago the item was used.</param>
    /// <param name="shown">Whether the line shows.</param>
    [Theory]
    [InlineData(0, true)]
    [InlineData(90, true)]
    [InlineData(91, false)]
    public void LastItemUsed_ShowsForNinetySeconds(int secondsAgo, bool shown)
    {
        var status = Status(("item_used", "Medicinal Herb"), ("item_target", "Luka"),
                            ("item_used_at", Unix(Now) - secondsAgo));

        Assert.Equal(shown, LinesOf(status).Contains("Just used Medicinal Herb on Luka!"));
    }

    /// <summary>
    /// Asserts that the top masters take turns every 45 seconds.
    /// </summary>
    [Fact]
    public void TopMaster_TakesTurns()
    {
        var status = Status(("master_name0", "Shadou"), ("master_jobs0", 135), ("master_races0", 46),
                            ("master_name1", "Bloody"), ("master_jobs1", 84), ("master_races1", 33),
                            ("master_name2", "Luka"), ("master_jobs2", 70), ("master_races2", 25));

        Assert.Contains("Shadou has mastered 135 Jobs and 46 Races already!", LinesOf(status, RotationStart));
        Assert.Contains("Bloody has mastered 84 Jobs and 33 Races already!", LinesOf(status, RotationStart.AddSeconds(45)));
        Assert.Contains("Luka has mastered 70 Jobs and 25 Races already!", LinesOf(status, RotationStart.AddSeconds(90)));
        Assert.Contains("Shadou has mastered 135 Jobs and 46 Races already!", LinesOf(status, RotationStart.AddSeconds(135)));
    }

    /// <summary>
    /// Asserts that the stats take turns every 45 seconds, and a stat without a leader writes no line.
    /// </summary>
    [Fact]
    public void TopStat_TakesTurnsThroughTheStats()
    {
        var status = Status(("stat_name0", "Max HP"), ("stat_holder0", "Chaos"), ("stat_value0", "985,111"),
                            ("stat_name2", "Magic"), ("stat_holder2", "Chaos"), ("stat_value2", "6.298Bil."));

        Assert.Contains("Chaos has the highest Max HP (985,111) in the party!", LinesOf(status, RotationStart));
        Assert.DoesNotContain(LinesOf(status, RotationStart.AddSeconds(45)), line => line.Contains("has the highest", StringComparison.Ordinal));
        Assert.Contains("Chaos has the highest Magic (6.298Bil.) in the party!", LinesOf(status, RotationStart.AddSeconds(90)));
    }

    /// <summary>
    /// Asserts that requests, defeat scenes and battle fucks only show with the NSFW option on.
    /// </summary>
    [Fact]
    public void NsfwLines_NeedTheNsfwOption()
    {
        (string, object)[] values =
        {
            ("requests", 3), ("most_requested", "Alice"), ("most_requested_count", 2),
            ("rapes", 1), ("most_raped_by", "Rabbit Girl"), ("most_raped_count", 1), ("battlefucks_won", 57),
        };
        var expected = new[]
        {
            "Has made 3 requests!",
            "Has requested Alice the most, 2 times!",
            "Has been raped 1 time!",
            "Raped by Rabbit Girl the most, 1 time!",
            "Has won 57 battlefucks!",
        };

        Assert.Empty(LinesOf(Status(values)));
        Assert.Equal(expected, LinesOf(Status(values.Append(("nsfw", 1)).ToArray())));
    }

    /// <summary>
    /// Asserts that the lines of a part only rotate while it is played.
    /// </summary>
    /// <param name="part">The part of the story.</param>
    /// <param name="expected">The part lines expected.</param>
    [Theory]
    [MemberData(nameof(PartLines))]
    public void PartLines_ShowInTheirPart(int part, string[] expected)
    {
        var status = Status(("part", part), ("spirits", 2), ("spirits_total", 4), ("ore", "Mithril Ore"),
                            ("naval_side", "pirates"), ("queens", 5), ("queens_total", 14),
                            ("routes_cleared", 1), ("routes_total", 3), ("randolphs", 23), ("randolphs_total", 80));

        Assert.Equal(expected, LinesOf(status));
    }

    /// <summary>
    /// Asserts that the Phenomena of Ruin only show on the Chaos route.
    /// </summary>
    /// <param name="route">The route of the final chapter.</param>
    /// <param name="shown">Whether the line shows.</param>
    [Theory]
    [InlineData("chaos", true)]
    [InlineData("judgment", false)]
    [InlineData("destroyer", false)]
    [InlineData("", false)]
    public void PhenomenaOfRuin_ShowOnTheChaosRoute(string route, bool shown)
    {
        var status = Status(("part", 3), ("route", route), ("phenomena", 5), ("phenomena_total", 16));

        Assert.Equal(shown, LinesOf(status).Contains("5 out of 16 Phenomena of Ruin have been defeated!"));
    }

    /// <summary>
    /// Asserts that hidden spoilers take the spoiler lines out and leave the others.
    /// </summary>
    [Fact]
    public void Spoilers_LeaveOutTheSpoilerLines()
    {
        (string, object)[] values =
        {
            ("part", 3), ("route", "chaos"), ("routes_cleared", 1), ("routes_total", 3),
            ("randolphs", 23), ("randolphs_total", 80), ("phenomena", 5), ("phenomena_total", 16),
        };

        Assert.Equal(new[] { "Has cleared 1 out of 3 routes!" }, LinesOf(Status(values.Append(("hide_spoilers", 1)).ToArray())));
        Assert.Equal(3, LinesOf(Status(values)).Count);
    }

    /// <summary>
    /// Asserts that the naval side names who Luka sided with, and nothing for an unknown side.
    /// </summary>
    /// <param name="side">The side the game script published.</param>
    /// <param name="expected">The line, or <see langword="null"/> for none.</param>
    [Theory]
    [InlineData("pirates", "Sided with the Pirates this playthrough!")]
    [InlineData("marines", "Sided with the Marines this playthrough!")]
    [InlineData("", null)]
    public void NavalSide_NamesTheSide(string side, string? expected) =>
        Assert.Equal(expected, LinesOf(Status(("part", 2), ("naval_side", side))).SingleOrDefault());

    /// <summary>
    /// Asserts that the general lines rotate before the lines of the part.
    /// </summary>
    [Fact]
    public void Lines_RotateGeneralLinesFirst()
    {
        var lines = LinesOf(Status(("part", 1), ("spirits", 1), ("spirits_total", 4), ("gold", 100), ("battles", 7)));

        Assert.Equal(new[] { "Has fought 7 battles!", "Currently carrying 100 gold!", "Has recruited 1 out of 4 spirits!" }, lines);
    }

    /// <summary>
    /// The part lines expected in each part, for <see cref="PartLines_ShowInTheirPart"/>.
    /// </summary>
    public static TheoryData<int, string[]> PartLines => new()
    {
        { 0, Array.Empty<string>() },
        { 1, new[] { "Has recruited 2 out of 4 spirits!", "Has unlocked Mithril Ore for forging!" } },
        {
            2, new[]
            {
                "Has recruited 2 out of 4 spirits!", "Has unlocked Mithril Ore for forging!",
                "Sided with the Pirates this playthrough!", "Has recruited 5 out of 14 monster queens!",
            }
        },
        { 3, new[] { "Has cleared 1 out of 3 routes!", "Has found 23 out of 80 Randolphs!" } },
    };

    /// <summary>
    /// Writes the lines of a status at <see cref="StatusFactory.Now"/>.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <returns>The lines.</returns>
    private static IReadOnlyList<string> LinesOf(GameStatus status) => TriviaBuilder.LinesOf(status, Now);

    /// <summary>
    /// Writes the lines of a status at a given time.
    /// </summary>
    /// <param name="status">The status.</param>
    /// <param name="now">The time.</param>
    /// <returns>The lines.</returns>
    private static IReadOnlyList<string> LinesOf(GameStatus status, DateTimeOffset now) => TriviaBuilder.LinesOf(status, now);
}
