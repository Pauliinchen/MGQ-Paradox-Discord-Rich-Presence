//----------------------------------------------------------------
//  PresenceLoop.cs
//
//  Changelog:
//      Paulinchen  2026-09-29: Noted the launch before the loop's thread starts, so the title screen never asks too early
//                            - Noted whether Discord started the game, with the link it used, and whether its join came
//                            - Stopped letting friends who ask to join in, which moved a hosting friend into the player's party
//      Paulinchen  2026-09-28: Showed the connection another mod reports on the second line, in a party both games share, with an invite banner
//                            - Kept Discord's invites for the mod that joins with them, and let friends who ask in while it hosts
//                            - Logged whether an update is open to invites
//                            - Registered how Discord starts the game
//      Paulinchen  2026-09-27: Handed the current time to the activity
//                            - Fixed the Discord application instead of reading it from Settings.ini
//                            - Stopped looking up the app icon, the default art asset stands in for it
//                            - Cleared the profile while the player turned the presence off
//      Paulinchen  2026-09-26: Sent the status every 4 seconds and rotated the trivia every 4th update
//                            - Named the game script by its new file name
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Threading;
using MGQParadox.DiscordPresence.Discord;
using MGQParadox.DiscordPresence.Game;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Mirrors the game onto the Discord profile, on a thread of its own inside the game.
/// </summary>
internal sealed class PresenceLoop
{
    /// <summary>
    /// Size at which the log starts over.
    /// </summary>
    private const long MaxLogBytes = 200_000;

    /// <summary>
    /// The Discord application that shows as "Playing Monster Girl Quest! Paradox RPG" and holds the
    /// art assets the pictures name.
    /// </summary>
    private const string ClientId = "1553096377959981117";

    /// <summary>
    /// Ticks each trivia line stays up, 16 seconds.
    /// </summary>
    private const int TicksPerTriviaLine = 4;

    /// <summary>
    /// Players a connection takes, the host and one guest.
    /// </summary>
    private const int MaxPlayers = 2;

    /// <summary>
    /// The art asset shown as the banner of an invite to play together, 1024 x 576.
    /// </summary>
    /// <remarks>
    /// Set in the activity, since the application's invite image in the Developer Portal did not reach the invites.
    /// </remarks>
    private const string InviteCoverAsset = "invite_cover";

    /// <summary>
    /// The activity that clears the profile, sent while the player turned the presence off.
    /// </summary>
    private const string NoActivity = "null";

    /// <summary>
    /// Gap between two ticks, each of which sends the latest status if it changed.
    /// </summary>
    /// <remarks>
    /// Discord accepts 5 updates per 20 seconds, so ticks never come closer than this.
    /// </remarks>
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How long to wait before trying to reach Discord again.
    /// </summary>
    private static readonly TimeSpan ReconnectInterval = TimeSpan.FromSeconds(15);

    /// <summary>
    /// 1 once the loop has been started, so a second start does nothing.
    /// </summary>
    private static int _started;

    /// <summary>
    /// The status the game handed over last, not yet parsed.
    /// </summary>
    private static string? _latestStatus;

    /// <summary>
    /// The connection to Discord.
    /// </summary>
    private readonly DiscordIpcClient _discord = new(ClientId);

    /// <summary>
    /// The game process, which the presence belongs to.
    /// </summary>
    private readonly int _processId = Environment.ProcessId;

    /// <summary>
    /// Where the Pocket Castle lines start, so not every session opens with the same one.
    /// </summary>
    private readonly int _pocketCastleOffset = Random.Shared.Next(ActivityBuilder.PocketCastleLineCount);

    /// <summary>
    /// The status text <see cref="_status"/> was parsed from.
    /// </summary>
    private string? _parsedStatus;

    /// <summary>
    /// The latest status, parsed.
    /// </summary>
    private GameStatus? _status;

    /// <summary>
    /// The activity Discord last accepted, as JSON.
    /// </summary>
    private string? _lastSentActivity;

    /// <summary>
    /// When the last attempt to reach Discord was made.
    /// </summary>
    private DateTime _lastConnectAttemptAt = DateTime.MinValue;

    /// <summary>
    /// Whether the log already says that Discord cannot be reached.
    /// </summary>
    private bool _reportedUnreachable;

    /// <summary>
    /// Ticks since the loop started.
    /// </summary>
    private int _ticks;

    /// <summary>
    /// Creates the loop, taking Discord's invite events.
    /// </summary>
    private PresenceLoop()
    {
        _discord.Dispatched = Take;
    }

    /// <summary>
    /// Picks both the trivia and the Pocket Castle line, moving on every <see cref="TicksPerTriviaLine"/> ticks.
    /// </summary>
    private int Rotation => _ticks / TicksPerTriviaLine;

