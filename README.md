# Monster Girl Quest! Paradox RPG – Discord Rich Presence

Shows what you're doing in **Monster Girl Quest! Paradox RPG** on your Discord profile: where you are, what you're up to, and rotating trivia about your playthrough.

![Discord profile showing the party leader tooltip, the Pocket Castle and a trivia line](docs/images/presence.png)

**[Download the latest release](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/releases/latest)**

## Features

| Where you are | What Discord shows |
|---|---|
| Anywhere | `<Location> - Exploring . . .` / `In combat!` / `In menu . . .` |
| In a shop, the forge, the casino and other menu screens | `<Location> - Shopping . . .` / `Synthesizing . . .` / `Playing poker at the casino . . .` and more |
| World map | `Traveling the world - On foot . . .` / `Sailing . . .` / `Flying . . .` |
| Pocket Castle | `Pocket Castle - Currently in crafting hell...` and other rotating lines |
| Setting up camp | `<Location> - Setting up for Camp . . .` |
| In a conversation | `<Location> - Talking to <Name> . . .` |
| No button pressed for a minute | `<Location> - Idle . . .`, except in battles, at camp, in conversations, the Pocket Castle and the Labyrinth of Chaos |
| Labyrinth of Chaos | `Labyrinth of Chaos <Normal/Carnage> (<Biome>) - Floor <X> \| <Y> Rare Points!` |
| In a request (NSFW) | `Pocket Castle - In a request with <Companion> for the <Nth> time!` |
| After losing a battle (NSFW) | `<Location> - Raped by <Monster girl> for the <Nth> time!` |
| In a battle fuck (NSFW) | `<Location> - Currently Battlefucking <Battlefucker>!` |

**Hover the picture** to see where your story stands and your party leader: `Part <N>: <Ilias/Alice> side | Party leader: <Name> Lv <X> | Race: <Race> Lv <Y> | Class: <Job> Lv <Z>`, with the route instead of the side in Part 3 (`Part 3: Chaos route | …`), and the act instead of the part during the Collaboration Scenario (`Collaboration Scenario: Act 5 | …`).

**The picture** is the game's icon, or if you [choose so](#options) Ilias or Alice, whoever you picked, later the route you are on, and the collab's heroes during the Collaboration Scenario.

**A `Get the mod` button** under your status links your friends to this page. Discord doesn't show it to you, only to others, and leaves it out while your status offers invites.

**Trivia** rotates every 16 seconds:

