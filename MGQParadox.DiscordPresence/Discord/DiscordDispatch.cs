//----------------------------------------------------------------
//  DiscordDispatch.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

using System.Text.Json;

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// An event Discord sends on its own: READY after the handshake, and the invite events the client
/// subscribed to.
/// </summary>
/// <param name="Event">The event's name, such as <see cref="Ready"/>.</param>
/// <param name="Secret">The join secret of <see cref="ActivityJoin"/>.</param>
/// <param name="UserId">The user of <see cref="Ready"/> or <see cref="ActivityJoinRequest"/>.</param>
/// <param name="UserName">That user's display name, or their user name when they set none.</param>
internal sealed record DiscordDispatch(string Event, string? Secret, string? UserId, string? UserName)
{
    /// <summary>
    /// The handshake succeeded, naming the user Discord is logged in as.
    /// </summary>
    public const string Ready = "READY";

    /// <summary>
    /// The player accepted an invite in Discord, which hands over its join secret.
    /// </summary>
    public const string ActivityJoin = "ACTIVITY_JOIN";

    /// <summary>
    /// A friend asks to join the party the player invites to.
    /// </summary>
    public const string ActivityJoinRequest = "ACTIVITY_JOIN_REQUEST";

    /// <summary>
    /// The command every event arrives as.
    /// </summary>
    private const string DispatchCommand = "DISPATCH";

    /// <summary>
    /// Reads an event from a frame's body.
    /// </summary>
    /// <param name="body">The body.</param>
    /// <returns>The event, or <see langword="null"/> when the body is an answer to a command or no JSON at all.</returns>
    public static DiscordDispatch? TryParse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            if (Text(root, "cmd") != DispatchCommand || Text(root, "evt") is not { } name)
            {
                return null;
            }

            var data = root.TryGetProperty("data", out var value) && value.ValueKind == JsonValueKind.Object ? value : default;
            var user = data.ValueKind == JsonValueKind.Object && data.TryGetProperty("user", out var found) && found.ValueKind == JsonValueKind.Object
                ? found
                : default;

            return new DiscordDispatch(
                name,
                Text(data, "secret"),
                Text(user, "id"),
                Text(user, "global_name") ?? Text(user, "username"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a string property.
    /// </summary>
    /// <param name="element">The object, or a default element.</param>
    /// <param name="name">The property.</param>
    /// <returns>The text, or <see langword="null"/> when absent, empty or no string.</returns>
    private static string? Text(JsonElement element, string name) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(name, out var value) &&
        value.ValueKind == JsonValueKind.String &&
        value.GetString() is { Length: > 0 } text
            ? text
            : null;
}
