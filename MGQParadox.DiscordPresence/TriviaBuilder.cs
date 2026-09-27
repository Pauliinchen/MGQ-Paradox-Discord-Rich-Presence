//----------------------------------------------------------------
//  TriviaBuilder.cs
//
//  Changelog:
//      Paulinchen  2026-09-27: Added the medals earned
//                            - Created
//
//----------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using MGQParadox.DiscordPresence.Game;

namespace MGQParadox.DiscordPresence;

/// <summary>
/// Writes the trivia, the second Discord line, of which the presence shows one at a time.
/// </summary>
internal static class TriviaBuilder
{
    /// <summary>
    /// Route of the final chapter whose lines only show while it is played.
    /// </summary>
    private const string ChaosRoute = "chaos";

    /// <summary>
    /// Part of the story the routes belong to.
    /// </summary>
    private const int FinalPart = 3;

    /// <summary>
    /// Number of base parameters an actor has, whose leaders take turns.
    /// </summary>
    private const int ParamCount = 8;

    /// <summary>
    /// Time a used item stays among the lines.
    /// </summary>
    private static readonly TimeSpan ItemUseShown = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Time before the top master and top stat lines move on to the next one.
    /// </summary>
    private static readonly TimeSpan RotationInterval = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Name and comment by the game's difficulty value.
    /// </summary>
    private static readonly Dictionary<long, (string Name, string Comment)> Difficulties = new()
    {
        [-2] = ("Very Easy", "Here for the story, and that's fine"),
        [-1] = ("Easy", "Taking it nice and slow"),
        [0] = ("Normal", "The way it's meant to be played"),
        [1] = ("Hard", "Starting to sweat a little"),
        [2] = ("Very Hard", "Pain is a choice, and they chose it"),
        [3] = ("Hell", "Welcome to hell, enjoy your stay"),
        [4] = ("Paradox", "Has lost all sense of self-preservation"),
    };

    /// <summary>
    /// Comment on the playtime by the hours it takes, lowest first. The highest one reached shows.
    /// </summary>
    private static readonly (long Hours, string Comment)[] PlaytimeComments =
    {
        (0, "Still an Apprentice Hero fresh out of Iliasville"),
        (10, "Has learned that every loss is a new bad end"),
        (25, "Starting to understand the job system... probably"),
        (50, "The Pocket Castle is starting to feel like home"),
        (100, "The job and race grind has begun in earnest"),
        (200, "Has seen more of the Labyrinth of Chaos than the sun"),
        (400, "Ilias has stopped answering their prayers"),
        (700, "The grind never ends, and neither do they"),
        (1000, "Has become the true Paradox"),
    };

    /// <summary>
    /// Names of the sides taken at the Navy Headquarters, by the key the game script publishes.
    /// </summary>
    private static readonly Dictionary<string, string> NavalSideNames = new()
    {
        ["pirates"] = "Pirates",
        ["marines"] = "Marines",
    };

    /// <summary>
    /// Every line, in the order they rotate. A line of a part or route only rotates while it is played.
    /// </summary>
    private static readonly TriviaLine[] Lines =
    {
        Line(DeadPartyMembers),
        Line(RecruitedMembers),
        Line(MostAffection),
        Line(BattlesFought),
        Line(Difficulty),
        Line(Playtime),
        Line(LastItemUsed),
        Line(CurrentTrack),
        Line(TopMaster),
        Line(EnemiesDefeated),
        Line(BattlesEscaped),
        Line(Wipeouts),
        Line(TopStat),
        Line(BiggestHit),
        Line(GoldSpent),
        Line(ItemsSynthesized),
        Line(DeepestLabyrinthFloor),
        Line(MedalsEarned),
        Line(RequestsMade) with { IsNsfw = true },
        Line(MostRequested) with { IsNsfw = true },
        Line(TimesRaped) with { IsNsfw = true },
        Line(MostRapedBy) with { IsNsfw = true },
        Line(BattlefucksWon) with { IsNsfw = true },
        Line(GoldCarried),
        Line(SpiritsRecruited) with { Parts = new[] { 1, 2 } },
        Line(ForgingOre) with { Parts = new[] { 1, 2 } },
        Line(NavalSide) with { Parts = new[] { 2 } },
        Line(QueensRecruited) with { Parts = new[] { 2 } },
        Line(RoutesCleared) with { Parts = new[] { FinalPart } },
        Line(RandolphsFound) with { Parts = new[] { FinalPart }, IsSpoiler = true },
        Line(PhenomenaDefeated) with { Parts = new[] { FinalPart }, Route = ChaosRoute, IsSpoiler = true },
    };

