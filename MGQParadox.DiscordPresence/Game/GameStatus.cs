//----------------------------------------------------------------
//  GameStatus.cs
//
//  Changelog:
//      Paulinchen  2026-09-28: Read whose team the player fights in a friend battle
//                            - Read whether the player fights their own team in a mirror match
//      Paulinchen  2026-09-27: Read the medals earned
//                            - Read the screen open in a menu
//                            - Read the values behind the trivia instead of finished trivia lines
//                            - Read the Labyrinth's rare points as a number
//                            - Read whether Part 3 spoilers are hidden
//                            - Read whether the player turned the Rich Presence option off
//                            - Read the act of the Collaboration Scenario
//                            - Read the part of the story, the side chosen and the route
//                            - Read the art asset the Picture option picked
//      Paulinchen  2026-09-26: Read who the player is talking to
//                            - Read who plays a running battle fuck
//                            - Read whether the camp music plays
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
    /// Whether the player turned the Rich Presence option off, so Discord shows nothing.
    /// </summary>
    public bool IsHidden => Value("hidden") == "1";

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
        "battlefuck" => Scene.Battlefuck,
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
    /// The game's class name of the screen open in <see cref="Scene.Menu"/>, such as "Scene_Shop".
    /// </summary>
    public string Screen => Value("screen");

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
    /// Rare points collected in the Labyrinth of Chaos.
    /// </summary>
    public long LabyrinthRarePoints => Number("loc_rare") ?? 0;

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
    /// The battlefucker of the running battle fuck, only set in <see cref="Scene.Battlefuck"/>.
    /// </summary>
    public string BattlefuckPartner => Value("battlefuck_with");

    /// <summary>
    /// Who the player is talking to, empty outside a conversation.
    /// </summary>
    public string ConversationPartner => Value("talking_to");

    /// <summary>
    /// The friend whose team the player fights in a friend battle, empty outside one.
    /// </summary>
    public string FriendBattleOpponent => Value("friend_battle_with");

    /// <summary>
    /// Whether the player fights their own team in a mirror match.
    /// </summary>
    public bool IsInMirrorMatch => Value("friend_battle") == "mirror";

    /// <summary>
    /// The Discord application's art asset to show as the picture, empty for the default picture.
    /// </summary>
    public string Picture => Value("picture");

    /// <summary>
    /// The part of the story, 1 to 3, or 0 when unknown.
    /// </summary>
    public int Part => Count("part");

    /// <summary>
    /// The act of the Collaboration Scenario being played, 1 to 12, or 0 outside it.
    /// </summary>
    public int CollabAct => Count("collab_act");

    /// <summary>
    /// The side this playthrough chose, "ilias" or "alice", empty before the choice.
    /// </summary>
    public string Side => Value("side");

    /// <summary>
    /// The route of the final chapter, "destroyer", "judgment" or "chaos", empty while unknown.
    /// </summary>
    public string Route => Value("route");

    /// <summary>
    /// Whether names, map names and routes that spoil Part 3 are left out.
    /// </summary>
    public bool HidesSpoilers => Value("hide_spoilers") == "1";

    /// <summary>
    /// Whether requests, defeat scenes and battle fucks may show.
    /// </summary>
    public bool IsNsfw => Value("nsfw") == "1";

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
    /// How many of the active party are down.
    /// </summary>
    public long DeadMembers => Number("dead_members") ?? 0;

    /// <summary>
    /// How many companions have joined, Luka not counted, <see langword="null"/> before a save is loaded.
    /// </summary>
    public long? Companions => Number("companions");

    /// <summary>
    /// Gold the party carries, <see langword="null"/> before a save is loaded.
    /// </summary>
    public long? Gold => Number("gold");

    /// <summary>
    /// Battles this save has fought, <see langword="null"/> before a save is loaded.
    /// </summary>
    public long? Battles => Number("battles");

    /// <summary>
    /// The game's difficulty value, -2 (Very Easy) to 4 (Paradox), <see langword="null"/> when unknown.
    /// </summary>
    public long? Difficulty => Number("difficulty");

    /// <summary>
    /// Full hours played in this save, <see langword="null"/> before a save is loaded.
    /// </summary>
    public long? PlaytimeHours => Number("playtime_hours");

    /// <summary>
    /// Deepest Labyrinth of Chaos floor reached.
    /// </summary>
    public long LabyrinthRecord => Number("labyrinth_record") ?? 0;

    /// <summary>
    /// The item an actor used last this session, empty before the first.
    /// </summary>
    public string ItemUsed => Value("item_used");

    /// <summary>
    /// Who <see cref="ItemUsed"/> was used on.
    /// </summary>
    public string ItemTarget => Value("item_target");

    /// <summary>
    /// When <see cref="ItemUsed"/> was used, in Unix seconds, <see langword="null"/> when unknown.
    /// </summary>
    public long? ItemUsedAt => UnixSeconds("item_used_at");

    /// <summary>
    /// Title of the music that is playing, empty when the jukebox does not know it.
    /// </summary>
    public string Track => Value("track");

    /// <summary>
    /// The recruited companions with the most affection, highest first.
    /// </summary>
    public IReadOnlyList<(string Name, long Love)> MostAffection =>
        Ranked("affection_name", rank => (Value($"affection_name{rank}"), Number($"affection_love{rank}") ?? 0));

    /// <summary>
    /// The actors in the active party with the most mastered jobs and races, most first.
    /// </summary>
    public IReadOnlyList<(string Name, long Jobs, long Races)> TopMasters =>
        Ranked("master_name", rank => (Value($"master_name{rank}"), Number($"master_jobs{rank}") ?? 0, Number($"master_races{rank}") ?? 0));

    /// <summary>
    /// Enemies defeated, in this save or all saves as the Statistics option says.
    /// </summary>
    public long EnemiesDefeated => Number("defeated") ?? 0;

    /// <summary>
    /// Battles run away from, in this save or all saves.
    /// </summary>
    public long BattlesEscaped => Number("escaped") ?? 0;

    /// <summary>
    /// Times the party was wiped out, in this save or all saves.
    /// </summary>
    public long Wipeouts => Number("wipeouts") ?? 0;

    /// <summary>
    /// Items synthesized, in this save or all saves.
    /// </summary>
    public long ItemsSynthesized => Number("synthesized") ?? 0;

    /// <summary>
    /// Gold spent in shops, in this save or all saves.
    /// </summary>
    public long GoldSpent => Number("gold_spent") ?? 0;

    /// <summary>
    /// The biggest hit dealt, formatted the way the game writes large numbers, empty before the first.
    /// </summary>
    public string BiggestHit => Value("biggest_hit");

    /// <summary>
    /// Battle fucks won, in this save or all saves.
    /// </summary>
    public long BattlefucksWon => Number("battlefucks_won") ?? 0;

    /// <summary>
    /// Requests this save has made.
    /// </summary>
    public long RequestsMade => Number("requests") ?? 0;

    /// <summary>
    /// Who this save made the most requests to and how often, an empty name before the first.
    /// </summary>
    public (string Name, long Count) MostRequested => (Value("most_requested"), Number("most_requested_count") ?? 0);

    /// <summary>
    /// Defeat scenes this save has seen.
    /// </summary>
    public long TimesRaped => Number("rapes") ?? 0;

    /// <summary>
    /// Which monster girl this save saw the most defeat scenes of and how often, an empty name before the first.
    /// </summary>
    public (string Name, long Count) MostRapedBy => (Value("most_raped_by"), Number("most_raped_count") ?? 0);

    /// <summary>
    /// The game's medals earned, out of how many there are, across all saves.
    /// </summary>
    public (long Done, long Total) MedalsEarned => OutOf("medals", "medals_total");

    /// <summary>
    /// The four spirits recruited, out of how many there are.
    /// </summary>
    public (long Done, long Total) SpiritsRecruited => OutOf("spirits", "spirits_total");

    /// <summary>
    /// The monster queens recruited, out of how many there are.
    /// </summary>
    public (long Done, long Total) QueensRecruited => OutOf("queens", "queens_total");

    /// <summary>
    /// The routes of the final chapter cleared, out of how many there are.
    /// </summary>
    public (long Done, long Total) RoutesCleared => OutOf("routes_cleared", "routes_total");

    /// <summary>
    /// Randolph's hiding places found, out of how many there are.
    /// </summary>
    public (long Done, long Total) RandolphsFound => OutOf("randolphs", "randolphs_total");

    /// <summary>
    /// The Phenomena of Ruin defeated, out of how many there are.
    /// </summary>
    public (long Done, long Total) PhenomenaDefeated => OutOf("phenomena", "phenomena_total");

    /// <summary>
    /// Name of the best ore the party holds for forging, empty before the first.
    /// </summary>
    public string ForgingOre => Value("ore");

    /// <summary>
    /// Side taken at the Navy Headquarters, "pirates" or "marines", empty before the choice.
    /// </summary>
    public string NavalSide => Value("naval_side");

    /// <summary>
    /// Who in the active party leads in a stat.
    /// </summary>
    /// <param name="paramId">The stat, from 0 like the game counts its base parameters.</param>
    /// <returns>The stat's name, its leader and the value as the game writes it, or <see langword="null"/> when unknown.</returns>
    public (string Stat, string Holder, string Value)? TopStat(int paramId) =>
        Value($"stat_holder{paramId}") is { Length: > 0 } holder
            ? (Value($"stat_name{paramId}"), holder, Value($"stat_value{paramId}"))
            : null;

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
    /// Looks up a published number, which may be negative or larger than an <see cref="int"/>.
    /// </summary>
    /// <param name="key">The key Discord_RPC.rb published it under.</param>
    /// <returns>The number, or <see langword="null"/> when absent or invalid.</returns>
    private long? Number(string key) =>
        long.TryParse(Value(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : null;

    /// <summary>
    /// Looks up a count together with the total it is out of.
    /// </summary>
    /// <param name="key">The key of the count.</param>
    /// <param name="totalKey">The key of the total.</param>
    /// <returns>Both, 0 when absent or invalid.</returns>
    private (long Done, long Total) OutOf(string key, string totalKey) => (Number(key) ?? 0, Number(totalKey) ?? 0);

    /// <summary>
    /// Collects a ranked list published as <c>name0</c>, <c>name1</c> and so on up to the first gap.
    /// </summary>
    /// <typeparam name="T">The kind of entry.</typeparam>
    /// <param name="nameKey">The key of an entry's name, without its rank.</param>
    /// <param name="read">Reads the entry of a rank.</param>
    /// <returns>The entries, highest rank first.</returns>
    private List<T> Ranked<T>(string nameKey, Func<int, T> read)
    {
        var entries = new List<T>();

        for (var rank = 0; Value($"{nameKey}{rank}").Length > 0; rank++)
        {
            entries.Add(read(rank));
        }

        return entries;
    }
}
