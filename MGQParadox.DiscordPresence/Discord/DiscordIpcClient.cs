//----------------------------------------------------------------
//  DiscordIpcClient.cs
//
//  Changelog:
//      Paulinchen  2026-10-06: Left join secrets out of the log
//      Paulinchen  2026-09-29: Stopped accepting a friend's request to join, which moved a hosting friend's game into the player's party
//      Paulinchen  2026-09-28: Subscribed to Discord's invite events and passed them on, with the user's name from READY
//                            - Added accepting a friend's request to join
//      Paulinchen  2026-09-27: Dropped IDisposable, nothing disposed the client
//                            - Documented that a null activity clears the profile
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MGQParadox.DiscordPresence.Discord;

/// <summary>
/// A connection to the local Discord client over its IPC pipe.
/// </summary>
internal sealed class DiscordIpcClient
{
    /// <summary>
    /// Number of pipe names Discord may listen on.
    /// </summary>
    private const int PipeCount = 10;

    /// <summary>
    /// How long a single pipe gets to accept the connection, in milliseconds.
    /// </summary>
    private const int ConnectTimeoutMs = 500;

    /// <summary>
    /// How long a single frame of the handshake may take to arrive, in milliseconds.
    /// </summary>
    private const int HandshakeFrameTimeoutMs = 3000;

    /// <summary>
    /// Size of the opcode and length ahead of every body.
    /// </summary>
    private const int FrameHeaderSize = 8;

    /// <summary>
    /// Largest body accepted, anything longer means the stream is out of step.
    /// </summary>
    private const int MaxFrameLength = 1 << 20;

    /// <summary>
    /// How much of a body is written to the log.
    /// </summary>
    private const int LogExcerptLength = 400;

    /// <summary>
    /// How long Discord gets to confirm the handshake.
    /// </summary>
    private static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(6);

    /// <summary>
    /// The join secrets in a body, which let their holder into the player's world and so stay out of the log.
    /// </summary>
    private static readonly Regex SecretsPattern = new("\"secrets\":\\{[^}]*\\}", RegexOptions.Compiled);

    /// <summary>
    /// The Discord application the presence is shown for.
    /// </summary>
    private readonly string _clientId;

    /// <summary>
    /// Keeps a pong from the drain thread from interleaving with a command.
    /// </summary>
    private readonly object _writeLock = new();

    /// <summary>
    /// The open pipe, or <see langword="null"/> while disconnected.
    /// </summary>
    private NamedPipeClientStream? _pipe;

    /// <summary>
    /// Creates a client that is not connected yet.
    /// </summary>
    /// <param name="clientId">The Discord application the presence is shown for.</param>
    public DiscordIpcClient(string clientId)
    {
        _clientId = clientId;
    }

    /// <summary>
    /// Whether Discord has confirmed the connection and not closed it since.
    /// </summary>
    public bool IsConnected => Volatile.Read(ref _pipe) is { IsConnected: true };

    /// <summary>
    /// The name of the user Discord is logged in as, once READY told it.
    /// </summary>
    public string? UserName { get; private set; }

    /// <summary>
    /// Receives the invite events Discord sends, on the thread that reads the pipe.
    /// </summary>
    public Action<DiscordDispatch>? Dispatched { get; set; }

