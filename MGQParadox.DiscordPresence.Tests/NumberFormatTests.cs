//----------------------------------------------------------------
//  NumberFormatTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers how the presence writes numbers.
/// </summary>
/// <remarks>
/// The DLL runs with invariant globalization, while tests may run under any culture, so these
/// also guard against a culture's own separators slipping in.
/// </remarks>
public sealed class NumberFormatTests
{
    /// <summary>
    /// Asserts that thousands are grouped with commas.
    /// </summary>
    /// <param name="number">The number.</param>
    /// <param name="expected">The grouped number.</param>
    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(4605, "4,605")]
    [InlineData(604761547, "604,761,547")]
    public void Grouped_GroupsThousands(long number, string expected) =>
        Assert.Equal(expected, NumberFormat.Grouped(number));

    /// <summary>
    /// Asserts that a count takes the singular only for exactly one.
    /// </summary>
    [Fact]
    public void Counted_PicksSingularOrPlural()
    {
        Assert.Equal("1 battle", NumberFormat.Counted(1, "battle"));
        Assert.Equal("0 battles", NumberFormat.Counted(0, "battle"));
        Assert.Equal("7,073 enemies", NumberFormat.Counted(7073, "enemy", "enemies"));
    }

    /// <summary>
    /// Asserts the ordinals, the teens included.
    /// </summary>
    /// <param name="number">The count.</param>
    /// <param name="expected">The ordinal.</param>
    [Theory]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(111, "111th")]
    [InlineData(1021, "1,021st")]
    public void Ordinal_WritesTheSuffix(long number, string expected) =>
        Assert.Equal(expected, NumberFormat.Ordinal(number));
}
