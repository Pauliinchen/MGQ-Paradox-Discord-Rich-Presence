# Activities

Everything the presence shows on a Discord profile, by the release that added it. Newest first.

A Discord profile has four places to fill:

- **Line 1** says where you are and what you're doing. Built in `ActivityBuilder.cs`.
- **Line 2** shows one trivia line at a time, rotating. Written in `Trivia` in `Discord_RPC.rb`.
- **Tooltip** appears when someone hovers the picture. Built in `ActivityBuilder.cs`.
- **Time** shows how long the game has been running.

When you add or change an activity, add it under the version it will ship in.

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
| Labyrinth of Chaos | `Labyrinth of Chaos <Normal/Carnage> (<Biome>) - Floor <X> \| <Y> Rare Points!` | Inside the labyrinth. The biome is left out at the entrance. Carnage detection is untested. |

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