    /// <summary>
    /// Writes the lines that currently apply.
    /// </summary>
    /// <remarks>
    /// Spoilers stay out while the game hides them, and requests, defeat scenes and battle fucks while
    /// its NSFW option is off.
    /// </remarks>
    /// <param name="status">The status the game published.</param>
    /// <param name="now">The current time, which the item and rotating lines depend on.</param>
    /// <returns>The lines, in the order they rotate.</returns>
    public static IReadOnlyList<string> LinesOf(GameStatus status, DateTimeOffset now) =>
        Lines.Where(line => Applies(line, status))
             .Select(line => line.Write(status, now))
             .OfType<string>()
             .ToList();

    /// <summary>
    /// Reports whether a line belongs to the point of the story and the options the game published.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <param name="status">The status the game published.</param>
    /// <returns><see langword="true"/> when the line may show.</returns>
    private static bool Applies(TriviaLine line, GameStatus status) =>
        (line.Parts.Length == 0 || line.Parts.Contains(status.Part)) &&
        (line.Route == null || line.Route == status.Route) &&
        !(line.IsSpoiler && status.HidesSpoilers) &&
        !(line.IsNsfw && !status.IsNsfw);

    /// <summary>
    /// Wraps a line that only depends on the status.
    /// </summary>
    /// <param name="write">Writes the line.</param>
    /// <returns>The line, shown throughout.</returns>
    private static TriviaLine Line(Func<GameStatus, string?> write) => new((status, _) => write(status));

    /// <summary>
    /// Wraps a line that also depends on the time.
    /// </summary>
    /// <param name="write">Writes the line.</param>
    /// <returns>The line, shown throughout.</returns>
    private static TriviaLine Line(Func<GameStatus, DateTimeOffset, string?> write) => new(write);

    /// <summary>
    /// Counts up every <see cref="RotationInterval"/>, which picks the top master and the top stat.
    /// </summary>
    /// <param name="now">The current time.</param>
    /// <returns>The current rotation.</returns>
    private static long Rotation(DateTimeOffset now) =>
        now.ToUnixTimeSeconds() / (long)RotationInterval.TotalSeconds;

    /// <summary>
    /// Joins items the way a sentence lists them: "A", "A and B", "A, B and C".
    /// </summary>
    /// <param name="items">The items.</param>
    /// <returns>The list.</returns>
    private static string Listed(IReadOnlyList<string> items) =>
        items.Count < 2 ? string.Concat(items) : $"{string.Join(", ", items.Take(items.Count - 1))} and {items[^1]}";

    /// <summary>
    /// How many of the active party are down.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> while nobody is down.</returns>
    private static string? DeadPartyMembers(GameStatus status) =>
        status.DeadMembers > 0 ? $"Currently has {NumberFormat.Counted(status.DeadMembers, "dead party member")}!" : null;

    /// <summary>
    /// How many companions have joined, Luka not counted.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> when unknown.</returns>
    private static string? RecruitedMembers(GameStatus status) =>
        status.Companions is { } companions ? $"Has recruited {companions} party members this playthrough!" : null;

    /// <summary>
    /// The recruited companions with the most affection.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> while no companion has any affection.</returns>
    private static string? MostAffection(GameStatus status)
    {
        var ranked = status.MostAffection;

        return ranked.Count > 0
            ? $"Most affection with {Listed(ranked.Select(entry => $"{entry.Name} ({NumberFormat.Grouped(entry.Love)})").ToList())}!"
            : null;
    }

    /// <summary>
    /// How many battles this save has fought.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> when unknown.</returns>
    private static string? BattlesFought(GameStatus status) =>
        status.Battles is { } battles ? $"Has fought {NumberFormat.Grouped(battles)} battles!" : null;

    /// <summary>
    /// The difficulty, with a comment on it.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> for an unknown difficulty.</returns>
    private static string? Difficulty(GameStatus status) =>
        status.Difficulty is { } value && Difficulties.TryGetValue(value, out var difficulty)
            ? $"Currently playing on {difficulty.Name} - {difficulty.Comment}!"
            : null;

    /// <summary>
    /// The playtime in hours, with a comment on it.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> when unknown.</returns>
    private static string? Playtime(GameStatus status)
    {
        if (status.PlaytimeHours is not { } hours)
        {
            return null;
        }

        var comment = PlaytimeComments.Last(entry => hours >= entry.Hours).Comment;

        return hours == 0
            ? $"Is less than an hour in. {comment}!"
            : $"Is {NumberFormat.Counted(hours, "hour")} in. {comment}!";
    }

