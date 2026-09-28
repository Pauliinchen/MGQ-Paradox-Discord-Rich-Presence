//----------------------------------------------------------------
//  Activity.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Added a party, a join secret and the banner of an invite, which leave the buttons out
//      Paulinchen  2026-09-27: Added link buttons
//                            - Shared the longest text Discord accepts, so texts can shorten themselves first
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Most buttons Discord shows under an activity.
    /// </summary>
    private const int MaxButtons = 2;

    /// <summary>
    /// Longest button label Discord accepts.
    /// </summary>
    private const int MaxButtonLabelLength = 32;

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
    /// Link buttons under the activity, of which Discord shows the first <see cref="MaxButtons"/>.
    /// </summary>
    public IReadOnlyList<ActivityButton> Buttons { get; set; } = Array.Empty<ActivityButton>();

    /// <summary>
    /// The party the player invites to, <see langword="null"/> while they invite nobody.
    /// </summary>
    public ActivityParty? Party { get; set; }

    /// <summary>
    /// What Discord hands the game of a friend who joins, <see langword="null"/> while nobody can.
    /// </summary>
    /// <remarks>
    /// The buttons are left out while it is set, since whether Discord accepts both is untested.
    /// </remarks>
    public string? JoinSecret { get; set; }

    /// <summary>
    /// The art asset shown as the banner of an invite to the party, <see langword="null"/> for the
    /// application's invite image.
    /// </summary>
    public string? InviteCover { get; set; }

    /// <summary>
    /// Writes the activity object of a SET_ACTIVITY command.
    /// </summary>
    /// <remarks>
    /// Contains nothing that changes between calls, so equal JSON means an equal activity.
    /// </remarks>
    /// <returns>The JSON object.</returns>
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

        var assets = new List<string>();

        if (!string.IsNullOrEmpty(LargeImage))
        {
            assets.Add("\"large_image\":" + Json.Quote(LargeImage));

            if (LargeText.Length >= MinTextLength)
            {
                assets.Add("\"large_text\":" + Json.Quote(Fit(LargeText)));
            }
        }

        if (!string.IsNullOrEmpty(InviteCover))
        {
            assets.Add("\"invite_cover_image\":" + Json.Quote(InviteCover));
        }

        if (assets.Count > 0)
        {
            json.Append(",\"assets\":{").Append(string.Join(",", assets)).Append('}');
        }

        if (Party != null)
        {
            json.Append(",\"party\":{\"id\":").Append(Json.Quote(Party.Id))
                .Append(",\"size\":[").Append(Party.Size).Append(',').Append(Party.Max).Append("]}");
        }

        if (JoinSecret != null)
        {
            json.Append(",\"secrets\":{\"join\":").Append(Json.Quote(JoinSecret)).Append('}');
        }
        else if (Buttons.Count > 0)
        {
            var buttons = Buttons.Take(MaxButtons).Select(button =>
                $"{{\"label\":{Json.Quote(Fit(button.Label, MaxButtonLabelLength))},\"url\":{Json.Quote(button.Url)}}}");

            json.Append(",\"buttons\":[").Append(string.Join(",", buttons)).Append(']');
        }

        return json.Append('}').ToString();
    }

    /// <summary>
    /// Shortens a text to what fits into one field.
    /// </summary>
    /// <param name="text">The text to shorten.</param>
    /// <param name="maxLength">The longest text the field accepts.</param>
    /// <returns>The text, cut off with an ellipsis when it was too long.</returns>
    private static string Fit(string text, int maxLength = MaxTextLength) =>
        text.Length <= maxLength ? text : text.Substring(0, maxLength - 1) + "…";
}
