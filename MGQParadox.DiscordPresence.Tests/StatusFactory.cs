//----------------------------------------------------------------
//  StatusFactory.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Created
//
//----------------------------------------------------------------

using System;
using System.Globalization;
using System.Linq;
using MGQParadox.DiscordPresence.Game;

namespace MGQParadox.DiscordPresence.Tests;

/// <summary>
/// Builds game statuses the way Discord_RPC.rb publishes them.
/// </summary>
internal static class StatusFactory
{
    /// <summary>
    /// A fixed point in time the tests measure from, 2026-09-27 12:00 UTC.
    /// </summary>
    public static readonly DateTimeOffset Now = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Builds a status on the map from published values.
    /// </summary>
    /// <param name="values">The values by key, written as the game script writes them.</param>
    /// <returns>The parsed status.</returns>
    public static GameStatus Status(params (string Key, object Value)[] values)
    {
        var lines = values.Prepend(("scene", "map"))
                          .Select(value => $"{value.Key}={Convert.ToString(value.Value, CultureInfo.InvariantCulture)}");

        return GameStatus.Parse(string.Join("\n", lines))!;
    }

    /// <summary>
    /// Writes a point in time the way the game script publishes it.
    /// </summary>
    /// <param name="time">The point in time.</param>
    /// <returns>The Unix seconds.</returns>
    public static long Unix(DateTimeOffset time) => time.ToUnixTimeSeconds();
}