    /// <summary>
    /// The item an actor used last, for <see cref="ItemUseShown"/>.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The line, or <see langword="null"/> when no item was used lately.</returns>
    private static string? LastItemUsed(GameStatus status, DateTimeOffset now) =>
        status.ItemUsed.Length > 0 && status.ItemUsedAt is { } usedAt &&
        now.ToUnixTimeSeconds() - usedAt <= ItemUseShown.TotalSeconds
            ? $"Just used {status.ItemUsed} on {status.ItemTarget}!"
            : null;

    /// <summary>
    /// The music that is playing, named like in the music room of Kagetsumugi's jukebox.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> when no known music plays.</returns>
    private static string? CurrentTrack(GameStatus status) =>
        status.Track.Length > 0 ? $"Currently vibing to {status.Track}!" : null;

    /// <summary>
    /// Names one of the actors with the most mastered jobs and races, taking turns every
    /// <see cref="RotationInterval"/>.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The line, or <see langword="null"/> while nobody has mastered anything.</returns>
    private static string? TopMaster(GameStatus status, DateTimeOffset now)
    {
        var ranked = status.TopMasters;

        if (ranked.Count == 0)
        {
            return null;
        }

        var master = ranked[(int)(Rotation(now) % ranked.Count)];

        return $"{master.Name} has mastered {master.Jobs} Jobs and {master.Races} Races already!";
    }

    /// <summary>
    /// How many enemies were defeated.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? EnemiesDefeated(GameStatus status) =>
        status.EnemiesDefeated > 0 ? $"Has defeated {NumberFormat.Counted(status.EnemiesDefeated, "enemy", "enemies")}!" : null;

    /// <summary>
    /// How many battles were run away from.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? BattlesEscaped(GameStatus status) =>
        status.BattlesEscaped > 0 ? $"Has run away from {NumberFormat.Counted(status.BattlesEscaped, "battle")}!" : null;

    /// <summary>
    /// How often the party was wiped out.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? Wipeouts(GameStatus status) =>
        status.Wipeouts > 0 ? $"Has been wiped out {NumberFormat.Counted(status.Wipeouts, "time")}!" : null;

    /// <summary>
    /// Names who in the active party leads in one stat, a different stat every
    /// <see cref="RotationInterval"/>.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <param name="now">The current time.</param>
    /// <returns>The line, or <see langword="null"/> when the stat's leader is unknown.</returns>
    private static string? TopStat(GameStatus status, DateTimeOffset now) =>
        status.TopStat((int)(Rotation(now) % ParamCount)) is { } top
            ? $"{top.Holder} has the highest {top.Stat} ({top.Value}) in the party!"
            : null;

    /// <summary>
    /// The biggest hit dealt.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? BiggestHit(GameStatus status) =>
        status.BiggestHit.Length > 0 ? $"Biggest hit dealt: {status.BiggestHit} damage!" : null;

    /// <summary>
    /// How much gold was spent in shops.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first purchase.</returns>
    private static string? GoldSpent(GameStatus status) =>
        status.GoldSpent > 0 ? $"Has spent {NumberFormat.Grouped(status.GoldSpent)} gold in shops!" : null;

    /// <summary>
    /// How many items were synthesized.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? ItemsSynthesized(GameStatus status) =>
        status.ItemsSynthesized > 0 ? $"Has synthesized {NumberFormat.Counted(status.ItemsSynthesized, "item")}!" : null;

    /// <summary>
    /// The deepest Labyrinth of Chaos floor reached.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first floor.</returns>
    private static string? DeepestLabyrinthFloor(GameStatus status) =>
        status.LabyrinthRecord > 0 ? $"Has reached floor {status.LabyrinthRecord} in the Labyrinth of Chaos!" : null;

    /// <summary>
    /// How many of the game's medals have been earned, across all saves like the game counts them.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? MedalsEarned(GameStatus status) =>
        OutOf(status.MedalsEarned, "Has earned {0} out of {1} medals!");

    /// <summary>
    /// How many requests this save has made.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? RequestsMade(GameStatus status) =>
        status.RequestsMade > 0 ? $"Has made {NumberFormat.Counted(status.RequestsMade, "request")}!" : null;

