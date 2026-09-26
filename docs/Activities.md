# Activities

Everything the presence shows on a Discord profile, by the release that added it. Newest first.

A Discord profile has four places to fill:

- **Line 1** says where you are and what you're doing. Built in `ActivityBuilder.cs`.
- **Line 2** shows one trivia line at a time, rotating. Written in `Trivia` in `Discord_RPC.rb`.
- **Tooltip** appears when someone hovers the picture. Built in `ActivityBuilder.cs`.
- **Time** shows how long the game has been running.

When you add or change an activity, add it under the version it will ship in.

## 1.3.1

| Change | Before | Now |
|---|---|---|
| Labyrinth of Chaos Carnage | Always shown as *Normal*, with the floor of the last Normal run | Shown as *Carnage*, with the Carnage run's floor |
| Defeat scene (NSFW) on 2.x | Never shown or counted | Shown and counted like on 3.x |

### Picture

| Activity | Picture | When |
|---|---|---|
| Story picture | Ilias or Alice / the route: Monster Realm, Angelic Dominion or Chaos | With the new Picture option on *Dynamic* (default *Static*: the app icon). Ilias or Alice, whoever this playthrough chose; in the final chapter the route, taken from the map you are on. On maps all routes share, like the Pocket Castle, the Chaos route once it is open, otherwise the route seen last. The app icon before the choice and at the title screen. |

## 1.3

The Statistics option also switches *Battlefucks won* between the game's counts across all saves (default) and per save.

### Line 1

| Activity | Text | When |
|---|---|---|
| Camp | `<Location> - Setting up for Camp . . .` / `Traveling the world - Setting up for Camp . . .` | While the camp music (*Camping*) plays, except in battles, the Pocket Castle and the Labyrinth of Chaos. Shown instead of idle. |
| Conversation | `<Location> - Talking to <Name> . . .` / `Pocket Castle - ...` / `Traveling the world - ...` | From the first line someone other than Luka speaks until the event or story scene is over, named after the last one who spoke. Not in battles or the Labyrinth of Chaos. Camp comes first; shown instead of idle. |
| Battle fuck (NSFW) | `<Location> - Currently Battlefucking <Battlefucker>!` | Between Luka and a Battlefucker, from accepting a battle fuck until it is over, the scene after a win included. Replays in the Recollection Room don't show. |

### Trivia

| Activity | Text | When |
|---|---|---|
| Most affection | `Most affection with <Companion> (<N>), <Companion> (<N>) and <Companion> (<N>)!` | The three recruited companions with the most affection, fewer while fewer have any. Affection is the game's own, shared by all saves. Comes after *Recruited members*. |
| Battlefucks won (NSFW) | `Has won <N> battlefuck(s)!` | From the first battle fuck won, counted by the game itself: across all saves, or per save with the Statistics option on *This save*. Comes after *Most raped by*. |

## 1.2

*NSFW* activities only show with the mod's NSFW option on, which is off by default. It is in the Mod Config Menu when that is installed, in the game's Config menu otherwise.

The Statistics option, next to it, switches *Enemies defeated*, *Battles escaped*, *Wipeouts*, *Biggest hit*, *Gold spent* and *Items synthesized* between the game's own counts across all saves (default) and the mod's per-save counts.

### Line 1

| Activity | Text | When |
|---|---|---|
| Idle | `<Location> - Idle . . .` / `Traveling the world - Idle . . .` | No button pressed for 1 minute, also while the game is in the background. Never in battles, the Pocket Castle, the Labyrinth of Chaos or at the title screen. |
| Request (NSFW) | `Pocket Castle - In a request with <Companion> for the <Nth> time!` / `<Location> - ...` | While a request plays, in the Pocket Castle or aboard the MS Fish. Replays in the Recollection Room don't count. The count is per save and includes the running request. |
| Defeat scene (NSFW) | `<Location> - Raped by <Monster girl> for the <Nth> time!` / `Traveling the world - ...` | While the scene after a lost battle plays, named after the monster girl who won. Skipped scenes, replays from the encyclopedia or the Recollection Room, and Labyrinth of Chaos defeats don't count. The count is per save and includes the running scene. |

### Trivia

