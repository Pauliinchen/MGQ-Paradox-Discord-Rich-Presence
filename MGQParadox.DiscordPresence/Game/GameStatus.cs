//----------------------------------------------------------------
//  GameStatus.cs
//
//  Changelog:
//      Paulinchen  2026-09-26: Read whether the camp music plays
//                            - Read who plays a running request or defeat scene, and how often it happened
//                            - Read when the player last pressed a button
//                            - Named the game script by its new file name
//      Paulinchen  2026-09-25: Created
//
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

namespace MGQParadox.DiscordPresence.Game;

/// <summary>
/// One snapshot of the game, as GameScript/Discord_RPC.rb published it.
/// </summary>
internal sealed class GameStatus
{
    /// <summary>
    /// The published values by key.
    /// </summary>
    private readonly Dictionary<string, string> _values;

    /// <summary>
    /// Creates a snapshot over parsed values.
    /// </summary>
    /// <param name="values">The published values by key.</param>
    private GameStatus(Dictionary<string, string> values)
    {
        _values = values;
        Trivia = ReadTrivia();
    }

    /// <summary>
    /// Start of the game session in Unix seconds, <see langword="null"/> when unknown.
    /// </summary>
    public long? StartedAt => UnixSeconds("start");

    /// <summary>
    /// When the player last pressed a button, in Unix seconds, <see langword="null"/> when unknown.
    /// </summary>
    public long? LastInputAt => UnixSeconds("last_input");

    /// <summary>
    /// What the game is showing.
    /// </summary>
    public Scene Scene => Value("scene").ToLowerInvariant() switch
    {
        "title" => Scene.Title,
        "battle" => Scene.Battle,
        "travel" => Scene.Travel,
        "menu" => Scene.Menu,
        "request" => Scene.Request,
        "defeat_scene" => Scene.DefeatScene,
        _ => Scene.Map,
    };

    /// <summary>
    /// How the party crosses the world map, only meaningful in <see cref="Scene.Travel"/>.
    /// </summary>
    public Vehicle Vehicle => Value("vehicle") switch
    {
        "sea" => Vehicle.Sea,
        "air" => Vehicle.Air,
        _ => Vehicle.Foot,
    };

    /// <summary>
    /// Name of the current map.
    /// </summary>
    public string Area => Value("area");

    /// <summary>
    /// Whether a fight takes place on the world map.
    /// </summary>
    public bool IsOnWorldMap => Value("overworld") == "1";

    /// <summary>
    /// Whether the camp music plays.
    /// </summary>
    public bool IsCamping => Value("camping") == "1";

    /// <summary>
    /// Whether the party is inside the Labyrinth of Chaos.
    /// </summary>
    public bool IsInLabyrinth => LabyrinthFloor.Length > 0;

    /// <summary>
    /// Current Labyrinth of Chaos floor.
    /// </summary>
    public string LabyrinthFloor => Value("loc_floor");

    /// <summary>
    /// Kind of Labyrinth of Chaos run, "Normal" or "Carnage".
    /// </summary>
    public string LabyrinthType => Value("loc_type");

    /// <summary>
    /// Rare points collected in the Labyrinth of Chaos, already formatted.
    /// </summary>
    public string LabyrinthRarePoints => Value("loc_rare");

    /// <summary>
    /// Who plays the running request, only set in <see cref="Scene.Request"/>.
    /// </summary>
    public string RequestCharacter => Value("request_with");

    /// <summary>
    /// How many requests this save made to <see cref="RequestCharacter"/>, the running one included.
    /// </summary>
    public int RequestCount => Count("request_count");

    /// <summary>
    /// The monster girl of the running defeat scene, only set in <see cref="Scene.DefeatScene"/>.
    /// </summary>
    public string RapedBy => Value("raped_by");

    /// <summary>
    /// How many defeat scenes this save saw of <see cref="RapedBy"/>, the running one included.
    /// </summary>
    public int RapedCount => Count("raped_count");

    /// <summary>
    /// Name of the party leader.
    /// </summary>
    public string LeaderName => Value("leader");

    /// <summary>
    /// Personal level of the party leader.
    /// </summary>
    public string LeaderLevel => Value("level");

    /// <summary>
    /// Job of the party leader.
    /// </summary>
    public string ClassName => Value("class");

    /// <summary>
    /// Job level of the party leader.
    /// </summary>
    public string ClassLevel => Value("class_level");

    /// <summary>
    /// Race of the party leader.
    /// </summary>
    public string RaceName => Value("race");

    /// <summary>
    /// Race level of the party leader.
    /// </summary>
    public string RaceLevel => Value("race_level");

    /// <summary>
    /// The trivia lines that currently apply, in the order Discord_RPC.rb lists them.
    /// </summary>
    public IReadOnlyList<string> Trivia { get; }

    /// <summary>
    /// Parses the published text.
    /// </summary>
    /// <param name="text">The <c>key=value</c> lines.</param>
    /// <returns>The snapshot, or <see langword="null"/> when the text holds no value at all.</returns>
    public static GameStatus? Parse(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in text.Split('\n'))
        {
            var separator = line.IndexOf('=');

            if (separator > 0)
            {
                values[line.Substring(0, separator).Trim()] = line.Substring(separator + 1).Trim();
            }
        }

        return values.Count > 0 ? new GameStatus(values) : null;
    }

    /// <summary>
    /// Looks up a published value.
    /// </summary>
    /// <param name="key">The key Discord_RPC.rb published it under.</param>
    /// <returns>The value, or an empty string when absent.</returns>
    private string Value(string key) => _values.TryGetValue(key, out var value) ? value : string.Empty;

    /// <summary>
    /// Looks up a published point in time.
    /// </summary>
    /// <param name="key">The key Discord_RPC.rb published it under.</param>
    /// <returns>The time in Unix seconds, or <see langword="null"/> when absent or invalid.</returns>
    private long? UnixSeconds(string key) =>
        long.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? seconds
            : null;

    /// <summary>
    /// Looks up a published count.
    /// </summary>
    /// <param name="key">The key Discord_RPC.rb published it under.</param>
    /// <returns>The count, or 0 when absent or invalid.</returns>
    private int Count(string key) =>
        int.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var count) ? count : 0;

    /// <summary>
    /// Collects <c>trivia0</c>, <c>trivia1</c> and so on up to the first gap.
    /// </summary>
    /// <returns>The trivia lines.</returns>
    private List<string> ReadTrivia()
    {
        var trivia = new List<string>();

        for (var index = 0; Value($"trivia{index}") is { Length: > 0 } line; index++)
        {
            trivia.Add(line);
        }

        return trivia;
    }
}
