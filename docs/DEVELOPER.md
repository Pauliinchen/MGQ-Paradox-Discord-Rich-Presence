# Developer notes

## Layout

```
MGQ-Paradox-Discord-Rich-Presence.slnx   Visual Studio solution
Directory.Build.targets                  puts vswhere.exe on the PATH, which the NativeAOT link needs
GameScript/Discord_RPC.rb                Ruby, runs inside the game
MGQParadox.DiscordPresence/              C# NativeAOT project -> DiscordPresence.dll, and the package
MGQParadox.DiscordPresence.Tests/        xUnit tests of the DLL, 32-bit like it
package/                                 static files shipped as-is
Shipping/                                publish output, git-ignored
docs/DEVELOPER.md                        this file
docs/Activities.md                       every line Discord shows, by the release that added it
.github/workflows/release.yml            tests, builds and attaches the zip on release
```

**`GameScript/Discord_RPC.rb`:** a Patch folder mod, shipped as `Patch/Discord_RPC.rb` and loaded by the mod loader in `Patch.rb`. It collects the game state and hands it to `DiscordPresence.dll` as plain values; it writes no text Discord shows, the DLL does. Everything lives in the module `MGQ_Discord`, which holds the settings and the start-up, tick and publish steps. Nested modules do one job each:

| Module | What it is |
|---|---|
| `Log` | `Logs/Discord InGame.log` in the game folder, capped per session. |
| `Options` | The mod's options in the game's menus, see [Options](#options). |
| `NumberFormat` | The game's large-number style (`give_unit`), which only the game can write. |
| `GameState` | Everything read from the game: scene, area, vehicle, Labyrinth of Chaos, mastery. |
| `Requests` | Which novel scene is a request, and who plays it. |
| `DefeatScenes` | The scene after a lost battle, and which monster girl won. |
| `Battlefucks` | A running battle fuck, and which battlefucker Luka faces. |
| `Conversations` | Who the player is talking to, from the name boxes of the messages, or the untranslated game's speaker lines. |
| `Story` | Where the story stands: the part, the side chosen and the route of the final chapter, which the tooltip and the Activity Image option show, and whether Part 3 spoilers are hidden (`hides_spoilers?`). |
| `Trivia` | The values behind the second Discord line. Each reader in `READERS` returns its values by key; a failing reader drops only its own. Every value is published whatever the part, route or options, since `TriviaBuilder.cs` decides which lines apply. Read at most every 5 s. |
| `Statistics` | The counts the trivia shows: `SaveStats`, or the game's own across all saves when the Statistics option says so. |
| `SaveStats` | The per-save counters, see [Per-save statistics](#per-save-statistics-savestats). |
| `StatusText` | The `key=value` status. The keys are shared with `Game/GameStatus.cs`. |
| `Bridge` | What other mods hand the presence and take from it, see [Other mods](#other-mods). |
| `Presence` | Calls `DiscordPresence.dll` through `Win32API`. |

The game hooks follow the module at the end of the file. `MGQ_Discord.hookable?` guards them, see [Earlier versions](#earlier-versions).

**`MGQParadox.DiscordPresence/`** builds `DiscordPresence.dll`: C# compiled with NativeAOT into a native 32-bit DLL, because the game is a 32-bit process. Players need no .NET installed. Namespaces follow the folders:

| Path | What it is |
|---|---|
| `Exports.cs` | The functions `Discord_RPC.rb` calls: `presence_start`, `presence_update`, `presence_check_for_update`, `presence_newer_version` and `presence_invite_state`; and for `Bridge`, `presence_set_connection`, `presence_take_invite` and `presence_player_name`. Nothing may throw out of them. |
| `UpdateCheck.cs` | Asks GitHub's API for the latest release once per session, on its own thread, and compares it with the DLL's version. Development builds (`0.0.0-dev`) never ask. |
| `PresenceLoop.cs` | The DLL's own thread: rate limit, trivia rotation, reconnecting to Discord. Holds the Discord application's id (`ClientId`), which players cannot change. |
| `ActivityBuilder.cs` | Builds the Discord activity: the first line, the tooltip, the picture and the buttons. **All user-visible texts live here and in `TriviaBuilder.cs`.** New or changed ones also go into [Activities.md](Activities.md) under the version they ship in. |
| `TriviaBuilder.cs` | Writes the trivia, one method per line. `Lines` lists them in rotation order, each with the parts (`Parts`) or the route (`Route`) it belongs to; a line without either shows throughout. `IsSpoiler` lines stay out while spoilers are hidden, `IsNsfw` lines while the NSFW option is off. |
| `NumberFormat.cs` | Thousands separators, counted nouns and ordinals. |
| `ModFolder.cs` | The `Discord` folder, which the game script finds the DLL in. |
| `Log.cs` | `DiscordPresence.log` in the game folder's `Logs` folder, one above it. |
| `NativeMethods.cs` | Lets the DLL find its own folder. |
| `Game/GameStatus.cs` | One parsed status, a property per value. `Scene.cs` and `Vehicle.cs` hold its enums. |
| `Discord/DiscordIpcClient.cs` | Discord's local named-pipe protocol. `Opcode.cs` holds the frame kinds. |
| `Discord/Activity.cs`, `Discord/ActivityButton.cs`, `Discord/ActivityParty.cs`, `Discord/Json.cs` | The activity, its link buttons, the party a connection invites to and its JSON, with Discord's field limits. |
| `Discord/DiscordDispatch.cs` | The events Discord sends on its own: `READY` (the user's name), `ACTIVITY_JOIN` (an accepted invite's join secret) and `ACTIVITY_JOIN_REQUEST` (who asked to join, for the log). |
| `Discord/LaunchRegistration.cs` | Points the application's URL scheme `discord-<ClientId>` (`HKCU\Software\Classes`) at the running Game.exe, so accepting an invite starts a closed game. The command passes the link on (`"%1"`), which the RGSS player ignores. |
| `InviteLaunch.cs` | Tells whether Discord started the game for an invite and whether its join came (`InviteLaunchState`): the command line holds the `discord-<ClientId>://` link (`GetCommandLineW`, logged with a join code cut off); the invite counts as ended when no `ACTIVITY_JOIN` came within 5 s of reaching Discord (`JoinGrace`), or Discord was not reached within 20 s (`ConnectGrace`), since Discord starts the game for every invite clicked but hands over the join secret only while the host still offers it. |
| `Connection.cs`, `ConnectionKind.cs` | The connection with a friend that another mod reports, the invite the player accepted in Discord and the player's name on Discord, see [Other mods](#other-mods). |

**`package/Discord/`:** `Settings.ini` (the options `presence`, `nsfw`, `all_saves`, `spoilers`, `picture`, `shown_picture`, `sealed_sides`, `layered_routes` and `update_check`, which the game script reads and writes), the player `README.txt`, and the updater: `Update.bat` runs `Update.ps1` (Windows PowerShell 5.1), which downloads the latest release's zip, extracts it over the game folder and puts the old option values back into the new `Settings.ini`. It reads the installed version from the DLL's `ProductVersion`, so it needs no version file.

## Build and test

You need the .NET 10 SDK and the Visual Studio workload **Desktop development with C++**, whose linker NativeAOT uses.

Everything is in the projects; there is no separate build script. Publishing the DLL project assembles the complete release layout in `Shipping/` at the repository root. Copy its content into a game folder to install or update the mod there:

```
Discord/  DiscordPresence.dll  Settings.ini  README.txt  Update.bat  Update.ps1
Patch/    Discord_RPC.rb
```

- **Publish, not build:** only a publish runs NativeAOT, so a plain build gives no usable DLL.

  ```powershell
  dotnet publish MGQParadox.DiscordPresence -c Release -p:Version=1.3.0
  ```

- **Release publishes** also zip it: `bin/Release/MGQ-Paradox-Discord-RPC-<Version>.zip`.
- **Every publish replaces `Shipping/`.** Close the game before copying it over an install: `DiscordPresence.dll` is locked while the game runs.
- **Visual Studio:** open the `.slnx`. The shipped files appear in the DLL project under `Shipped`.
- **Game folder for testing:** it needs the community's mod loader (see [How it hooks in](#how-it-hooks-in)). Then every change only needs a publish, a copy and a game restart.
- **Logs:** `DiscordPresence.log` (the DLL) and `Discord InGame.log` (in-game errors), both in the game folder's `Logs` folder, which every mod writes its logs to. Set `DEBUG = true` in `Discord_RPC.rb` to log every status write.

### Tests

`MGQParadox.DiscordPresence.Tests` covers the DLL without the game or Discord: every trivia line and first line with the texts the presence sends, the tooltip, the activity's JSON and Discord's field limits, parsing the status, the number formats, how the update check tells a newer release, the Discord events, and the activity a connection that another mod reports makes. A test builds a status the way the game script publishes it (`StatusFactory.Status`) and passes a fixed time, so idle, the item window and the rotations are reproducible.

The project targets [Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/unit-testing-platform-intro) and builds into an executable, so it runs itself:

```powershell
dotnet run --project MGQParadox.DiscordPresence.Tests
```

- **32-bit:** the DLL project builds for the 32-bit game, so the tests run 32-bit too and need the x86 .NET 10 runtime (`C:\Program Files (x86)\dotnet`). The release workflow installs it before running them. When `DOTNET_ROOT` points at the x64 install, as `setup-dotnet` sets it, the tests fail to start (`hostfxr.dll … failed`) unless `DOTNET_ROOT_X86` points at the x86 one.
- **Not covered:** `Discord_RPC.rb`, which only runs inside the game, and `Update.ps1`, which needs a published release to update to. Check it there, with `DiscordPresence.log` showing what was sent.

## Conventions

- **File headers.** Every `.cs`, `.rb`, `.ps1` and `.bat` file starts with a header block listing the file name and a changelog, newest entry first. Entries by the same author on the same day are grouped. Only changes after the first commit are recorded; before that, a file carries just `Created`.
- **Documentation comments.** Every type and member is documented: XML comments in C#, YARD-style comments in Ruby. The summary is mandatory, parameters and return values whenever they apply, and remarks only where they add something. Keep them short, and always describe the current state.
- **Inline comments.** Kept to a minimum. The code has to speak for itself through its names and structure. A comment is only added where the code cannot say something itself: a constraint, a trap, or the failure a line prevents.
- **Names.** Magic strings and numbers get a named constant. Enums replace string states.
- **Language.** Code, comments and documentation are English.

## Releasing

1. Push your changes.
2. On GitHub, go to **Releases → Draft a new release**, create a tag like `v1.0.0`, write the notes, and click **Publish**.
3. The *Release* workflow runs the tests, builds the mod and attaches `MGQ-Paradox-Discord-RPC-1.0.0.zip` to that release within a few minutes. Check the **Actions** tab if it doesn't appear.
4. If the run failed, fix the cause on `main`, then start **Actions → Release → Run workflow** with the release's tag. It builds the code at that tag with the workflow from `main` and attaches the zip to the existing release, so there is no need to release again.

## How it hooks in

The game's script "パッチスクリプト読込" (module `NWPatch`) evals `Patch/Patch.rb`, but only if line 1 holds a valid checksum of the rest of the file.

The mod requires the community's mod loader, a replacement `Patch.rb` from [*Patch.rb (enable Type 1 mods)*](https://mgq.miraheze.org/wiki/Paradox_mods#Patch.rb_(enable_Type_1_mods)) on the MGQ wiki. It is not ours, so it is never shipped, copied or written. The current version evals every `.rb` file under `Patch/` (recursively, sorted, except `Patch.rb`), rescues each one and reports a failing mod in a message box. This mod is one of those files, `Patch/Discord_RPC.rb`, which is *not* checksummed, so installing or updating the mod is just extracting the zip. Translation updates overwrite `Patch.rb`; the user puts the community's `Patch.rb` back.

### Earlier versions

They appended a block (`# >>> MGQ Discord RPC` … `# <<< MGQ Discord RPC`) to `Patch.rb` that loads `Discord/rpc.rb`, and shipped `DiscordPatcher.bat` next to `Game.exe`. Their hooks use the same alias names, so loaded next to `Discord_RPC.rb`, each hook would call itself until the stack overflows.

- **At start-up** `MGQ_Discord.hookable?` deletes `Discord/rpc.rb`. The block runs *after* the mod loader, which sits above it in `Patch.rb`, so on the very first start after extracting the new zip it already finds nothing to load. The hooks are skipped (and `Discord InGame.log` says why) when `Graphics` already has `mgq_discord_update`, from another copy of this script or an earlier version, or when `rpc.rb` cannot be deleted *and* `Patch.rb` still holds the block that would load it. A leftover `rpc.rb` with no block to load it only gets a log line.
- **The block itself stays** in `Patch.rb`, with nothing left to load, until the next translation update or a fresh download of the community's `Patch.rb` replaces the file. The mod never writes `Patch.rb`, so there is no uninstaller: uninstalling is deleting `Patch/Discord_RPC.rb` and `Discord/`.

## Runtime

`Discord_RPC.rb` hooks `Graphics.update`. At start-up it calls `presence_start`, and every 30 frames it hands changed `key=value` lines to `presence_update`. Both return at once. The DLL's own thread parses the latest status and sends `SET_ACTIVITY` over `\\.\pipe\discord-ipc-N`.

- **Nothing blocks the game.** `presence_update` only stores the text; parsing, the network and the pipe all run on the DLL's thread.
- **Nothing throws into the game.** Every export and the DLL's threads catch everything; an exception escaping them would end the game.
- **No process to watch.** The DLL lives and dies with the game. When the game closes, the pipe closes and Discord clears the status by itself.
- **Reconnecting:** when Discord closes the pipe, for example on a restart, the DLL tries again every 15 s, logs the outage once, and sends the current status again once connected.
- **Invite box:** `presence_start` notes from the command line whether Discord started the game before the DLL's thread starts, so the title screen never asks too early. When `presence_invite_state` says it did, the first title screen opens `InviteBox`, which keeps the command window from taking input and follows the state every 10 frames. Once the join came, another mod takes it through `Bridge.take_invite` (Monster Girl Quest! Online loads the newest save and joins), and the box goes with the title screen; without such a mod it closes after 10 s. When the invite had ended, the box says so until the player presses a key, but still turns to the join if it comes late. Only an invite `Connection` took counts as the join.
- **Title screen load order:** while the box is open, the `Scene_Title#update` hook only updates the screen and the box and skips the methods it wraps, which includes the `Scene_Title#update` hooks of Patch files that loaded before `Discord_RPC.rb`. A mod that joins from the title screen must therefore load after it, so its hook wraps this one, as Monster Girl Quest! Online's `mp_battles_pvp.rbx` does, which its `Multiplayer.rb` loads (the loader sorts capitals first).
- **Update check:** on the title screen, with the Rich Presence and Update Check options on, the game script calls `presence_check_for_update` once and then polls `presence_newer_version` every 30 frames. Once it returns a version, `UpdateNotice` draws two lines below the translation's version, and the `Scene_Title#terminate` hook takes them off again.
- **Rate limit:** Discord accepts 5 updates per 20 s. The DLL ticks every 4 s, sends the status on a tick only if it changed, and moves the trivia on every 4th tick (16 s). Sends only happen on ticks, so the first one after a reconnect waits for the next tick too.
- **F12:** the game's reset only restarts `rgss_main`; `Patch.rb` and with it `Discord_RPC.rb` are evaluated once per start. Should the file be evaluated twice anyway, `MGQ_Discord.hookable?` keeps the hooks from wrapping themselves.

## Other mods

`MGQ_Discord::Bridge` in `Discord_RPC.rb` is all another mod sees of this one, and this mod knows no other. [Monster Girl Quest! Online](https://github.com/Pauliinchen/MGQ-Online) uses it when this mod is installed. A mod checks `Bridge::VERSION` first, which changes whenever a call's meaning changes. Every call does nothing while the presence is off or the DLL is missing (`available?`).

| Call | What it does |
|---|---|
| `hosting(party, join_secret)` | The player waits for a friend: the activity gets a party of 1 of 2, the join secret, the invite banner and `Waiting for a friend` on the second line. |
| `connected(party, friend)` | The player plays with a friend: a party of 2 of 2 and `Playing with <Friend>` on the second line. |
| `idle` | Neither; the second line shows the trivia again. |
| `take_invite` | The join secret of an invite the player accepted in Discord, handed out once, or `nil`. |
| `player_name` | The player's name on Discord, or `nil` until Discord told it. |
| `add_status { \|scene\| ... }` | Adds the Hash the block returns to every status `StatusText.build` hands the DLL. A block that raises is logged and left out. Monster Girl Quest! Online adds `pvp_battle_with` and `pvp_battle=mirror` for the first line this way, `mp_world`, `mp_world_id`, `mp_world_size`, `mp_world_max` and `mp_world_invite` (the world code, which becomes the join secret) for the world the player plays in, and `mp_party`, `mp_party_size` and `mp_party_max` for their party of two or more there. In a world, `ActivityBuilder` replaces the trivia with `Playing on World <World>` and takes turns with `Currently in a Party!` at the trivia's pace, each with its own party, which Discord shows the size of ("(3 of 8)", "(2 of 4)"). |

The DLL keeps the reported connection in `Connection` (`presence_set_connection`). A report without a party, or hosting without a join secret of at most 128 characters (Discord's limit), counts as none. `PresenceLoop.WithConnection` puts it into the activity; Discord puts the party's size behind the second line. Both games name the party the same, so Discord sees them in the same party.

After `READY` the client subscribes to `ACTIVITY_JOIN`, whose join secret waits in `Connection` until `presence_take_invite` takes it, and `ACTIVITY_JOIN_REQUEST`, which is only logged: accepting it for the player (`SEND_ACTIVITY_JOIN_INVITE`) moved a friend who hosted too into the player's party and ended the friend's hosting, so the player invites them instead. Buttons are left out while a join secret is set. The invite banner is the art asset `invite_cover` (1024 x 576), named as `assets.invite_cover_image`, since the invite image set in the Developer Portal did not reach the invites. The chat's + menu offers the invite only while the activity has the party and join secret, that is while the player hosts or plays in a world. Join secrets are cut out of the log.

## Game data used (Paradox)

| What | Source |
|---|---|
| Levels | `actor.base_level` / `class_level` / `tribe_level`; the race is `actor.tribe` |
| World map | `$game_map.overworld?` (tileset mode 0), except inside the LoC |
| Battles fought | `$game_system.battle_count` (per save) |
| Defeats, escapes, wipeouts, synthesis, gold spent, biggest hit | `SaveStats` in `Discord_RPC.rb` (per save; see below). With the Statistics option on *All saves*, `Statistics` reads `$game_library.party_defeat`, `party_escape`, `party_lose`, `party_synthesize`, `purchase_gold` and `party_damage_record_actor` instead, which are shared by all saves. |
| Difficulty | `$game_variables[NWConst::Var::CURRENT_DIFFICULTY]` (-2..4) |
| Idle | `Input.press?` on every button, published as `last_input` (Unix seconds); the DLL compares it with its own clock. Monster Girl Quest! Online, which keeps the game running in the background, has `Input` report no buttons meanwhile, so a game left in the background turns idle |
| Music playing | `RPG::BGM.last.name`, named through `NWConst::Library::BGM_SCENE_ITEMS` (the jukebox's music room, 227 of the 234 BGM files) |
| Menu screens | While `GameState.scene` is `menu`, the class name of `SceneManager.scene` (`Scene_Shop`, `Scene_Synthesize`, `Scene_Smith` for reinforcing, `Scene_EquipStone*` for gems, `Scene_Poker`, …), published as `screen`. `ActivityBuilder.ScreenTexts` names the ones with a text of their own; the same classes exist in 2.x. A menu opened on the world map stays `travel` |
| Medals | `NWConst::Library::MEDAL_DATA` without `NO_USE_MEDAL`, like the game's Library counts them (189 in 2.41, 394 in 3.06); earned ones are `$game_library.has_medal?`, shared by all saves. Published as `medals` and `medals_total` |
| Camp | `RPG::BGM.last.name` is `yaei` (*Camping* in the music room), published as `camping` |
| Conversations | Every message line passes `Game_Message#add`; the speaker is its Ace Message System name box (`\n<Name>`, also `\n1<`…`\n5<`, `\nc<`, `\nr<`), without companions' ` (Affection:\V[id])` suffix. The untranslated game has no name box but opens a message with the speaker alone on its first line, `【Name】`, sometimes followed by `（好感度：\V[id]）`. Lines by `Luka` (`ルカ`) are skipped. The speaker is kept with the interpreter running the event on screen (`$game_novel.interpreter` in `Scene_Novel`, `$game_map.interpreter` in `Scene_Map`) and that interpreter's `@list`; the conversation lasts while the same interpreter still runs the same list. Polling `$game_message` instead misses most of it: the message window clears it after every page. Published as `talking_to`. |
| Story (tooltip, picture) | The Collaboration Scenario: its maps are all under folder 920 *Collab Overworld* of the map tree (block 0, in 2.x and 3.x; the Pocket Castle starts it), and its acts count their progress in `Collab:C1` to `Collab:C12`; the act is the last one above 0. The end of Part 2 (common event 379, *Reaching the Monster Lord's Castle Throne*) closes an unfinished collab by setting `C1` to 8 and `C12` to 9, so only its maps tell that it is being played. Published as `collab_act`; the tooltip then shows the act instead of the part, and the picture is `collab`. The part: variable `Overall Events Progress`, which the story events raise from 0 at the intro to 40 at the Great Decision; below 20 is Part 1 (20 is set right after the escape from Tartarus), below 40 Part 2, from 40 Part 3. Published as `part`, `side` and, in Part 3, `route`, which `ActivityBuilder` puts in front of the tooltip. The picture: published as `picture`, an art asset of the Discord application, which `ActivityBuilder` shows instead of the asset `default` (the game's icon, also at the title screen); the route in Part 3, else the side. The side: switches `Ilias Chosen` / `Alice Chosen` (assets `ilias_adult`/`alice_adult`, or `ilias_sealed`/`alice_sealed` while the Ilias / Alice option is Sealed). The route: the variable counting its progress, looked up by name: 混沌ルート進行度 (`chaos`), 天界ルート進行度 (`destroyer`, the heaven route, logo 天界の破壊者), 魔界ルート進行度 (`judgment`, the demon realm route, logo 魔界の審判), in that order, whose assets are the logos `chaos`, `destroyer` and `judgment`, or `destroyer_layered` and `judgment_layered` (the logo over the route's heroines) while the Routes option is Layered. The Great Decision starts the chosen one at 1 (map 430 *Great Decision - Alice* Destroyer, map 431 *Great Decision - Ilias* Judgment), and a route not being played holds 0: every save after the Chaos route holds 0, 0 and 24 or 25. |
| Affection | `RPG::Actor#love`, variable `NWConst::Var::ACTOR_REL_BASE` (3000) + actor id, which `Game_Variables` redirects to `$game_global_system.actor_love` in the system save, so all saves share it |
| Requests | Map events start them with the script call `call_novel_scene(<common event>)`, which ends in `Game_Novel#setup`. A scene is a request when the Recollection Room (`NWConst::Library::H_SCENE_ITEMS`) lists its common event under a name matching `Requests::SCENE_NAME`; the character is the companion whose affection unlocks it (condition on variable `NWConst::Var::ACTOR_REL_BASE` (3000) + actor id, named from `$data_actors`), or else the entry's name without its form in brackets. The Recollection Room replays through the same method with switch `NWConst::Sw::LIBRARY_H_MEMORY` (443) on, which excludes replays. A running one is `$game_novel.running?` with `$game_novel.event_id`. |
| Defeat scenes | A lost battle calls `BattleManager.change_novel_scene`, which sets up `$game_troop.lose_event_id` (`NWConst::Common::LOSE_EVENT_BASE` (3000) + enemy id) as a novel scene. The monster girl is `$data_enemies[$game_temp.lose_event_enemy_id]` on 3.x, which records her at the start of the battle; 2.x has no `lose_event_enemy_id`, so there she is the event id minus `LOSE_EVENT_BASE`, as the game's own encyclopedia works it out. Not counted: encyclopedia battle replays (`BattleManager.memory_battle?`), the Recollection Room (switch 443), the Labyrinth of Chaos (its monsters all play `LOSE_EVENT_BASE` itself), and skipped scenes (skipping swaps the novel interpreter's `@list` for a copy). |
| Battle fucks | A map event starts one by calling a common event (`Game_Interpreter#command_117`), which returns once the match and the scene after a win are over. A common event starts a battle fuck when the Recollection Room lists it under the name `BF` (`Battlefucks::SCENE_NAME`, 58 entries); the battlefucker is the entry's name without its form in brackets. The game counts wins and losses itself (`$game_library.count_up_battlefuck_win`/`_lose` from common events 6999 and 7000, wins per save in variable `NWConst::Var::BATTLEFUCKER_DEFEAT` (907)). *Battlefucks won* reads `$game_library.battlefuck_win` with the Statistics option on *All saves*, variable 907 otherwise. A running one is the remembered interpreter still `running?` on the same `$game_map`, so loading a save or going back to the title mid-match forgets it. |
| Switches (looked up by name in `$data_system`, the translated name or the untranslated game's from `GameState::ORIGINAL_NAMES`; both share their ids) | `Ilias Chosen`, `Alice Chosen`, `Within Chaos Labyrinth`; the 80 switches named 親方発見 (Randolph found, ids 3101 to 3180 in 3.06, one per hiding place, all set on final-chapter maps; 2.x has none) for the Randolphs found |
| Spirits recruited | Actors named Sylph, Gnome, Undine and Salamander, untranslated シルフ, ノーム, ウンディーネ and サラマンダー (ids 12 to 15 in 2.x and 3.x) in `$game_party`'s `@include_actors`, the same roster the game's *Ally Joined Switch* (1000 + actor id) reads |
| Forging ore | Items 151 to 158 (Lump of Iron to Meteorite; 158 is a dummy in 2.x), each handed out once in its mine. Forging never uses them up: the blacksmiths (for example common event 107 *Papi's Smithy*) branch on holding one, so the best ore in `$game_party` is the latest unlocked |
| Naval side | Switches *Support Pirates* and *Support Navy* (2.x and 3.x), turned on by the choice at the Navy Headquarters (map 279); common event 153 clears them with the other story switches later on |
| Monster queens recruited | Actors 218, 245, 268, 280, 293, 315, 316, 322, 323, 328, 329, 334, 340 and 341 (the same in 2.x and 3.x) in `@include_actors`, like the spirits. The game has no counter of its own. By id, since 3.x has second actors named Cow Demon Queen (838) and Fatima (840) |
| Routes cleared (3.x only) | Switches *Angelic Dominion Route Clear* (Destroyer), *Monster Realm Route Clear* (Judgment) and 混沌ルートクリア (Chaos), per save |
| Phenomena of Ruin (Chaos route, 3.x only) | Variable 十六の破滅事象撃破数 holds the ones still **left**, despite its name: the Chaos Route Prologue (map 475 *Pocket Castle Attack B1F*) sets it to 16, each defeat subtracts 1 and calls common event 9060, which shows "Remaining Phenomena of Ruin" and, at 0, turns on switch 図鑑フラグ：十六の破滅事象全撃破. That switch tells the 0 after the last defeat from the 0 before the prologue. |
| Variables (looked up by name in `$data_system`, like the switches) | `Chaos Labyrinth Current LV` (the floor, in Normal and Carnage runs alike), `Labyrinth of Chaos: Type` (9 in a Normal run, 10 in Carnage; the Labyrinth's own events tell the two apart by it, in 2.x and 3.x), `Labyrinth of Chaos Rare Value` |

## Per-save statistics (`SaveStats`)

`$game_library` lives in the game's *system* save and is shared by all save slots. Its fields are the monster encyclopedia plus `party_stat`: the counts of defeats, escapes, losses and syntheses, the gold spent and the damage records.

**Counting.** `SaveStats` wraps the library's own update methods, so it counts the same events per save. The original method always runs first. The wrapped methods are:
- `count_up_party_defeat`, `count_up_party_escape`, `count_up_party_lose`, `count_up_party_synthesize`
- `addition_purchase_gold`
- `set_party_damage_record_actor`

Requests are counted from the `Game_Novel#setup` hook, defeat scenes from the `BattleManager.change_novel_scene` hook, each in total (`requests`, `rapes`) and per character (`SaveStats.tally`), whether or not the NSFW option is on.

**Storage.** In `$game_system`, as the instance variable `@mgq_discord_stats`: `{ :counts => { key => total }, :tallies => { key => { character => count } } }`. `Game_System` has no custom `marshal_dump`, so the game writes the variable with every save, autosave and backup save and reads it back on load, and a new game gets a new `$game_system` that starts at 0. Without the mod the variable is loaded along and never read.

**Earlier versions** kept the six library counters in `Discord/Stats/<save file name>.txt`: `key=value` lines plus `fingerprint=<save_count>:<frames_on_save>`. `SaveStats.import_legacy`, called from the `DataManager.load_game_without_rescue` hook, takes them over while the loaded save has no `@mgq_discord_stats` yet and the fingerprint matches.

**Hook order.** The mod loader runs at the translation's patch script, which comes *before* the `Plugins/*` scripts. None of those redefine any hooked method (checked: `Graphics.update`, `Game_Battler#item_apply`, `Game_Novel#setup`, `Game_Message#add`, `Game_Interpreter#command_117`, `BattleManager.change_novel_scene`, the `DataManager` save/autosave/backup-save/load methods, the `Game_Library` counters above, and `refresh` of `Window_Config` and the Mod Config Menu's `Window_ModConfig`). Recheck this if a translation update adds plugins. Other Patch folder mods load around this one in file name order and may wrap the same methods; the hooks alias under names of their own (`mgq_discord_*`) and always call the original, so they chain with mods that do the same.

## Options

`Options::MENU` describes each option's name, help, parent (`:under`), the parent's value it shows under (`:when`, optional) and values (the first is the default). Like EXP Overlord, the options are layered: `[Discord] Rich Presence` (`presence`) comes first and turns the whole status off; `NSFW`, `Statistics`, `Spoilers` and `Activity Image` (`picture`) sit under it. `Spoilers` (`spoilers`, default 0 *Hide*) makes `Story.hides_spoilers?` true in Part 3, which publishes `hide_spoilers=1`; `TriviaBuilder` then drops its `IsSpoiler` lines and `ActivityBuilder` leaves the map name off the first line (`AreaOf`), names nobody there (`NameOf`) and uses `SpoilerFreeRouteNames` in the tooltip. Under `Activity Image`, `Shown Image` (`shown_picture`) has `:when` 0 (*Static*), `Ilias / Alice` (`sealed_sides`) and `Routes` (`layered_routes`) `:when` 1 (*Dynamic*). `Options::INDENTS` puts the layer's indent in front of each name. An option without `:when` greys out while its parent is off (`Options.enabled?`, an `:enable` rule the game's own Config menu ignores); one with `:when` is taken out of the menu while its parent holds another value, and any option under a greyed out one is too (`Options.shown?`). `Options.arrange` rebuilds the mod's run of entries in the menu array, right below `Rich Presence`, which always shows; a `refresh` hook on `Window_Config` and `Window_ModConfig` calls it before every draw, as both windows refresh after each change and count the array anew. When it changed, the hook recreates the window's contents (the game's window sizes them to the option count, the Mod Config Menu measures its width) first. `Shown Image` picks one of `Options::FIXED_PICTURES`, every art asset of the application, published as `picture` like the story picture, or 0 for `default`. The story picture shows the side as `Story::ADULT_SIDES` or `Story::SEALED_SIDES`, and the route by its key (the logo) or as `Story::LAYERED_ROUTES`, as those options say. While it is off, `StatusText` publishes only `hidden=1`, and the DLL sends a `null` activity, which clears the profile. `Options.register` adds them to `NWConst::Config`: to `MOD_CONTENTS` when a mod config menu defined it (the community's Mod Config Menu, `Patch/0_ModConfigMenu.rb`, or the Mod Collection's Mod Config Remake, `Patch/0_ModConfigRemake.rb`, which keeps its interface), which the loader's file name order runs first, to the game's own `CONTENTS` otherwise. Both menus read `DATA`, `DATA_TEXT` and `DEFAULT` and store the value in `$game_system.conf`.

`$game_system.conf` is part of every save, but the options are meant to be the same for all saves and saves must stay untouched. So:
- **`Discord/Settings.ini`** holds them under short keys (`presence = 1`, `nsfw = 0`, `all_saves = 1`, `spoilers = 0`, `picture = 0`, `shown_picture = 0`, `sealed_sides = 1`, `layered_routes = 1`, `update_check = 1`, mapped in `Options::NAMES`). `Options.write` replaces only their lines and appends missing ones, so comments and hand edits survive. The file ships with the mod, so extracting a new zip by hand resets the options; `Update.ps1` puts them back.
- **`Options.sync`** runs before every publish. A new `$game_system.conf` (a save loaded, a new game, the title screen) gets the stored values; any other difference was made in a menu and is stored.
- **`Options.left_out_of_save`** wraps the save, autosave and backup-save methods and takes the options out of `$game_system.conf` while the game writes the file.