| Activity | Text | When |
|---|---|---|
| Music playing | `Currently vibing to <Track>!` | While music plays that the jukebox's music room names (227 of the 234 tracks). Comes after *Last item used*. |
| Requests made (NSFW) | `Has made <N> request(s)!` | Per save, from the first request. Comes after *Deepest floor*. |
| Most requested (NSFW) | `Has requested <Companion> the most, <N> time(s)!` | Per save, from the first request. Comes after *Requests made*. |
| Times raped (NSFW) | `Has been raped <N> time(s)!` | Per save, from the first defeat scene. Comes after *Most requested*. |
| Most raped by (NSFW) | `Raped by <Monster girl> the most, <N> time(s)!` | Per save, from the first defeat scene. Comes after *Times raped*. |

## 1.1.1

| Change | Before | Now |
|---|---|---|
| Update speed | At most every 15 seconds | Every 4 seconds, Discord's full limit of 5 updates per 20 seconds |
| Trivia rotation | Every 15 seconds | Every 16 seconds, every 4th update |

## 1.1.0

No new activities. This release changed how the mod is installed.

## 1.0

### Line 1

| Activity | Text | When |
|---|---|---|
| Title screen | `At the title screen` | At the title screen. Line 2 and the tooltip stay empty. |
| Exploring | `<Location> - Exploring . . .` | On a map |
| Combat | `<Location> - In combat!` / `Traveling the world - In combat!` | In a battle, the second on the world map |
| Menu | `<Location> - In menu . . .` | In any other screen, shops and the save screen included |
| World map | `Traveling the world - On foot . . .` / `Sailing . . .` / `Flying . . .` | On the world map, by vehicle |
| Pocket Castle | `Pocket Castle - <line>` | In the Pocket Castle, except in battle. One of seven lines, rotating with the trivia from a random start: *Not knowing what to do next...*, *Rearranging the party...*, *Wondering how to build a character...*, *Currently in crafting hell...*, *Trying to buy out Vanilla's stock...*, *Befriending some Companions...*, *Getting lost...* |
| Labyrinth of Chaos | `Labyrinth of Chaos <Normal/Carnage> (<Biome>) - Floor <X> \| <Y> Rare Points!` | Inside the labyrinth. The biome is left out at the entrance. Carnage was always shown as Normal until 1.3.1. |

### Tooltip

| Activity | Text | When |
|---|---|---|
| Party leader | `Party leader: <Name> Lv <X> \| Race: <Race> Lv <Y> \| Class: <Job> Lv <Z>` | Once a save is loaded. Unknown parts are left out. |

### Time

| Activity | Text | When |
|---|---|---|
| Elapsed time | `<mm:ss> elapsed` (Discord's own format) | Always, counted from the game's start |

### Trivia

Rotating in this order. A line that doesn't apply is skipped. *Per save* means the mod counts it itself, starting when it was installed.

| Activity | Text | When |
|---|---|---|
| Dead party members | `Currently has <N> dead party member(s)!` | Someone in the active party is down |
| Recruited members | `Has recruited <N> party members this playthrough!` | Always. Luka isn't counted. |
| Battles fought | `Has fought <N> battles!` | Always |
| Difficulty | `Currently playing on <Difficulty> - <comment>!` | Always, with one comment per difficulty from Very Easy to Paradox |
| Playtime | `Is <N> hours in. <comment>!` / `Is less than an hour in. <comment>!` | Always, with a comment that changes at 0, 10, 25, 50, 100, 200, 400, 700 and 1000 hours |
| Chosen side | `Has chosen Ilias this playthrough!` / `Has chosen Alice this playthrough!` | Once the choice is made |
| Last item used | `Just used <Item> on <Target>!` | For 90 seconds after using an item |
| Top master | `<Name> has mastered <N> Jobs and <M> Races already!` | Someone in the active party mastered a job or race. Takes turns among the top three every 45 seconds. |
| Enemies defeated | `Has defeated <N> enemies!` | Per save, from the first defeat |
| Battles escaped | `Has run away from <N> battles!` | Per save, from the first escape |
| Wipeouts | `Has been wiped out <N> times!` | Per save, from the first wipeout |
| Top stat | `<Name> has the highest <Stat> (<value>) in the party!` | Always. A different stat every 45 seconds. |
| Biggest hit | `Biggest hit dealt: <X> damage!` | Per save, from the first hit. Large numbers are shortened like in the game. |
| Gold spent | `Has spent <N> gold in shops!` | Per save, from the first purchase |
| Items synthesized | `Has synthesized <N> items!` | Per save, from the first synthesis |
| Deepest floor | `Has reached floor <N> in the Labyrinth of Chaos!` | Once the labyrinth was entered |
| Gold carried | `Currently carrying <N> gold!` | Always |
