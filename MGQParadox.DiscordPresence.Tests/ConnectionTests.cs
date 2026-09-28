//----------------------------------------------------------------
//  ConnectionTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

using MGQParadox.DiscordPresence.Discord;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers the connection another mod reports, the activity it makes, and the invites it hands on.
/// </summary>
public sealed class ConnectionTests
{
    /// <summary>
    /// A join secret as the Multiplayer mod writes one.
    /// </summary>
    private const string JoinSecret = "mgqfb1;abcdefghjk;47625;203.0.113.7";

    /// <summary>
    /// Asserts that hosting invites: party 1 of 2, the join secret, the banner and the waiting line.
    /// </summary>
    [Fact]
    public void Hosting_InvitesToTheParty()
    {
        var connection = new Connection();
        connection.Report("hosting", "party", JoinSecret, "");

        var activity = PresenceLoop.WithConnection(Trivia(), connection);

        Assert.Equal(new ActivityParty("party", 1, 2), activity.Party);
        Assert.Equal(JoinSecret, activity.JoinSecret);
        Assert.Equal("invite_cover", activity.InviteCover);
        Assert.Equal("Waiting for a friend", activity.State);
    }

    /// <summary>
    /// Asserts that playing with a friend shows a full party and their name, and invites nobody.
    /// </summary>
    [Fact]
    public void Connected_ShowsTheFriend()
    {
        var connection = new Connection();
        connection.Report("connected", "party", "", "Guest");

        var activity = PresenceLoop.WithConnection(Trivia(), connection);

        Assert.Equal(new ActivityParty("party", 2, 2), activity.Party);
        Assert.Null(activity.JoinSecret);
        Assert.Equal("Playing with Guest", activity.State);
    }

    /// <summary>
    /// Asserts that without a connection, or with a report missing what its kind needs, the trivia stays.
    /// </summary>
    /// <param name="kind">The reported kind.</param>
    /// <param name="party">The reported party.</param>
    /// <param name="joinSecret">The reported join secret.</param>
    /// <param name="friend">The reported friend.</param>
    [Theory]
    [InlineData("", "", "", "")]
    [InlineData("hosting", "party", "", "")]
    [InlineData("hosting", "", JoinSecret, "")]
    [InlineData("connected", "party", "", "")]
    [InlineData("spectating", "party", JoinSecret, "Guest")]
    public void IncompleteReports_KeepTheTrivia(string kind, string party, string joinSecret, string friend)
    {
        var connection = new Connection();
        connection.Report(kind, party, joinSecret, friend);

        var activity = PresenceLoop.WithConnection(Trivia(), connection);

        Assert.Null(activity.Party);
        Assert.Equal("Has fought 12 battles!", activity.State);
    }

    /// <summary>
    /// Asserts that a join secret longer than Discord takes is not offered.
    /// </summary>
    [Fact]
    public void Hosting_WithATooLongJoinSecret_InvitesNobody()
    {
        var connection = new Connection();
        connection.Report("hosting", "party", new string('x', Connection.MaxJoinSecretLength + 1), "");

        Assert.Null(connection.Hosting);
    }

    /// <summary>
    /// Asserts that an accepted invite waits until it is taken, and is handed out once.
    /// </summary>
    [Fact]
    public void Invite_IsHandedOutOnce()
    {
        var connection = new Connection();

        Assert.True(connection.ReceiveInvite(JoinSecret));
        Assert.Equal(JoinSecret, connection.PeekInvite());

        connection.TakeInvite();

        Assert.Null(connection.PeekInvite());
    }

    /// <summary>
    /// Asserts that an invite without a usable join secret is ignored.
    /// </summary>
    /// <param name="length">The length of the invite's join secret.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(Connection.MaxJoinSecretLength + 1)]
    public void UnusableInvites_AreIgnored(int length)
    {
        var connection = new Connection();

        Assert.False(connection.ReceiveInvite(new string('x', length)));
        Assert.Null(connection.PeekInvite());
    }

    /// <summary>
    /// Builds an activity as the trivia would leave it.
    /// </summary>
    /// <returns>The activity.</returns>
    private static Activity Trivia() => new() { Details = "Iliasville - Exploring . . .", State = "Has fought 12 battles!" };
}
