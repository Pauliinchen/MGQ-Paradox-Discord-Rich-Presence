//----------------------------------------------------------------
//  UpdateCheckTests.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using System;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Covers how the update check tells a newer release.
/// </summary>
public sealed class UpdateCheckTests
{
    /// <summary>
    /// Asserts that a release build reads its version and a development build none.
    /// </summary>
    /// <param name="informationalVersion">The version the build stamped.</param>
    /// <param name="expected">The release version, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("1.3.5+4357977", "1.3.5")]
    [InlineData("1.4.0", "1.4.0")]
    [InlineData("0.0.0-dev+9611cfb", null)]
    [InlineData("", null)]
    public void ReleaseVersionOf_SkipsDevelopmentBuilds(string informationalVersion, string? expected) =>
        Assert.Equal(expected, UpdateCheck.ReleaseVersionOf(informationalVersion)?.ToString());

    /// <summary>
    /// Asserts that only a newer release is reported, without the tag's <c>v</c>.
    /// </summary>
    /// <param name="installed">The installed version.</param>
    /// <param name="tag">The latest release's tag.</param>
    /// <param name="expected">The reported version, or <see langword="null"/>.</param>
    [Theory]
    [InlineData("1.3.5", "v1.4.0", "1.4.0")]
    [InlineData("1.3.5", "v1.3.10", "1.3.10")]
    [InlineData("1.3.5", "v1.3.5", null)]
    [InlineData("1.4.0", "v1.3.5", null)]
    [InlineData("1.3.5", "nightly", null)]
    public void NewerRelease_ReportsOnlyANewerTag(string installed, string tag, string? expected) =>
        Assert.Equal(expected, UpdateCheck.NewerRelease(Version.Parse(installed), $$"""{"id":1,"tag_name":"{{tag}}","assets":[]}"""));
}
