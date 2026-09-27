# Activities

Everything the presence shows on a Discord profile, by the release that added it. Newest first.

A Discord profile has four places to fill:

- **Line 1** says where you are and what you're doing. Built in `ActivityBuilder.cs`.
- **Line 2** shows one trivia line at a time, rotating. Written in `Trivia` in `Discord_RPC.rb`.
- **Tooltip** appears when someone hovers the picture. Built in `ActivityBuilder.cs`.
- **Time** shows how long the game has been running.

When you add or change an activity, add it under the version it will ship in.

## 1.4.0

Trivia lines now belong to a group: *general* lines rotate throughout; a part's lines (Part 1, 2 or 3) only rotate while it is played, after the general ones; a Part 3 route's lines (Destroyer, Judgment, Chaos) only rotate while it is played, after those. Every earlier line is general.

The new **Spoilers** option (default *Hide*, under `[Discord] Rich Presence`) protects viewers who haven't played Part 3. While Part 3 is played with it on *Hide*, spoiler trivia is left out and the changes below apply. Parts 1 and 2 are never affected.

### Line 1

| Activity | Before | With spoilers hidden in Part 3 |
|---|---|---|
| Location | `<Location> - Exploring . . .` and every other line that names the map | The map name is left out: `Exploring . . .`. The world map, the Pocket Castle and the Labyrinth of Chaos still show as usual. |
| Conversation | `<Location> - Talking to <Name> . . .` | `Talking to someone . . .` |
| Request (NSFW) | `… In a request with <Companion> for the <Nth> time!` | `… In a request with a companion for the <Nth> time!` |
| Defeat scene (NSFW) | `… Raped by <Monster girl> for the <Nth> time!` | `… Raped by a monster girl for the <Nth> time!` |
| Battle fuck (NSFW) | `… Currently Battlefucking <Battlefucker>!` | `… Currently Battlefucking a battlefucker!` |

### Options

| Change | Before | Now |
|---|---|---|
| Image options | `Picture`, `-> Shown Picture` | `Activity Image`, `-> Shown Image`; the Settings.ini keys stay `picture` and `shown_picture` |

### Buttons

| Button | Link | When |
|---|---|---|
| `Get the mod` | The mod's GitHub page, with its requirements, installation and download | Always, the title screen included. Discord shows buttons to everyone but the player. |

### Tooltip

| Activity | Before | With spoilers hidden in Part 3 |
|---|---|---|
| Route | `Part 3: Destroyer route` / `Judgment route` / `Chaos route` | `Part 3: Monster route` / `Angel route` / `Third route` |

### Trivia

| Activity | Text | When |
|---|---|---|
| Spirits recruited (Parts 1 and 2) | `Has recruited <X> out of 4 spirits!` | In Parts 1 and 2, from the first of Sylph, Gnome, Undine and Salamander in the party. |
| Forging ore (Parts 1 and 2) | `Has unlocked <Ore> for forging!` | In Parts 1 and 2, the best ore held: Lump of Iron, Gold Ore, Mithril Ore, Crystal, Dragon Scale Fossil, Orichalcum, Rainbow Crystal, or Meteorite (3.x). Comes after *Spirits recruited*. |
| Naval side (Part 2) | `Sided with the <Pirates/Marines> this playthrough!` | In Part 2, once Luka has aided the Fishy Pirates or boarded the naval vessel at the Navy Headquarters. Comes after *Forging ore*. |
| Monster queens recruited (Part 2) | `Has recruited <X> out of 14 monster queens!` | In Part 2, from the first of the Cow Demon Queen, Miria, Antine Ann, Poseidoness, Candy, Airy, Freya, Alrauna, Lucretia, Laura, Kraken, the Spider Princess, Fatima, and Lilith & Lilim in the party. Comes after *Naval side*. |
| Routes cleared (Part 3) | `Has cleared <X> out of 3 routes!` | In Part 3 on 3.x, from the first route cleared (Destroyer, Judgment or Chaos). Comes before *Randolphs found*. |
| Randolphs found (Part 3, spoiler) | `Has found <X> out of <Y> Randolphs!` | In Part 3 on 3.x, from the first Randolph found. Hidden while spoilers are hidden. |
| Phenomena of Ruin defeated (Chaos route, spoiler) | `<X> out of 16 Phenomena of Ruin have been defeated!` | On the Chaos route, from the first Phenomenon of Ruin defeated, counted like the game's own "Remaining Phenomena of Ruin" message. Hidden while spoilers are hidden. |

## 1.3.5

| Change | Before | Now |
|---|---|---|
| Labyrinth of Chaos Carnage | Always shown as *Normal*, with the floor of the last Normal run | Shown as *Carnage*, with the Carnage run's floor |
| Defeat scene (NSFW) on 2.x | Never shown or counted | Shown and counted like on 3.x |
| Tooltip | `Party leader: <Name> Lv <X> \| Race: <Race> Lv <Y> \| Class: <Job> Lv <Z>` | `Part <N>: <Ilias/Alice> side \| Party leader: …`, in Part 3 `Part 3: <Judgment/Destroyer/Chaos> route \| …` once the route is chosen, and `Collaboration Scenario: Act <N> \| …` without the part while the Collaboration Scenario is played. Part 1 ends with the escape from Tartarus, Part 2 with the Great Decision. Over 128 characters, the labels *Party leader*, *Race* and *Class* are left out. |
| Options | `[Discord] NSFW`, `[Discord] Statistics` and `[Discord] Picture` side by side | Layered like EXP Overlord: `[Discord] Rich Presence` (On/Off) first, with `NSFW`, `Statistics` and `Picture` indented below it. Off clears the Discord status. New below `Picture`, each shown only while it applies: `-> Shown Picture` while `Picture` is *Static*, every picture of the application (*Default*, the app icon; Ilias and Alice adult or sealed; the Judgment and Destroyer logos alone or layered; Chaos; Collaboration Scenario); `-> Ilias / Alice` (*Sealed*/*Adult*) and `-> Routes` (*Layered*/*Logo*) while it is *Dynamic*. Options under a greyed out one leave the menu. |
| Chosen side (trivia) | `Has chosen Ilias this playthrough!` / `Has chosen Alice this playthrough!` | Removed, the tooltip shows the side all the time |

### Picture

| Activity | Picture | When |
|---|---|---|
| Story picture | Ilias or Alice / the route: Judgment, Destroyer or Chaos / the collab's heroes | With the new Picture option on *Dynamic* (default *Static*: the app icon). Ilias or Alice, whoever this playthrough chose; in the final chapter the route, from the moment it is chosen at the Great Decision; the collab picture during the Collaboration Scenario. The app icon before the choice and at the title screen. |

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