    /// <summary>
    /// Who this save has made the most requests to.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? MostRequested(GameStatus status) =>
        status.MostRequested is { Name.Length: > 0 } top
            ? $"Has requested {top.Name} the most, {NumberFormat.Counted(top.Count, "time")}!"
            : null;

    /// <summary>
    /// How many defeat scenes this save has seen.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? TimesRaped(GameStatus status) =>
        status.TimesRaped > 0 ? $"Has been raped {NumberFormat.Counted(status.TimesRaped, "time")}!" : null;

    /// <summary>
    /// Which monster girl this save has seen the most defeat scenes of.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? MostRapedBy(GameStatus status) =>
        status.MostRapedBy is { Name.Length: > 0 } top
            ? $"Raped by {top.Name} the most, {NumberFormat.Counted(top.Count, "time")}!"
            : null;

    /// <summary>
    /// How many battle fucks were won.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? BattlefucksWon(GameStatus status) =>
        status.BattlefucksWon > 0 ? $"Has won {NumberFormat.Counted(status.BattlefucksWon, "battlefuck")}!" : null;

    /// <summary>
    /// How much gold the party carries.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> when unknown.</returns>
    private static string? GoldCarried(GameStatus status) =>
        status.Gold is { } gold ? $"Currently carrying {NumberFormat.Grouped(gold)} gold!" : null;

    /// <summary>
    /// How many of the four spirits have joined the party.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? SpiritsRecruited(GameStatus status) =>
        OutOf(status.SpiritsRecruited, "Has recruited {0} out of {1} spirits!");

    /// <summary>
    /// The best ore the party holds for forging, the one found last.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first ore.</returns>
    private static string? ForgingOre(GameStatus status) =>
        status.ForgingOre.Length > 0 ? $"Has unlocked {status.ForgingOre} for forging!" : null;

    /// <summary>
    /// Whether Luka sided with the pirates or the marines at the Navy Headquarters.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the choice.</returns>
    private static string? NavalSide(GameStatus status) =>
        NavalSideNames.TryGetValue(status.NavalSide, out var side) ? $"Sided with the {side} this playthrough!" : null;

    /// <summary>
    /// How many of the monster queens have joined the party.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? QueensRecruited(GameStatus status) =>
        OutOf(status.QueensRecruited, "Has recruited {0} out of {1} monster queens!");

    /// <summary>
    /// How many routes of the final chapter this playthrough has cleared.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? RoutesCleared(GameStatus status) =>
        OutOf(status.RoutesCleared, "Has cleared {0} out of {1} routes!");

    /// <summary>
    /// How many of Randolph's hiding places this playthrough has found.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? RandolphsFound(GameStatus status) =>
        OutOf(status.RandolphsFound, "Has found {0} out of {1} Randolphs!");

    /// <summary>
    /// How many of the Phenomena of Ruin the Chaos route has defeated.
    /// </summary>
    /// <param name="status">The status the game published.</param>
    /// <returns>The line, or <see langword="null"/> before the first.</returns>
    private static string? PhenomenaDefeated(GameStatus status) =>
        OutOf(status.PhenomenaDefeated, "{0} out of {1} Phenomena of Ruin have been defeated!");

    /// <summary>
    /// Writes a count out of a total into a sentence.
    /// </summary>
    /// <param name="progress">The count and the total.</param>
    /// <param name="format">The sentence, with the count at {0} and the total at {1}.</param>
    /// <returns>The line, or <see langword="null"/> while the count is 0.</returns>
    private static string? OutOf((long Done, long Total) progress, string format) =>
        progress.Done > 0 ? string.Format(CultureInfo.InvariantCulture, format, progress.Done, progress.Total) : null;

    /// <summary>
    /// A trivia line and where in the story it shows.
    /// </summary>
    /// <param name="Write">Writes the line, or returns <see langword="null"/> while it does not apply.</param>
    private sealed record TriviaLine(Func<GameStatus, DateTimeOffset, string?> Write)
    {
        /// <summary>
        /// The parts of the story the line shows in, every part when empty.
        /// </summary>
        public int[] Parts { get; init; } = Array.Empty<int>();

        /// <summary>
        /// The route of the final chapter the line shows on, every route when <see langword="null"/>.
        /// </summary>
        public string? Route { get; init; }

        /// <summary>
        /// Whether the line spoils Part 3 and stays out while spoilers are hidden.
        /// </summary>
        public bool IsSpoiler { get; init; }

        /// <summary>
        /// Whether the line is about requests, defeat scenes or battle fucks.
        /// </summary>
        public bool IsNsfw { get; init; }
    }
}
