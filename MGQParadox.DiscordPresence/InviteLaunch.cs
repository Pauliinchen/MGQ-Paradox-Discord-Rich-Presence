//----------------------------------------------------------------
//  InviteLaunch.cs
//
//  Changelog:
//      Paulinchen  2026-09-29: Masked join codes of every version, not only the first
//                            - Created
//
//----------------------------------------------------------------

using System;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Whether Discord started the game for an invite, and whether the invite's join came.
/// </summary>
/// <remarks>
/// Discord starts the game through the application's URL scheme for every invite a player clicks, but
/// hands over the join only while the host still offers it.
/// </remarks>
internal sealed class InviteLaunch
{
    /// <summary>
    /// How long after reaching Discord its join may still arrive. It comes within a second in practice.
    /// </summary>
    public static readonly TimeSpan JoinGrace = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long after the start Discord may take to be reached at all.
    /// </summary>
    public static readonly TimeSpan ConnectGrace = TimeSpan.FromSeconds(20);

    /// <summary>
    /// How a Multiplayer mod join code of any version starts, which lets a game into the host's
    /// session and stays out of the log.
    /// </summary>
    private const string JoinCodePrefix = "mgqmp";

    /// <summary>
    /// Guards every field below.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// The link Discord started the game with, <see langword="null"/> when it did not.
    /// </summary>
    private string? _link;

    /// <summary>
    /// When the game started.
    /// </summary>
    private DateTime _startedAt;

    /// <summary>
    /// When Discord was first reached, <see langword="null"/> until then.
    /// </summary>
    private DateTime? _connectedAt;

    /// <summary>
    /// Whether Discord handed over a join.
    /// </summary>
    private bool _joined;

    /// <summary>
    /// The game's launch, which the presence loop and the game script share.
    /// </summary>
    public static InviteLaunch Current { get; } = new();

    /// <summary>
    /// Finds the link Discord starts the game with in a command line.
    /// </summary>
    /// <param name="commandLine">The game's command line.</param>
    /// <param name="clientId">The Discord application, whose URL scheme the link uses.</param>
    /// <returns>The <c>discord-&lt;client id&gt;://</c> link up to the next quote or space, or <see langword="null"/> when there is none.</returns>
    public static string? DiscordLink(string? commandLine, string clientId)
    {
        var start = commandLine?.IndexOf($"discord-{clientId}://", StringComparison.OrdinalIgnoreCase) ?? -1;

        if (start < 0)
        {
            return null;
        }

        var end = commandLine!.IndexOfAny(['"', ' '], start);
        return end < 0 ? commandLine[start..] : commandLine[start..end];
    }

    /// <summary>
    /// Writes a link for the log, a join code in it masked.
    /// </summary>
    /// <param name="link">The link.</param>
    /// <returns>The link, cut before a join code.</returns>
    public static string Masked(string link)
    {
        var start = link.IndexOf(JoinCodePrefix, StringComparison.OrdinalIgnoreCase);
        return start < 0 ? link : link[..start] + "<join code>";
    }

    /// <summary>
    /// Notes whether Discord started the game.
    /// </summary>
    /// <param name="commandLine">The game's command line.</param>
    /// <param name="clientId">The Discord application.</param>
    /// <param name="now">The current time, the game's start.</param>
    /// <returns>The link Discord started the game with, or <see langword="null"/> when it did not.</returns>
    public string? Detect(string? commandLine, string clientId, DateTime now)
    {
        lock (_gate)
        {
            _link = DiscordLink(commandLine, clientId);
            _startedAt = now;
            return _link;
        }
    }

    /// <summary>
    /// Notes that Discord was reached, the first time only.
    /// </summary>
    /// <param name="now">The current time.</param>
    public void Connected(DateTime now)
    {
        lock (_gate)
        {
            _connectedAt ??= now;
        }
    }

    /// <summary>
    /// Notes that Discord handed over a join.
    /// </summary>
    public void Joined()
    {
        lock (_gate)
        {
            _joined = true;
        }
    }

    /// <summary>
    /// Tells where the launch stands.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns><see cref="InviteLaunchState.Ended"/> once no join came within <see cref="JoinGrace"/> of reaching Discord, or within <see cref="ConnectGrace"/> of the start while Discord was never reached.</returns>
    public InviteLaunchState State(DateTime now)
    {
        lock (_gate)
        {
            if (_link == null)
            {
                return InviteLaunchState.None;
            }

            if (_joined)
            {
                return InviteLaunchState.Joined;
            }

            var overdue = _connectedAt is { } connectedAt ? now - connectedAt >= JoinGrace : now - _startedAt >= ConnectGrace;
            return overdue ? InviteLaunchState.Ended : InviteLaunchState.Waiting;
        }
    }
}
