//----------------------------------------------------------------
//  InviteLaunchTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-29: Created
//
//----------------------------------------------------------------

using System;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers telling that Discord started the game for an invite, and whether its join came.
/// </summary>
public sealed class InviteLaunchTests
{
    /// <summary>
    /// The Discord application the tests use.
    /// </summary>
    private const string ClientId = "1553096377959981117";

    /// <summary>
    /// The link Discord starts the game with.
    /// </summary>
    private const string Link = "discord-1553096377959981117://";

    /// <summary>
    /// The command line Discord starts the game with.
    /// </summary>
    private const string DiscordCommandLine = "\"C:\\Games\\MGQ\\Game.exe\" \"" + Link + "\"";

    /// <summary>
    /// When the game starts in the tests.
    /// </summary>
    private static readonly DateTime StartedAt = new(2026, 9, 29, 3, 44, 16, DateTimeKind.Utc);

    /// <summary>
    /// When the tests reach Discord.
    /// </summary>
    private static readonly DateTime ConnectedAt = StartedAt + TimeSpan.FromSeconds(1);

    /// <summary>
    /// Asserts that only the application's own link counts, and that it is read up to its end.
    /// </summary>
    [Fact]
    public void DiscordLink_FindsOnlyTheApplicationsLink()
    {
        Assert.Equal(Link, InviteLaunch.DiscordLink(DiscordCommandLine, ClientId));
        Assert.Equal(Link + "join", InviteLaunch.DiscordLink("Game.exe " + Link + "join --other", ClientId));
        Assert.Null(InviteLaunch.DiscordLink("\"C:\\Games\\MGQ\\Game.exe\"", ClientId));
        Assert.Null(InviteLaunch.DiscordLink("\"Game.exe\" \"discord-123://\"", ClientId));
        Assert.Null(InviteLaunch.DiscordLink(null, ClientId));
    }

    /// <summary>
    /// Asserts that a join code in a link, with the addresses it holds, stays out of the log.
    /// </summary>
    [Fact]
    public void Masked_CutsTheJoinCode()
    {
        Assert.Equal(Link + "join?secret=<join code>", InviteLaunch.Masked(Link + "join?secret=mgqmp1;abcdefghjk;47625;203.0.113.7"));
        Assert.Equal(Link, InviteLaunch.Masked(Link));
    }

    /// <summary>
    /// Asserts that a Discord launch waits for the join, and counts as ended once the grace after
    /// reaching Discord is over, not before.
    /// </summary>
    [Fact]
    public void State_EndsAfterTheJoinGrace_WithoutAJoin()
    {
        var launch = Started(DiscordCommandLine);
        launch.Connected(ConnectedAt);

        Assert.Equal(InviteLaunchState.Waiting, launch.State(ConnectedAt + InviteLaunch.JoinGrace - TimeSpan.FromSeconds(1)));
        Assert.Equal(InviteLaunchState.Ended, launch.State(ConnectedAt + InviteLaunch.JoinGrace));
    }

    /// <summary>
    /// Asserts that a launch counts as ended when Discord is never reached within its grace.
    /// </summary>
    [Fact]
    public void State_EndsAfterTheConnectGrace_WhenDiscordIsNeverReached()
    {
        var launch = Started(DiscordCommandLine);

        Assert.Equal(InviteLaunchState.Waiting, launch.State(StartedAt + InviteLaunch.ConnectGrace - TimeSpan.FromSeconds(1)));
        Assert.Equal(InviteLaunchState.Ended, launch.State(StartedAt + InviteLaunch.ConnectGrace));
    }

    /// <summary>
    /// Asserts that a join counts, even after the grace, and that a start without Discord's link is none.
    /// </summary>
    [Fact]
    public void State_JoinedOrNone()
    {
        var later = ConnectedAt + TimeSpan.FromMinutes(1);
        var joined = Started(DiscordCommandLine);
        joined.Connected(ConnectedAt);
        joined.Joined();
        var startedByHand = Started("\"Game.exe\"");
        startedByHand.Connected(ConnectedAt);

        Assert.Equal(InviteLaunchState.Joined, joined.State(later));
        Assert.Equal(InviteLaunchState.None, startedByHand.State(later));
    }

    /// <summary>
    /// Asserts that reaching Discord again after a dropped connection keeps the first time, so the
    /// grace does not start over.
    /// </summary>
    [Fact]
    public void Connected_KeepsTheFirstTime()
    {
        var launch = Started(DiscordCommandLine);
        launch.Connected(ConnectedAt);
        launch.Connected(ConnectedAt + TimeSpan.FromMinutes(5));

        Assert.Equal(InviteLaunchState.Ended, launch.State(ConnectedAt + InviteLaunch.JoinGrace));
    }

    /// <summary>
    /// Makes a launch with a command line, started at <see cref="StartedAt"/>.
    /// </summary>
    /// <param name="commandLine">The game's command line.</param>
    /// <returns>The launch.</returns>
    private static InviteLaunch Started(string commandLine)
    {
        var launch = new InviteLaunch();
        launch.Detect(commandLine, ClientId, StartedAt);
        return launch;
    }
}
