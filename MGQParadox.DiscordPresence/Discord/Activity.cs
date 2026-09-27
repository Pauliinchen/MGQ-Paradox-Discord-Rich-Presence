//----------------------------------------------------------------
//  Activity.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Shared the longest text Discord accepts, so texts can shorten themselves first
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System.Text;

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// The rich presence shown on a Discord profile.
/// </summary>
internal sealed class Activity
{
    /// <summary>
    /// Longest text Discord accepts in a single field.
    /// </summary>
    public const int MaxTextLength = 128;

    /// <summary>
    /// Shortest text Discord accepts in a single field.
    /// </summary>
    /// <remarks>
    /// A shorter one does not just go missing, Discord rejects the whole activity over it.
    /// </remarks>
    private const int MinTextLength = 2;

    /// <summary>
    /// First line: where the player is and what they are doing.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// Second line: the current trivia.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Start of the game session in Unix seconds, shown as elapsed time.
    /// </summary>
    public long? StartedAt { get; set; }

    /// <summary>
    /// Asset key or https address of the large picture.
    /// </summary>
    public string? LargeImage { get; set; }

    /// <summary>
    /// Tooltip of the large picture.
    /// </summary>
    /// <remarks>
    /// Discord shows hover text only on a picture, so this is dropped without <see cref="LargeImage"/>.
    /// </remarks>
    public string LargeText { get; set; } = string.Empty;

    /// <summary>
    /// Writes the activity object of a SET_ACTIVITY command.
    /// </summary>
    /// <returns>The JSON object.</returns>
    /// <remarks>
    /// Contains nothing that changes between calls, so equal JSON means an equal activity.
    /// </remarks>
    public string ToJson()
    {
        var json = new StringBuilder("{\"details\":").Append(Json.Quote(Fit(Details)));

        if (State.Length >= MinTextLength)
        {
            json.Append(",\"state\":").Append(Json.Quote(Fit(State)));
        }

        if (StartedAt is { } startedAt)
        {
            json.Append(",\"timestamps\":{\"start\":").Append(startedAt).Append('}');
        }

        if (!string.IsNullOrEmpty(LargeImage))
        {
            json.Append(",\"assets\":{\"large_image\":").Append(Json.Quote(LargeImage));

            if (LargeText.Length >= MinTextLength)
            {
                json.Append(",\"large_text\":").Append(Json.Quote(Fit(LargeText)));
            }

            json.Append('}');
        }

        return json.Append('}').ToString();
    }

    /// <summary>
    /// Shortens a text to what fits into one field.
    /// </summary>
    /// <param name="text">The text to shorten.</param>
    /// <returns>The text, cut off with an ellipsis when it was too long.</returns>
    private static string Fit(string text) =>
        text.Length <= MaxTextLength ? text : text.Substring(0, MaxTextLength - 1) + "…";
}
