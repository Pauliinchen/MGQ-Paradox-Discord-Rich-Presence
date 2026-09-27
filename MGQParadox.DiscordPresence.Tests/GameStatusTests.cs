//----------------------------------------------------------------
//  GameStatusTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using MGQParadox.DiscordPresence.Game;
using static MGQParadox.DiscordPresence.Tests.StatusFactory;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers reading the status text Discord_RPC.rb publishes.
/// </summary>
public sealed class GameStatusTests
{
    /// <summary>
    /// Asserts that a text without a single value is no status at all.
    /// </summary>
    /// <param name="text">The text.</param>
    [Theory]
    [InlineData("")]
    [InlineData("no value here")]
    [InlineData("=value without a key")]
    public void Parse_ReturnsNothingWithoutValues(string text) =>
        Assert.Null(GameStatus.Parse(text));

    /// <summary>
    /// Asserts that keys and values are trimmed, keys match without regard to case, and a value may
    /// hold an equals sign.
    /// </summary>
    [Fact]
    public void Parse_ReadsKeysAndValues()
    {
        var status = GameStatus.Parse(" AREA = Iliasville \r\ntalking_to=a=b\n")!;

        Assert.Equal("Iliasville", status.Area);
        Assert.Equal("a=b", status.ConversationPartner);
    }

    /// <summary>
    /// Asserts that an unknown scene reads as the map.
    /// </summary>
    [Fact]
    public void Scene_FallsBackToTheMap() =>
        Assert.Equal(Scene.Map, Status(("scene", "casino")).Scene);

    /// <summary>
    /// Asserts that numbers may be negative, and an absent or invalid one reads as unknown.
    /// </summary>
    [Fact]
    public void Numbers_ReadNegativeAndUnknownValues()
    {
        Assert.Equal(-2, Status(("difficulty", -2)).Difficulty);
        Assert.Null(Status(("difficulty", "hard")).Difficulty);
        Assert.Null(Status().Difficulty);
        Assert.Equal(604_761_547_000, Status(("gold", 604_761_547_000)).Gold);
    }

    /// <summary>
    /// Asserts that a ranked list ends at its first gap.
    /// </summary>
    [Fact]
    public void RankedLists_EndAtTheFirstGap()
    {
        var status = Status(("affection_name0", "Alice"), ("affection_love0", 2093),
                            ("affection_name2", "Melk"), ("affection_love2", 689));

        var entry = Assert.Single(status.MostAffection);
        Assert.Equal(("Alice", 2093L), entry);
    }

    /// <summary>
    /// Asserts that a stat without a leader reads as unknown.
    /// </summary>
    [Fact]
    public void TopStat_IsUnknownWithoutALeader()
    {
        var status = Status(("stat_name0", "Max HP"), ("stat_holder0", "Luka"), ("stat_value0", "120"));

        Assert.Equal(("Max HP", "Luka", "120"), status.TopStat(0));
        Assert.Null(status.TopStat(1));
    }

    /// <summary>
    /// Asserts that a count out of a total reads both, 0 while absent.
    /// </summary>
    [Fact]
    public void OutOf_ReadsTheCountAndTheTotal()
    {
        Assert.Equal((23L, 80L), Status(("randolphs", 23), ("randolphs_total", 80)).RandolphsFound);
        Assert.Equal((0L, 0L), Status().RandolphsFound);
    }
}