- Battles fought, enemies defeated, escapes and wipeouts
- Difficulty and playtime, each with a comment
- The biggest hit dealt, shortened like in the game (`118.497Qnt.`)
- Who in your party has the highest stat, and who has mastered the most jobs and races
- The last item you used, and on whom
- The music that's playing, named like in Kagetsumugi's jukebox
- The three companions with the most affection
- Gold carried and spent, items synthesized, companions recruited, the deepest Labyrinth of Chaos floor, and more
- How many of the game's medals you earned
- In Parts 1 and 2: how many of the four spirits you recruited, and the latest ore you unlocked for forging; in Part 2 also whether you sided with the pirates or the marines, and how many monster queens you recruited
- In Part 3 (3.x): how many routes you cleared, how many Randolphs you found, and on the Chaos route how many of the 16 Phenomena of Ruin you defeated (spoilers, see [Options](#options))
- How many requests you made, and to whom the most (NSFW)
- How often you were raped after losing, and by whom the most (NSFW)
- How many battle fucks you won (NSFW)

Defeats, escapes, wipeouts, syntheses, gold spent, the biggest hit and battle fucks won count across all your saves, or only the save you're playing if you [choose so](#options). Affection and medals are the game's own, which all your saves share. Everything else belongs to the save you're playing. See [Per-save statistics](#per-save-statistics).

**NSFW** lines only show when you turn them on, see [Options](#options).

Every line, its exact text, when it shows and the release that added it: [Activities](docs/Activities.md).

## Requirements

- Windows 10 or 11. Linux and Steam Deck (Wine) are untested.
- Monster Girl Quest! Paradox RPG **2.x or 3.x** (tested on 2.x and 3.06), with or without the **English translation**.
- The community's **mod loader**: the `Patch.rb` from [*Patch.rb (enable Type 1 mods)*](https://mgq.miraheze.org/wiki/Paradox_mods#Patch.rb_(enable_Type_1_mods)) on the MGQ wiki, put into the `Patch` folder. If you already use other Patch folder mods, you have it.
- The Discord **desktop app**, with *Settings → Activity Privacy → Share your detected activities* turned on.

Nothing else needs to be installed.

## Installation

1. Download `MGQ-Paradox-Discord-RPC-x.y.z.zip` from the [latest release](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/releases/latest).
2. Close the game and extract the zip **into your game folder**, the one that contains `Game.exe`:
   ```
   Game.exe
   Discord\                 <- new, with Update.bat for later updates
   Patch\Discord_RPC.rb     <- new
   ...
   ```
3. Start the game as usual.

**Updating:** the title screen tells you when a new release is out (see [Update Check](#options)). Close the game and double-click `Discord\Update.bat`: it downloads the latest release, installs it and keeps your options. Versions before 1.4.0 have no `Update.bat` yet, so update to 1.4.0 by hand.

To update by hand, close the game and extract the new zip over the old one. That resets your options. Versions before 1.3 also left `Discord\Uninstall.exe`, which is no longer used; you can delete it, and `Update.bat` does so.

### Options

The mod adds its options to the game: in a mod config menu when you have one installed, in the game's own Config menu otherwise. For the translation's tabbed options screen, which the community's Mod Config Menu (`Patch\0_ModConfigMenu.rb`) no longer works with, there is the [Mod Config Remake](https://github.com/Pauliinchen/MGQ-Paradox-Mod-Collection/tree/main/0_ModConfigRemake). The options are grouped under **`[Discord] Rich Presence`**; the ones below it are indented, and greyed out in a mod config menu while it is off.

**`[Discord] Rich Presence`**
- **On** (default): Discord shows what you're doing in the game.
- **Off**: Discord shows nothing about the game, without uninstalling the mod. The options below keep their values.

**`NSFW`**
- **Off** (default): Discord never mentions requests, defeat scenes or battle fucks.
- **On**: Discord shows a request, defeat scene or battle fuck while it plays, and their counters in the trivia. Requests and defeat scenes are counted either way, so turning it on later shows the full count since you installed the mod.

**`Statistics`**, for enemies defeated, escapes, wipeouts, items synthesized, gold spent, the biggest hit and battle fucks won:
- **All saves** (default): the game's own counts, across every save and from before you installed the mod.
- **This save**: counted by the mod for the save you're playing, see [Per-save statistics](#per-save-statistics). Battle fucks won are the game's own count for the save.

**`Spoilers`**, for your friends who haven't played Part 3 yet:
- **Hide** (default): while you play Part 3, Discord leaves out the trivia that spoils it, leaves the map name off the first line (the world map, Pocket Castle and Labyrinth of Chaos still show), names nobody there (`Talking to someone . . .`, `In a request with a companion …`, `Raped by a monster girl …`, `Currently Battlefucking a battlefucker!`) and calls the routes `Monster route` (Destroyer), `Angel route` (Judgment) and `Third route` (Chaos) in the tooltip. Parts 1 and 2 show as usual.
- **Show**: Discord shows everything, Part 3 included.

**`Activity Image`**
- **Static** (default): always the same picture, the one picked under **`-> Shown Image`**: **Default** (the game's icon), Ilias or Alice in her adult form or sealed, the Judgment or Destroyer route's logo alone or over its heroines, Chaos or Collaboration Scenario.
- **Dynamic**: Ilias or Alice, whoever you picked this playthrough, in the final chapter the route you are on, and the collab's heroes during the Collaboration Scenario. Before you pick, it stays the game's icon. Two options appear under it instead of Shown Image:
  - **`-> Ilias / Alice`**: **Sealed** (default) or **Adult**.
  - **`-> Routes`**: **Layered** (default), the Judgment or Destroyer route's logo over its heroines, or **Logo**, the logo alone.

**`Update Check`**
- **On** (default): once per game start, the game asks GitHub for the mod's latest release, and the title screen tells you when it is newer than yours. Nothing else is sent.
- **Off**: the game never asks GitHub.

The options under `Activity Image` only show while they apply. The options are the same for every save. They are kept as `presence`, `nsfw`, `all_saves`, `spoilers`, `picture`, `shown_picture`, `sealed_sides`, `layered_routes` and `update_check` in `Discord\Settings.ini`, not in your save files. `Update.bat` keeps them, extracting a new zip by hand resets them.

### Playing with a friend

With [Monster Girl Quest! Online](https://github.com/Pauliinchen/MGQ-Online) installed as well, you can invite a friend through the **+** in a Discord chat (*Invite to play*) and accept their invites, and your status shows `Waiting for a friend (1 of 2)` while you host and `Playing with <Friend> (2 of 2)` while you play together. The `Get the mod` button leaves your profile while you host. When you accept an invite, a box on the title screen keeps you waiting until Discord hands it over. An invite ends when your friend stops hosting; the box then says so. In one of its worlds, the second line shows `Playing on World <World> (3 of 8)` instead of the trivia, taking turns with `Currently in a Party! (2 of 4)` while you play in a party, and friends can be invited into the world the same way, password included; the `Get the mod` button leaves your profile there too.

### After updating the translation

A translation update replaces `Patch\Patch.rb` and with it the mod loader, so no Patch folder mod is loaded any more. The game keeps working, but your Discord status stops showing. Put the community's `Patch.rb` back into the `Patch` folder.

### Uninstall

Close the game and delete `Patch\Discord_RPC.rb` and the `Discord` folder. The mod loader stays for your other mods.

## What it changes

The mod is a regular **Patch folder mod** ("Type 1"), `Patch\Discord_RPC.rb`, like the community's mods on the [MGQ wiki](https://mgq.miraheze.org/wiki/Paradox_mods). The community's mod loader runs it, and it works next to the other mods.

- **Only its own files** (`Discord\` and `Patch\Discord_RPC.rb`) **and your saves** are changed. `Patch\Patch.rb` is never touched.
- **Uninstalling** is deleting those files. The mod loader stays for your other mods.
- If you delete only the `Discord` folder, the game still starts normally, and the mod simply does nothing.
- **Your saves** get the mod's [per-save counters](#per-save-statistics) added, and nothing else. They load the same with or without the mod; without it, the game ignores the counters.

## Per-save statistics

The trivia comes from two places:

| Read from your save | Counted by the mod |
|---|---|
| Battles fought, gold carried, playtime, difficulty, companions recruited, deepest Labyrinth of Chaos floor, top stat, mastered jobs and races, battle fucks won | Enemies defeated, escapes, wipeouts, items synthesized, gold spent in shops, biggest hit, requests, defeat scenes |
| Covers your **whole playthrough** and shows up right away, even on old saves. | Covers only the time **since you installed the mod**. |

The game counts most of the second group only **across all saves combined**. To show them per playthrough, the mod counts these events itself for each save (the [Statistics option](#options) shows the game's counts across all saves instead):

- **When counting starts:** as soon as you play with the mod installed. Until a counter has counted something, its line stays hidden. Earlier numbers can't be recovered.
- **Where they're stored:** inside your save, like everything else the game keeps. Saving keeps them, and copied saves, the autosave and the game's backup save carry them along.
- **Earlier versions** kept these counters in `Discord\Stats\`. Loading a save takes its numbers over once; after you've saved each of your saves, you can delete that folder.

## Troubleshooting

- **Nothing shows on Discord:** make sure the mod loader is installed and activity sharing is turned on (see [Requirements](#requirements)). Discord can be started before or after the game; the status appears within about 15 seconds. If it still doesn't, check `Logs\DiscordPresence.log` in the game folder.
- **The status updates slowly:** Discord allows 5 updates per 20 seconds, so a new map or fight shows up within about 4 seconds. The game also pauses while its window is in the background, so the status doesn't change then, unless another mod such as Monster Girl Quest! Online keeps it running.
- **`Logs\Discord InGame.log` exists:** it only appears when something went wrong inside the game. Please attach it when [opening an issue](https://github.com/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/issues).

## Building from source

Publishing a release on GitHub builds the mod and attaches the zip automatically.

To build locally, you need the .NET 10 SDK and the Visual Studio workload *Desktop development with C++*:

```powershell
dotnet publish MGQParadox.DiscordPresence -c Release
```

This puts the finished mod into the `Shipping` folder: copy its content into your game folder, as in the [installation](#installation). It also zips it. See [docs/DEVELOPER.md](docs/DEVELOPER.md) for the code layout, how the mod hooks into the game, and which game data it reads.

## Credits

- The mod loader this mod runs in is the community's `Patch.rb` from the [MGQ wiki](https://mgq.miraheze.org/wiki/Paradox_mods#Patch.rb_(enable_Type_1_mods)). It isn't part of this repository or its releases; please download it from there.

## Disclaimer

This is an unofficial fan project. It is not affiliated with Torotoro Resistance, the creators of Monster Girl Quest, or the English translation team. It contains no game or translation files.
