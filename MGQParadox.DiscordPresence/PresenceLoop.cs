//----------------------------------------------------------------
//  PresenceLoop.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Cleared the profile while the player turned the presence off
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
    /// Ticks each trivia line stays up, 16 seconds.
    /// </summary>
    private const int TicksPerTriviaLine = 4;

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
    private readonly DiscordIpcClient _discord;

    /// <summary>
    /// The picture the presence shows, if there is one.
    /// </summary>
    private readonly string? _largeImage;

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
    /// Picks both the trivia and the Pocket Castle line, moving on every <see cref="TicksPerTriviaLine"/> ticks.
    /// </summary>
    private int Rotation => _ticks / TicksPerTriviaLine;

    /// <summary>
    /// Creates the loop.
    /// </summary>
    /// <param name="clientId">The Discord application the presence is shown for.</param>
    private PresenceLoop(string clientId)
    {
        _discord = new DiscordIpcClient(clientId);
        _largeImage = PresenceImage.Resolve(clientId);
    }

    /// <summary>
    /// Starts the loop on a background thread, once per game session.
    /// </summary>
    public static void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        new Thread(Run) { IsBackground = true, Name = "DiscordPresence" }.Start();
    }

    /// <summary>
    /// Hands over the game's latest status.
    /// </summary>
    /// <param name="status">The <c>key=value</c> lines Discord_RPC.rb built.</param>
    /// <remarks>
    /// Called on the game's own thread, so it only stores the text. Everything else happens on the
    /// loop's thread.
    /// </remarks>
    public static void Submit(string status) => Volatile.Write(ref _latestStatus, status);

    /// <summary>
    /// Runs the loop until the game ends.
    /// </summary>
    /// <remarks>
    /// Catches everything. An exception escaping this thread would end the whole game.
    /// </remarks>
    private static void Run()
    {
        try
        {
            Log.ClearIfLargerThan(MaxLogBytes);
            Log.Write("--- presence started ---");

            if (Settings.ClientId is not { Length: > 0 } clientId)
            {
                Log.Write("No client_id set in Settings.ini - nothing to do.");
                return;
            }

            new PresenceLoop(clientId).Mirror();
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

        var activity = status.IsHidden
            ? NoActivity
            : ActivityBuilder.Build(status, _largeImage, Rotation, Rotation + _pocketCastleOffset).ToJson();

        if (activity == _lastSentActivity)
        {
            return;
        }

        try
        {
            _discord.SetActivity(_processId, activity);
            _lastSentActivity = activity;
            Log.Write("presence updated");
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
    /// <returns><see langword="true"/> while connected.</returns>
    /// <remarks>
    /// A new connection starts with an empty profile, so the last activity is sent again even if
    /// the game has not changed since.
    /// </remarks>
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
            return true;
        }

        if (!_reportedUnreachable)
        {
            Log.Write($"Discord not reachable - trying again every {ReconnectInterval.TotalSeconds:0} s");
            _reportedUnreachable = true;
        }

        return false;
    }
}
