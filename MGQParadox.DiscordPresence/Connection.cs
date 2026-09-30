//----------------------------------------------------------------
//  Connection.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

using System.Threading;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// The connection with a friend that another mod, such as Monster Girl Quest! Online, reports through the
/// game script: whether the player hosts or plays with a friend, which the activity shows, and the
/// invite the player accepted in Discord, which that mod takes to join.
/// </summary>
/// <remarks>
/// The game script writes it on the game's thread, the presence loop and Discord's pipe read it on theirs.
/// </remarks>
internal sealed class Connection
{
    /// <summary>
    /// Longest join secret Discord takes.
    /// </summary>
    public const int MaxJoinSecretLength = 128;

    /// <summary>
    /// Guards every field below.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// Whether the player hosts or plays with a friend.
    /// </summary>
    private ConnectionKind _kind;

    /// <summary>
    /// Names the party on Discord, the same for both players.
    /// </summary>
    private string? _partyId;

    /// <summary>
    /// What Discord hands a friend who joins, while hosting.
    /// </summary>
    private string? _joinSecret;

    /// <summary>
    /// The friend's name, while connected.
    /// </summary>
    private string? _friend;

    /// <summary>
    /// The join secret of an invite the player accepted in Discord, until the other mod takes it.
    /// </summary>
    private string? _invite;

    /// <summary>
    /// The player's name on Discord.
    /// </summary>
    private string? _playerName;

    /// <summary>
    /// The game's connection, which the game script and Discord's events share.
    /// </summary>
    public static Connection Current { get; } = new();

    /// <summary>
    /// The party and join secret while the player hosts.
    /// </summary>
    public (string PartyId, string JoinSecret)? Hosting
    {
        get
        {
            lock (_gate)
            {
                return _kind == ConnectionKind.Hosting ? (_partyId!, _joinSecret!) : null;
            }
        }
    }

    /// <summary>
    /// The party and the friend's name while the player plays with a friend.
    /// </summary>
    public (string PartyId, string Friend)? Connected
    {
        get
        {
            lock (_gate)
            {
                return _kind == ConnectionKind.Connected ? (_partyId!, _friend!) : null;
            }
        }
    }

    /// <summary>
    /// The player's name on Discord, <see langword="null"/> until Discord told it.
    /// </summary>
    public string? PlayerName
    {
        get => Volatile.Read(ref _playerName);
        set => Volatile.Write(ref _playerName, value);
    }

    /// <summary>
    /// Takes the connection the game script reports. A report missing what its kind needs counts as none.
    /// </summary>
    /// <param name="kind">"hosting", "connected", or anything else for none.</param>
    /// <param name="partyId">Names the party.</param>
    /// <param name="joinSecret">What a friend who joins gets, for hosting.</param>
    /// <param name="friend">The friend's name, for connected.</param>
    public void Report(string kind, string partyId, string joinSecret, string friend)
    {
        var reported = kind switch
        {
            "hosting" when partyId.Length > 0 && IsJoinSecret(joinSecret) => ConnectionKind.Hosting,
            "connected" when partyId.Length > 0 && friend.Length > 0 => ConnectionKind.Connected,
            _ => ConnectionKind.None,
        };

        lock (_gate)
        {
            _kind = reported;
            _partyId = reported == ConnectionKind.None ? null : partyId;
            _joinSecret = reported == ConnectionKind.Hosting ? joinSecret : null;
            _friend = reported == ConnectionKind.Connected ? friend : null;
        }
    }

    /// <summary>
    /// Keeps the join secret of an invite the player accepted in Discord, until the other mod takes it.
    /// </summary>
    /// <param name="joinSecret">The invite's join secret.</param>
    /// <returns><see langword="false"/> when it is no join secret Discord could have sent.</returns>
    public bool ReceiveInvite(string joinSecret)
    {
        if (!IsJoinSecret(joinSecret))
        {
            return false;
        }

        lock (_gate)
        {
            _invite = joinSecret;
        }

        return true;
    }

    /// <summary>
    /// Looks at the waiting invite without taking it.
    /// </summary>
    /// <returns>Its join secret, or <see langword="null"/> while none waits.</returns>
    public string? PeekInvite()
    {
        lock (_gate)
        {
            return _invite;
        }
    }

    /// <summary>
    /// Takes the waiting invite, so it is handed over once.
    /// </summary>
    public void TakeInvite()
    {
        lock (_gate)
        {
            _invite = null;
        }
    }

    /// <summary>
    /// Reports whether a text can be a join secret.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns><see langword="true"/> when it is not empty and fits Discord's limit.</returns>
    private static bool IsJoinSecret(string text) => text.Length is > 0 and <= MaxJoinSecretLength;
}