    /// <summary>
    /// Notes whether Discord started the game, then starts the loop on a background thread, once per
    /// game session.
    /// </summary>
    /// <remarks>
    /// The launch is noted before the thread starts, since the title screen asks for it right away.
    /// </remarks>
    public static void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        var link = InviteLaunch.Current.Detect(NativeMethods.CommandLine(), ClientId, DateTime.UtcNow);
        new Thread(() => Run(link)) { IsBackground = true, Name = "DiscordPresence" }.Start();
    }

    /// <summary>
    /// Hands over the game's latest status.
    /// </summary>
    /// <remarks>
    /// Called on the game's own thread, so it only stores the text for the loop's thread.
    /// </remarks>
    /// <param name="status">The <c>key=value</c> lines Discord_RPC.rb built.</param>
    public static void Submit(string status) => Volatile.Write(ref _latestStatus, status);

    /// <summary>
    /// Runs the loop until the game ends.
    /// </summary>
    /// <remarks>
    /// Catches everything, since an exception escaping this thread would end the whole game.
    /// </remarks>
    /// <param name="link">The link Discord started the game with, or <see langword="null"/> when it did not.</param>
    private static void Run(string? link)
    {
        try
        {
            Log.ClearIfLargerThan(MaxLogBytes);
            Log.Write("--- presence started ---");
            LaunchRegistration.Register(ClientId);

            if (link != null)
            {
                Log.Write($"started by Discord with {InviteLaunch.Masked(link)}, waiting for its join");
            }

            new PresenceLoop().Mirror();
        }
        catch (Exception ex)
        {
            Log.Write($"fatal: {ex}");
        }
    }

    /// <summary>
    /// Mirrors the game onto the profile for as long as the game runs.
    /// </summary>
    private void Mirror()
    {
        while (true)
        {
            if (LatestStatus() is { } status)
            {
                Update(status);
            }

            _ticks++;
            Thread.Sleep(TickInterval);
        }
    }

    /// <summary>
    /// Parses the latest status, if it changed since the last look.
    /// </summary>
    /// <returns>The latest status, or <see langword="null"/> while the game has handed over none.</returns>
    private GameStatus? LatestStatus()
    {
        var text = Volatile.Read(ref _latestStatus);

        if (text != _parsedStatus)
        {
            _parsedStatus = text;
            _status = text == null ? null : GameStatus.Parse(text);
        }

        return _status;
    }

    /// <summary>
    /// Sends the activity for a status, if it changed since the last send.
    /// </summary>
    /// <param name="status">The status the game handed over.</param>
    private void Update(GameStatus status)
    {
        if (!EnsureConnected())
        {
            return;
        }

        var built = status.IsHidden
            ? null
            : WithConnection(ActivityBuilder.Build(status, Rotation, Rotation + _pocketCastleOffset, DateTimeOffset.UtcNow), Connection.Current);
        var activity = built?.ToJson() ?? NoActivity;

        if (activity == _lastSentActivity)
        {
            return;
        }

        try
        {
            _discord.SetActivity(_processId, activity);
            _lastSentActivity = activity;
            Log.Write(built?.JoinSecret != null ? "presence updated, open to invites" : "presence updated");
        }
        catch (Exception ex)
        {
            Log.Write($"send failed: {ex.Message}");
            _discord.Disconnect();
        }
    }

    /// <summary>
    /// Makes sure Discord is connected, trying again at most every <see cref="ReconnectInterval"/>.
    /// </summary>
    /// <remarks>
    /// A new connection starts with an empty profile, so the last activity is sent again even if
    /// the game has not changed since.
    /// </remarks>
    /// <returns><see langword="true"/> while connected.</returns>
    private bool EnsureConnected()
    {
        if (_discord.IsConnected)
        {
            return true;
        }

        _lastSentActivity = null;

        if (DateTime.UtcNow - _lastConnectAttemptAt < ReconnectInterval)
        {
            return false;
        }

        _lastConnectAttemptAt = DateTime.UtcNow;

        if (_discord.Connect())
        {
            _reportedUnreachable = false;
            Connection.Current.PlayerName = _discord.UserName;
            InviteLaunch.Current.Connected(DateTime.UtcNow);
            return true;
        }

        if (!_reportedUnreachable)
        {
            Log.Write($"Discord not reachable - trying again every {ReconnectInterval.TotalSeconds:0} s");
            _reportedUnreachable = true;
        }

        return false;
    }

    /// <summary>
    /// Shows the connection with a friend instead of the trivia: waiting for a friend with an invite
    /// while hosting, playing with them while connected, each with the party Discord shows the size of.
    /// </summary>
    /// <param name="activity">The activity the status describes.</param>
    /// <param name="connection">The connection another mod reported.</param>
    /// <returns>The same activity, with the connection when there is one.</returns>
    internal static Activity WithConnection(Activity activity, Connection connection)
    {
        if (connection.Hosting is { } hosted)
        {
            activity.Party = new ActivityParty(hosted.PartyId, 1, MaxPlayers);
            activity.JoinSecret = hosted.JoinSecret;
            activity.InviteCover = InviteCoverAsset;
            activity.State = ActivityBuilder.WaitingForFriendState;
        }
        else if (connection.Connected is { } connected)
        {
            activity.Party = new ActivityParty(connected.PartyId, MaxPlayers, MaxPlayers);
            activity.State = ActivityBuilder.PlayingWith(connected.Friend);
        }

        return activity;
    }

    /// <summary>
    /// Handles an invite event, on the thread that reads Discord's pipe.
    /// </summary>
    /// <param name="dispatch">The event.</param>
    private void Take(DiscordDispatch dispatch)
    {
        switch (dispatch.Event)
        {
            case DiscordDispatch.ActivityJoin when dispatch.Secret is { } secret:
                InviteLaunch.Current.Joined();
                Log.Write(Connection.Current.ReceiveInvite(secret) ? "invite accepted, waiting for the game to join" : "ignored an invite without a usable join secret");
                break;
            // Accepting on the player's behalf moved a friend who hosted too into the player's party,
            // which ended the friend's hosting, so the player invites them instead.
            case DiscordDispatch.ActivityJoinRequest:
                Log.Write($"{dispatch.UserName} asked to join, left to the player to invite");
                break;
        }
    }
}
