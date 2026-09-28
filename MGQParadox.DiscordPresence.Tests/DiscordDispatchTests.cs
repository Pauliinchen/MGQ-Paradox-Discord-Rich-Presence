//----------------------------------------------------------------
//  DiscordDispatchTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Created
//
//----------------------------------------------------------------

using MGQParadox.DiscordPresence.Discord;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers reading the events Discord sends on its own.
/// </summary>
public sealed class DiscordDispatchTests
{
    /// <summary>
    /// Asserts that READY names the user, by their display name when they set one.
    /// </summary>
    [Fact]
    public void TryParse_ReadsTheUserOfReady()
    {
        var ready = DiscordDispatch.TryParse(
            "{\"cmd\":\"DISPATCH\",\"evt\":\"READY\",\"data\":{\"v\":1,\"user\":{\"id\":\"42\",\"username\":\"guest_user\",\"global_name\":\"Guest\"}}}");

        Assert.NotNull(ready);
        Assert.Equal(DiscordDispatch.Ready, ready.Event);
        Assert.Equal("42", ready.UserId);
        Assert.Equal("Guest", ready.UserName);
    }

    /// <summary>
    /// Asserts that a user without a display name goes by their user name.
    /// </summary>
    [Fact]
    public void TryParse_FallsBackToTheUserName() =>
        Assert.Equal("guest_user", DiscordDispatch.TryParse(
            "{\"cmd\":\"DISPATCH\",\"evt\":\"ACTIVITY_JOIN_REQUEST\",\"data\":{\"user\":{\"id\":\"42\",\"username\":\"guest_user\",\"global_name\":null}}}")?.UserName);

    /// <summary>
    /// Asserts that an accepted invite hands over its join secret.
    /// </summary>
    [Fact]
    public void TryParse_ReadsTheJoinSecret()
    {
        var join = DiscordDispatch.TryParse("{\"cmd\":\"DISPATCH\",\"evt\":\"ACTIVITY_JOIN\",\"data\":{\"secret\":\"mgqfb1;abcdefghjk;47625;203.0.113.7\"}}");

        Assert.NotNull(join);
        Assert.Equal(DiscordDispatch.ActivityJoin, join.Event);
        Assert.Equal("mgqfb1;abcdefghjk;47625;203.0.113.7", join.Secret);
    }

    /// <summary>
    /// Asserts that answers to commands and broken bodies are no events.
    /// </summary>
    /// <param name="body">The frame's body.</param>
    [Theory]
    [InlineData("{\"cmd\":\"SET_ACTIVITY\",\"data\":{},\"nonce\":\"1\"}")]
    [InlineData("{\"cmd\":\"SUBSCRIBE\",\"data\":{\"evt\":\"ACTIVITY_JOIN\"},\"nonce\":\"2\"}")]
    [InlineData("{\"cmd\":\"DISPATCH\"}")]
    [InlineData("not json")]
    public void TryParse_IgnoresEverythingElse(string body) => Assert.Null(DiscordDispatch.TryParse(body));
}
