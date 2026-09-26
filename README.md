# Monster Girl Quest! Paradox RPG – Discord Rich Presence

Shows what you're doing in **Monster Girl Quest! Paradox RPG** on your Discord profile: where you are, what you're up to, and rotating trivia about your playthrough.

![Discord profile showing the party leader tooltip, the Pocket Castle and a trivia line](docs/images/presence.png)

**[Download the latest release](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/releases/latest)**

## Features

| Where you are | What Discord shows |
|---|---|
| Anywhere | `<Location> - Exploring . . .` / `In combat!` / `In menu . . .` |
| World map | `Traveling the world - On foot . . .` / `Sailing . . .` / `Flying . . .` |
| Pocket Castle | `Pocket Castle - Currently in crafting hell...` and other rotating lines |
| Labyrinth of Chaos | `Labyrinth of Chaos <Normal/Carnage> (<Biome>) - Floor <X> \| <Y> Rare Points!` |

**Hover the picture** to see your party leader: `Party leader: <Name> Lv <X> | Race: <Race> Lv <Y> | Class: <Job> Lv <Z>`

**Trivia** rotates every 15 seconds:

- Battles fought, enemies defeated, escapes and wipeouts
- Difficulty and playtime, each with a comment
- The biggest hit dealt, shortened like in the game (`118.497Qnt.`)
- Who in your party has the highest stat, and who has mastered the most jobs and races
- The last item you used, and on whom
- Gold carried and spent, items synthesized, companions recruited, your Ilias or Alice choice, the deepest Labyrinth of Chaos floor, and more

Every number belongs to the save you're playing. See [Per-save statistics](#per-save-statistics).

## Requirements

- Windows 10 or 11. Linux and Steam Deck (Wine) are untested.
- Monster Girl Quest! Paradox RPG with the **English translation** installed (the `Patch` folder must exist).
- The Discord **desktop app**, with *Settings → Activity Privacy → Share your detected activities* turned on.

Nothing else needs to be installed.

## Installation

1. Download `MGQ-Paradox-Discord-RPC-x.y.z.zip` from the [latest release](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/releases/latest).
2. Extract it **into your game folder**, the one that contains `Game.exe`:
   ```
   Game.exe
   DiscordPatcher.bat    <- new
   Discord\              <- new
   Patch\
   ...
   ```
3. Close the game, double-click **`DiscordPatcher.bat`** and choose **Yes**.
4. Start the game as usual.

> [!NOTE]
> Windows may warn that it protected your PC, because the setup is new and not signed. Click **More info → Run anyway**. The setup only edits `Patch\Patch.rb`, as described [below](#what-it-changes), and its source code is in this repository.

### After updating the translation

A translation update replaces `Patch\Patch.rb` and with it the mod's loader. The game keeps working, but your Discord status stops showing. Double-click **`DiscordPatcher.bat`** again and choose **Yes**.

### Uninstall

Double-click **`DiscordPatcher.bat`** and choose **No** (= Uninstall). Then delete the `Discord` folder and `DiscordPatcher.bat`.

## What it changes

Only **one** file outside its own folder: a small, clearly marked loader is added to the end of `Patch\Patch.rb`, and the checksum in that file's first line is updated so the game accepts it.

- Your original file is kept as `Patch\Patch.rb.backup`, and uninstalling restores it exactly.
- The loader works with whatever version of the translation you have. No translation file is ever replaced.
- If you delete the `Discord` folder without uninstalling, the game still starts normally. The loader then simply does nothing.
- **Your save files are never changed**, and they load the same with or without the mod.

## Per-save statistics

The trivia comes from two places:

| Read from your save | Counted by the mod |
|---|---|
| Battles fought, gold carried, playtime, difficulty, companions recruited, deepest Labyrinth of Chaos floor, your Ilias or Alice choice, top stat, mastered jobs and races | Enemies defeated, escapes, wipeouts, items synthesized, gold spent in shops, biggest hit |
| Covers your **whole playthrough** and shows up right away, even on old saves. | Covers only the time **since you installed the mod**. |

The game counts the second group only **across all saves combined**. To show them per playthrough, the mod counts the same events again for each save:

- **When counting starts:** from the first time you save with the mod installed. Until then, these counters are 0, and a line stays hidden until it has counted something. Earlier numbers can't be recovered.
- **Where they're stored:** in `Discord\Stats\`, one small file per save slot plus one for the autosave. Never inside the save file itself.
- **Copied or replaced saves:** each stats file is tied to one exact save. A save replaced or copied outside the game starts over at 0.
- **The game's backup save** (`SaveBackup.rvdata2`) isn't tracked. Loading it starts these counters at 0.

## Troubleshooting

- **Nothing shows on Discord:** make sure activity sharing is turned on (see [Requirements](#requirements)). Discord can be started before or after the game; the status appears within about 15 seconds. If it still doesn't, check `Discord\DiscordPresence.log`.
- **The status updates slowly:** Discord allows about one update every 15 seconds. The game also pauses while its window is in the background, so the status doesn't change then.
- **`Discord\InGame.log` exists:** it only appears when something went wrong inside the game. Please attach it when [opening an issue](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/issues).

## Building from source

Publishing a release on GitHub builds the mod and attaches the zip automatically.

To build locally, you need the .NET 10 SDK and the Visual Studio workload *Desktop development with C++*:

```powershell
dotnet publish MGQParadox.DiscordPresence -c Release
```

This builds the release package and zips it. See [docs/DEVELOPER.md](docs/DEVELOPER.md) for the code layout, how the mod hooks into the game, and which game data it reads.

## Disclaimer

This is an unofficial fan project. It is not affiliated with Torotoro Resistance, the creators of Monster Girl Quest, or the English translation team. It contains no game or translation files.
