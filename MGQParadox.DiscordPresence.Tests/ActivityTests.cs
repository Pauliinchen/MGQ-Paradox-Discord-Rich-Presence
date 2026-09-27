//----------------------------------------------------------------
//  ActivityTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using MGQParadox.DiscordPresence.Discord;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers the JSON of an activity and the field limits Discord enforces on it.
/// </summary>
/// <remarks>
/// Discord rejects a whole activity over one field it does not accept, so the profile goes blank
/// rather than showing the rest.
/// </remarks>
public sealed class ActivityTests
{
    /// <summary>
    /// Asserts the JSON of an activity with only its first line.
    /// </summary>
    [Fact]
    public void ToJson_WritesTheFirstLine() =>
        Assert.Equal("{\"details\":\"Iliasville - Exploring . . .\"}", new Activity { Details = "Iliasville - Exploring . . ." }.ToJson());

    /// <summary>
    /// Asserts the JSON of an activity with every field.
    /// </summary>
    [Fact]
    public void ToJson_WritesEveryField()
    {
        var activity = new Activity
        {
            Details = "Iliasville - Exploring . . .",
            State = "Has fought 7 battles!",
            StartedAt = 1790499273,
            LargeImage = "default",
            LargeText = "Part 1: Alice side",
            Buttons = new[] { new ActivityButton("Get the mod", "https://example.com") },
        };

        Assert.Equal("{\"details\":\"Iliasville - Exploring . . .\",\"state\":\"Has fought 7 battles!\"," +
                     "\"timestamps\":{\"start\":1790499273},\"assets\":{\"large_image\":\"default\",\"large_text\":\"Part 1: Alice side\"}," +
                     "\"buttons\":[{\"label\":\"Get the mod\",\"url\":\"https://example.com\"}]}",
                     activity.ToJson());
    }

    /// <summary>
    /// Asserts that texts are escaped for JSON.
    /// </summary>
    [Fact]
    public void ToJson_EscapesTexts() =>
        Assert.Equal("{\"details\":\"Say \\\"hi\\\" \\\\ bye\"}", new Activity { Details = "Say \"hi\" \\ bye" }.ToJson());

    /// <summary>
    /// Asserts that texts shorter than Discord accepts are left out rather than sent.
    /// </summary>
    [Fact]
    public void ToJson_LeavesOutTooShortTexts()
    {
        var json = new Activity { Details = "Iliasville", State = "!", LargeImage = "default", LargeText = "?" }.ToJson();

        Assert.DoesNotContain("\"state\"", json);
        Assert.DoesNotContain("\"large_text\"", json);
    }

    /// <summary>
    /// Asserts that a tooltip without a picture is left out, since Discord only shows it on one.
    /// </summary>
    [Fact]
    public void ToJson_LeavesOutTheTooltipWithoutAPicture() =>
        Assert.DoesNotContain("large_text", new Activity { Details = "Iliasville", LargeText = "Part 1: Alice side" }.ToJson());

    /// <summary>
    /// Asserts that a text longer than Discord accepts is cut to fit, with an ellipsis.
    /// </summary>
    [Fact]
    public void ToJson_CutsLongTexts()
    {
        var json = new Activity { Details = new string('a', 200) }.ToJson();

        Assert.Equal($"{{\"details\":\"{new string('a', Activity.MaxTextLength - 1)}…\"}}", json);
    }

    /// <summary>
    /// Asserts that button labels are cut to 32 characters and at most two buttons are sent.
    /// </summary>
    [Fact]
    public void ToJson_KeepsButtonsInDiscordsLimits()
    {
        var activity = new Activity
        {
            Details = "Iliasville",
            Buttons = new[]
            {
                new ActivityButton(new string('b', 40), "https://example.com/1"),
                new ActivityButton("Second", "https://example.com/2"),
                new ActivityButton("Third", "https://example.com/3"),
            },
        };

        Assert.Equal($"{{\"details\":\"Iliasville\",\"buttons\":[{{\"label\":\"{new string('b', 31)}…\",\"url\":\"https://example.com/1\"}}," +
                     "{\"label\":\"Second\",\"url\":\"https://example.com/2\"}]}",
                     activity.ToJson());
    }
}
