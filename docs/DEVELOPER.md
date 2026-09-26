# Developer notes

## Layout

```
MGQ-Paradox-Discord-Rich-Presence.slnx   Visual Studio solution
GameScript/Discord_RPC.rb                Ruby, runs inside the game
MGQParadox.DiscordPresence/              C# NativeAOT project -> DiscordPresence.dll, and the package
MGQParadox.DiscordPresence.Setup/        C# project -> Uninstall.exe
package/                                 static files shipped as-is
Shipping/                                publish output, git-ignored
docs/DEVELOPER.md                        this file
.github/workflows/release.yml            builds and attaches the zip on release
```

**`GameScript/Discord_RPC.rb`:** a Patch folder mod, shipped as `Patch/Discord_RPC.rb` and loaded by the mod loader in `Patch.rb`. It collects the game state and hands it to `DiscordPresence.dll`. Everything lives in the module `MGQ_Discord`, which holds the settings and the start-up, tick and publish steps. Nested modules do one job each:

| Module | What it is |
|---|---|
| `Log` | `InGame.log`, capped per session. |
| `NumberFormat` | Thousands separators, counted nouns, the game's large-number style. |
| `GameState` | Everything read from the game: scene, area, vehicle, Labyrinth of Chaos, mastery. |
| `Trivia` | The second Discord line. **All trivia texts live here**, one named method per line, rotating in the order of `LINES`. |
| `SaveStats` | The per-save counters, see [Per-save statistics](#per-save-statistics-savestats). |
| `StatusText` | The `key=value` status. The keys are shared with `Game/GameStatus.cs`. |
| `Presence` | Calls `DiscordPresence.dll` through `Win32API`. |

The game hooks follow the module at the end of the file. `MGQ_Discord.hookable?` guards them, see [Earlier versions](#earlier-versions).

**`MGQParadox.DiscordPresence/`** builds `DiscordPresence.dll`: C# compiled with NativeAOT into a native 32-bit DLL, because the game is a 32-bit process. Players need no .NET installed. Namespaces follow the folders:

| Path | What it is |
|---|---|
| `Exports.cs` | The functions `Discord_RPC.rb` calls: `presence_start` and `presence_update`. Nothing may throw out of them. |
| `PresenceLoop.cs` | The DLL's own thread: rate limit, trivia rotation, reconnecting to Discord. |
| `ActivityBuilder.cs` | Builds the Discord activity. **All user-visible texts on the C# side live here.** |
| `PresenceImage.cs` | Picks the picture: `large_image` from `Settings.ini`, or the application icon. |
| `ModFolder.cs` | The `Discord` folder, which the game script finds the DLL in. |
| `Log.cs`, `Settings.cs` | `DiscordPresence.log` and `Settings.ini` in that folder. |
| `NativeMethods.cs` | Lets the DLL find its own folder. |
| `Game/GameStatus.cs` | One parsed status, a property per value. `Scene.cs` and `Vehicle.cs` hold its enums. |
| `Discord/DiscordIpcClient.cs` | Discord's local named-pipe protocol. `Opcode.cs` holds the frame kinds. |
| `Discord/Activity.cs`, `Discord/Json.cs` | The activity and its JSON, with Discord's field limits. |

**`MGQParadox.DiscordPresence.Setup/`** builds the uninstaller `Uninstall.exe` (.NET Framework 4.8 with WinForms), which ships in `Discord/`. Installing needs no program. `ModFolder.cs` and `Log.cs` are linked in from the DLL project.

| Path | What it is |
|---|---|
| `Program.cs` | Entry point. No argument asks first and reports in dialogs; `--silent` uninstalls without them. |
| `Uninstaller.cs` | The dialogs and the uninstall flow. `SetupMode.cs` holds its modes. |
| `GameFolder.cs` | The game folder around the mod folder: layout check, paths, whether the game runs. |
| `PatchLoader.cs` | The loader block the earlier versions added to `Patch/Patch.rb`: finding and removing it, safe write. |
| `NWPatchChecksum.cs` | The checksum the game verifies on line 1 of `Patch.rb`. |

**`package/Discord/`:** `Settings.ini` (`client_id`, optional `large_image`) and the player `README.txt`.

## Build and test

You need the .NET 10 SDK and the Visual Studio workload **Desktop development with C++**, whose linker NativeAOT uses.

Everything is in the projects; there is no separate build script. Publishing the DLL project assembles the complete release layout in `Shipping/` at the repository root, building the setup project along the way. Copy its content into a game folder to install or update the mod there:

```
Discord/  DiscordPresence.dll  Uninstall.exe  Settings.ini  README.txt
Patch/    Discord_RPC.rb
```

- **Publish, not build:** only a publish runs NativeAOT, so a plain build gives no usable DLL.

  ```powershell
  dotnet publish MGQParadox.DiscordPresence -c Release -p:Version=1.2.0
  ```

- **Release publishes** also zip it: `bin/Release/MGQ-Paradox-Discord-RPC-<Version>.zip`.
- **Every publish replaces `Shipping/`.** Close the game before copying it over an install: `DiscordPresence.dll` is locked while the game runs.
- **Visual Studio:** open the `.slnx`. The shipped files appear in the DLL project under `Shipped`.
- **Game folder for testing:** it needs the community's mod loader (see [How it hooks in](#how-it-hooks-in)). Then every change only needs a publish, a copy and a game restart.
- **Logs:** `DiscordPresence.log` (DLL and uninstaller) and `InGame.log` (in-game errors). Set `DEBUG = true` in `Discord_RPC.rb` to log every status write.

## Conventions

- **File headers.** Every `.cs` and `.rb` file starts with a header block listing the file name and a changelog, newest entry first. Entries by the same author on the same day are grouped. Only changes after the first commit are recorded; before that, a file carries just `Created`.
- **Documentation comments.** Every type and member is documented: XML comments in C#, YARD-style comments in Ruby. The summary is mandatory, parameters and return values whenever they apply, and remarks only where they add something. Keep them short, and always describe the current state.
- **Inline comments.** Kept to a minimum. The code has to speak for itself through its names and structure. A comment is only added where the code cannot say something itself: a constraint, a trap, or the failure a line prevents.
- **Names.** Magic strings and numbers get a named constant. Enums replace string states.
- **Language.** Code, comments and documentation are English.

## Releasing

1. Push your changes.
2. On GitHub, go to **Releases → Draft a new release**, create a tag like `v1.0.0`, write the notes, and click **Publish**.
3. The *Release* workflow builds the mod and attaches `MGQ-Paradox-Discord-RPC-1.0.0.zip` to that release within a few minutes. Check the **Actions** tab if it doesn't appear.

## How it hooks in

The game's script "パッチスクリプト読込" (module `NWPatch`) evals `Patch/Patch.rb`, but only if line 1 holds a valid checksum of the rest of the file.

The mod requires the community's mod loader, a replacement `Patch.rb` from [*Patch.rb (enable Type 1 mods)*](https://mgq.miraheze.org/wiki/Paradox_mods#Patch.rb_(enable_Type_1_mods)) on the MGQ wiki. It is not ours, so it is never shipped, copied or written. The current version evals every `.rb` file under `Patch/` (recursively, sorted, except `Patch.rb`), rescues each one and reports a failing mod in a message box. This mod is one of those files, `Patch/Discord_RPC.rb`, which is *not* checksummed, so installing or updating the mod is just extracting the zip. Translation updates overwrite `Patch.rb`; the user puts the community's `Patch.rb` back.

### Earlier versions

They appended a block (`# >>> MGQ Discord RPC` … `# <<< MGQ Discord RPC`) to `Patch.rb` that loads `Discord/rpc.rb`, and shipped `DiscordPatcher.bat` next to `Game.exe`. Their hooks use the same alias names, so loaded next to `Discord_RPC.rb`, each hook would call itself until the stack overflows.

- **At start-up** `MGQ_Discord.hookable?` deletes `Discord/rpc.rb`. The block runs *after* the mod loader, which sits above it in `Patch.rb`, so on the very first start after extracting the new zip it already finds nothing to load. The hooks are skipped (and `InGame.log` says why) when `Graphics` already has `mgq_discord_update`, from another copy of this script or an earlier version, or when `rpc.rb` cannot be deleted *and* `Patch.rb` still holds the block that would load it. A leftover `rpc.rb` with no block to load it only gets a log line.
- **Uninstalling** deletes `Patch/Discord_RPC.rb`, `Discord/rpc.rb` and the old `DiscordPatcher.bat`, then removes the block from `Patch.rb` and recomputes line 1. Their `Patch.rb.backup` goes too when it matches the cleaned file. `Patch.rb` is written last, through a temporary file, so a failure never leaves it half changed. The mod loader stays for the other mods.

**The checksum:**
1. For every line after the first, strip digits and whitespace. Both are ASCII-only, like Ruby 1.9's `\d` and `\s`.
2. Sum the codepoints into `n`.
3. The checksum is `n * <last character of Math.sqrt(n).to_s>`.

Ruby's `Float#to_s` prints the *shortest* round-tripping decimal, and `x.0` for integral values. .NET's `"R"` format gives 17 digits and a different last digit, which makes the game exit on launch. `NWPatchChecksum.FormatLikeRuby` replicates Ruby.

## Runtime

`Discord_RPC.rb` hooks `Graphics.update`. At start-up it calls `presence_start`, and every 30 frames it hands changed `key=value` lines to `presence_update`. Both return at once. The DLL's own thread parses the latest status and sends `SET_ACTIVITY` over `\\.\pipe\discord-ipc-N`.

- **Nothing blocks the game.** `presence_update` only stores the text; parsing, the network and the pipe all run on the DLL's thread.
- **Nothing throws into the game.** Every export and the DLL's threads catch everything; an exception escaping them would end the game.
- **No process to watch.** The DLL lives and dies with the game. When the game closes, the pipe closes and Discord clears the status by itself.
- **Reconnecting:** when Discord closes the pipe, for example on a restart, the DLL tries again every 15 s, logs the outage once, and sends the current status again once connected.
- **Rate limit:** Discord drops updates sent less than 15 s apart. The DLL throttles to that and rotates the trivia on the same 15 s tick.
- **F12:** the game's reset only restarts `rgss_main`; `Patch.rb` and with it `Discord_RPC.rb` are evaluated once per start. Should the file be evaluated twice anyway, `MGQ_Discord.hookable?` keeps the hooks from wrapping themselves.

## Game data used (Paradox)

| What | Source |
|---|---|
| Levels | `actor.base_level` / `class_level` / `tribe_level`; the race is `actor.tribe` |
| World map | `$game_map.overworld?` (tileset mode 0), except inside the LoC |
| Battles fought | `$game_system.battle_count` (per save) |
| Defeats, escapes, wipeouts, synthesis, gold spent, biggest hit | `SaveStats` in `Discord_RPC.rb` (per save; see below). **Not** `$game_library.party_*`, which is shared by all saves. |
| Difficulty | `$game_variables[NWConst::Var::CURRENT_DIFFICULTY]` (-2..4) |
| Switches (looked up by name in `$data_system`) | `Ilias Chosen`, `Alice Chosen`, `Within Chaos Labyrinth` |
| Variables (looked up by name in `$data_system`) | `Chaos Labyrinth Current LV` (floor), `Labyrinth of Chaos Rare Value`, `Carnage Labyrinth of Chaos Current Floor` (> 0 counts as Carnage; **unverified**) |

The variable `Labyrinth of Chaos: Type` is *not* normal vs. Carnage: it held 9 on a normal run.

## Per-save statistics (`SaveStats`)

`$game_library` lives in the game's *system* save and is shared by all save slots. Its fields are the monster encyclopedia plus `party_stat`: the counts of defeats, escapes, losses and syntheses, the gold spent and the damage records.

**Counting.** `SaveStats` wraps the library's own update methods, so it counts the same events per save. The original method always runs first. The wrapped methods are:
- `count_up_party_defeat`, `count_up_party_escape`, `count_up_party_lose`, `count_up_party_synthesize`
- `addition_purchase_gold`
- `set_party_damage_record_actor`

**Storage.** Deliberately *not* in the save file, so that uninstalling leaves no trace and saves are identical with or without the mod:
- `DataManager.save_game_without_rescue(index)` → writes `Discord/Stats/SaveNN.txt`.
- `DataManager.auto_save_game_without_rescue(index)` → writes `Discord/Stats/AutoSave01.txt`. Autosaves have their own method, so without this hook, loading one would start every counter at 0.
- `DataManager.load_game_without_rescue(index)` → reads that file.
- `DataManager.setup_new_game` → resets the counters.

**Fingerprint.** Each stats file stores `save_count:frames_on_save` (both from `$game_system`, and both set in `on_before_save`). On load, the file is only used if the fingerprint matches, so a save that was replaced or copied outside the game starts fresh instead of showing another run's numbers.

**Known gaps:**
- The save *backup* (`Save/SaveBackup.rvdata2`) has no stats file.
- A backup loaded later starts at 0.

**Hook order.** The mod loader runs at the translation's patch script, which comes *before* the `Plugins/*` scripts. None of those redefine any hooked method (checked: `Graphics.update`, `Game_Battler#item_apply`, the `DataManager` save/autosave/load/new-game methods, and the `Game_Library` counters above). Recheck this if a translation update adds plugins. Other Patch folder mods load around this one in file name order and may wrap the same methods; the hooks alias under names of their own (`mgq_discord_*`) and always call the original, so they chain with mods that do the same.