    /// <summary>
    /// Connects to the first Discord that answers.
    /// </summary>
    /// <returns><see langword="true"/> once Discord has confirmed the connection.</returns>
    public bool Connect()
    {
        Disconnect();

        for (var index = 0; index < PipeCount; index++)
        {
            if (TryConnect($"discord-ipc-{index}"))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Shows an activity on the profile.
    /// </summary>
    /// <param name="processId">The process the activity belongs to, Discord clears it when that ends.</param>
    /// <param name="activityJson">The activity, as written by <see cref="Activity.ToJson"/>, or the JSON <c>null</c> to clear the profile.</param>
    public void SetActivity(int processId, string activityJson)
    {
        var pipe = Volatile.Read(ref _pipe) ?? throw new InvalidOperationException("Not connected to Discord.");
        var nonce = Json.Quote(Guid.NewGuid().ToString());

        Write(pipe, Opcode.Frame,
            "{\"cmd\":\"SET_ACTIVITY\",\"args\":{\"pid\":" + processId + ",\"activity\":" + activityJson + "},\"nonce\":" + nonce + "}");
    }

    /// <summary>
    /// Closes the pipe, if one is open.
    /// </summary>
    public void Disconnect() => Close(Interlocked.Exchange(ref _pipe, null));

    /// <summary>
    /// Opens one pipe and completes the handshake on it.
    /// </summary>
    /// <param name="pipeName">The pipe to try.</param>
    /// <returns><see langword="true"/> when Discord is listening there and accepted the client id.</returns>
    private bool TryConnect(string pipeName)
    {
        var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

        try
        {
            pipe.Connect(ConnectTimeoutMs);
            Write(pipe, Opcode.Handshake, "{\"v\":1,\"client_id\":" + Json.Quote(_clientId) + "}");

            if (!AwaitReady(pipe))
            {
                Log.Write($"no READY on {pipeName} (client_id rejected?) - dropping");
                Close(pipe);
                return false;
            }

            Subscribe(pipe, DiscordDispatch.ActivityJoin);
            Subscribe(pipe, DiscordDispatch.ActivityJoinRequest);
            Volatile.Write(ref _pipe, pipe);
            StartDrainThread(pipe);
            Log.Write($"connected and READY on {pipeName}");
            return true;
        }
        catch (TimeoutException)
        {
            Close(pipe);
            return false;
        }
        catch (Exception ex)
        {
            Log.Write($"{pipeName}: {ex.Message}");
            Close(pipe);
            return false;
        }
    }

    /// <summary>
    /// Waits for the READY event that confirms a handshake.
    /// </summary>
    /// <remarks>
    /// Discord silently ignores every command sent before READY, so a handshake only counts once
    /// it has been answered.
    /// </remarks>
    /// <param name="pipe">The pipe the handshake was sent on.</param>
    /// <returns><see langword="true"/> once READY arrived.</returns>
    private bool AwaitReady(NamedPipeClientStream pipe)
    {
        var deadline = DateTime.UtcNow + ReadyTimeout;

        while (DateTime.UtcNow < deadline && TryReadFrame(pipe, HandshakeFrameTimeoutMs, out var opcode, out var body))
        {
            Log.Write($"discord -> op={(int)opcode} {Excerpt(body)}");

            if (opcode == Opcode.Close)
            {
                return false;
            }

            if (opcode == Opcode.Frame && body.Contains("\"READY\""))
            {
                UserName = DiscordDispatch.TryParse(body)?.UserName;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Asks Discord to send an event from now on.
    /// </summary>
    /// <param name="pipe">The connected pipe.</param>
    /// <param name="eventName">The event, such as <see cref="DiscordDispatch.ActivityJoin"/>.</param>
    private void Subscribe(NamedPipeClientStream pipe, string eventName) =>
        Write(pipe, Opcode.Frame,
            "{\"cmd\":\"SUBSCRIBE\",\"evt\":" + Json.Quote(eventName) + ",\"nonce\":" + Json.Quote(Guid.NewGuid().ToString()) + "}");

    /// <summary>
    /// Reads everything Discord sends from here on, in the background.
    /// </summary>
    /// <remarks>
    /// Discord answers every command and pings now and then, which fill the pipe until writing to
    /// it blocks when left unread.
    /// </remarks>
    /// <param name="pipe">The connected pipe.</param>
    private void StartDrainThread(NamedPipeClientStream pipe)
    {
        new Thread(() => Drain(pipe)) { IsBackground = true }.Start();
    }

    /// <summary>
    /// Answers pings, passes events on to <see cref="Dispatched"/> and logs every other frame until
    /// the pipe closes, then lets go of it.
    /// </summary>
    /// <remarks>
    /// Ends quietly on any failure, since an exception escaping a thread would end the whole game.
    /// </remarks>
    /// <param name="pipe">The pipe to read.</param>
    private void Drain(NamedPipeClientStream pipe)
    {
        try
        {
            while (pipe.IsConnected && TryReadFrame(pipe, Timeout.Infinite, out var opcode, out var body))
            {
                switch (opcode)
                {
                    case Opcode.Ping:
                        Write(pipe, Opcode.Pong, body);
                        break;
                    case Opcode.Close:
                        Log.Write($"discord closed: {Excerpt(body)}");
                        return;
                    case Opcode.Frame when DiscordDispatch.TryParse(body) is { } dispatch:
                        Log.Write($"discord event: {dispatch.Event}");
                        PassOn(dispatch);
                        break;
                    default:
                        Log.Write(body.Contains("\"ERROR\"")
                            ? $"discord REJECTED: {Excerpt(body)}"
                            : $"discord ack: {Excerpt(body)}");
                        break;
                }
            }
        }
        catch
        {
        }
        finally
        {
            if (Interlocked.CompareExchange(ref _pipe, null, pipe) == pipe)
            {
                Log.Write("discord connection lost");
            }

            Close(pipe);
        }
    }

    /// <summary>
    /// Hands an event to <see cref="Dispatched"/>.
    /// </summary>
    /// <remarks>
    /// A failing receiver must not end the thread that reads the pipe.
    /// </remarks>
    /// <param name="dispatch">The event.</param>
    private void PassOn(DiscordDispatch dispatch)
    {
        try
        {
            Dispatched?.Invoke(dispatch);
        }
        catch (Exception ex)
        {
            Log.Write($"discord event {dispatch.Event} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Closes a pipe, ignoring one that is already broken.
    /// </summary>
    /// <param name="pipe">The pipe to close, if any.</param>
    private static void Close(NamedPipeClientStream? pipe)
    {
        try
        {
            pipe?.Dispose();
        }
        catch
        {
        }
    }

    /// <summary>
    /// Sends one frame.
    /// </summary>
    /// <param name="pipe">The pipe to write to.</param>
    /// <param name="opcode">The kind of frame.</param>
    /// <param name="json">The body.</param>
    private void Write(NamedPipeClientStream pipe, Opcode opcode, string json)
    {
        var body = Encoding.UTF8.GetBytes(json);
        var frame = new byte[FrameHeaderSize + body.Length];

        Buffer.BlockCopy(BitConverter.GetBytes((int)opcode), 0, frame, 0, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(body.Length), 0, frame, 4, 4);
        Buffer.BlockCopy(body, 0, frame, FrameHeaderSize, body.Length);

        lock (_writeLock)
        {
            pipe.Write(frame, 0, frame.Length);
            pipe.Flush();
        }
    }

    /// <summary>
    /// Reads one frame.
    /// </summary>
    /// <param name="pipe">The pipe to read from.</param>
    /// <param name="timeoutMs">How long each read may wait, or <see cref="Timeout.Infinite"/>.</param>
    /// <param name="opcode">The kind of frame.</param>
    /// <param name="body">The body, decoded.</param>
    /// <returns><see langword="false"/> when the pipe closed, timed out or sent a malformed header.</returns>
    private static bool TryReadFrame(NamedPipeClientStream pipe, int timeoutMs, out Opcode opcode, out string body)
    {
        opcode = default;
        body = string.Empty;

        var header = new byte[FrameHeaderSize];

        if (!TryReadExactly(pipe, header, timeoutMs))
        {
            return false;
        }

        var length = BitConverter.ToInt32(header, 4);

        if (length < 0 || length > MaxFrameLength)
        {
            return false;
        }

        var content = new byte[length];

        if (!TryReadExactly(pipe, content, timeoutMs))
        {
            return false;
        }

        opcode = (Opcode)BitConverter.ToInt32(header, 0);
        body = Encoding.UTF8.GetString(content);
        return true;
    }

    /// <summary>
    /// Fills a buffer completely from the pipe.
    /// </summary>
    /// <param name="pipe">The pipe to read from.</param>
    /// <param name="buffer">The buffer to fill.</param>
    /// <param name="timeoutMs">How long each read may wait, or <see cref="Timeout.Infinite"/>.</param>
    /// <returns><see langword="false"/> when the pipe closed, failed or timed out first.</returns>
    private static bool TryReadExactly(NamedPipeClientStream pipe, byte[] buffer, int timeoutMs)
    {
        var received = 0;

        while (received < buffer.Length)
        {
            try
            {
                var read = pipe.BeginRead(buffer, received, buffer.Length - received, null, null);

                if (!read.AsyncWaitHandle.WaitOne(timeoutMs))
                {
                    return false;
                }

                var count = pipe.EndRead(read);

                if (count <= 0)
                {
                    return false;
                }

                received += count;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Shortens a body to a single log line.
    /// </summary>
    /// <param name="body">The body to shorten.</param>
    /// <returns>The body on one line, cut off when long.</returns>
    private static string Excerpt(string body)
    {
        var line = SecretsPattern.Replace(body.Replace("\r", " ").Replace("\n", " "), "\"secrets\":{...}");

        return line.Length > LogExcerptLength ? line.Substring(0, LogExcerptLength) + "..." : line;
    }
}
