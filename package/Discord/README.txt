Monster Girl Quest! Paradox RPG - Discord Rich Presence
=======================================================

Shows what you're doing in Monster Girl Quest! Paradox RPG on your
Discord profile: where you are and what you're doing (exploring,
traveling, in combat, setting up camp, who you're talking to, idle,
Labyrinth of Chaos floor and rare points),
rotating trivia about your playthrough (the music that's playing
included), and the part of the story you're in with your side or route,
and your party leader's level, race and class, when someone hovers the
picture. Requests, defeat scenes and battle fucks only show if
you turn on the NSFW option (see OPTIONS).


REQUIREMENT
-----------
Monster Girl Quest! Paradox RPG 2.x or 3.x (tested on 2.x and 3.06) with
the English translation.

This is a Patch folder mod ("Type 1"), so the community's mod loader must
be installed: download "Patch.rb (enable Type 1 mods)" from the MGQ wiki
and put it into your Patch folder. If you already use other Patch folder
mods, you have it.

   https://mgq.miraheze.org/wiki/Paradox_mods#Patch.rb_(enable_Type_1_mods)


INSTALL
-------
1. Close the game and extract the download into your Monster Girl Quest!
   Paradox RPG folder (the one that contains Game.exe). It should then
   look like this:

      Game.exe
      Discord\
      Patch\Discord_RPC.rb
      ...

2. Start the game as usual. Discord (the desktop app) needs to be running,
   and "Share your detected activities" must be enabled in Discord's
   Settings > Activity Privacy.


UPDATE
------
The title screen tells you when a new release is out (see Update Check
under OPTIONS). Close the game and double-click Discord\Update.bat: it
downloads the latest release, installs it and keeps your options.

To update by hand, close the game and extract the new download over the
old one. That resets your options. Versions before 1.3 also left
Discord\Uninstall.exe, which is no longer used; you can delete it, and
Update.bat does so.


OPTIONS
-------
The mod adds its options to the game: in the Mod Config Menu if you have
it installed, in the game's own Config menu otherwise. They are grouped
under [Discord] Rich Presence, with the others indented below it.

[Discord] Rich Presence: On (the default) shows what you're doing on
Discord. Off shows nothing about the game, without uninstalling the mod;
the options below keep their values.

  NSFW: Off (the default) never mentions requests, defeat scenes
or battle fucks on Discord. On shows a request, defeat scene or battle
fuck while it plays, and how often they happened.

  Statistics: All saves (the default) shows the game's own counts
of enemies defeated, escapes, wipeouts, items synthesized, gold spent and
the biggest hit across every save. This save counts them for the save
you're playing instead, since you installed the mod.

  Spoilers: Hide (the default) keeps Part 3 spoilers off Discord while
you play Part 3: spoiler trivia is left out, the first line names neither
the map nor anybody ("Talking to someone . . ."), and the routes are
called Monster route, Angel route and Third route. Show shows everything.

  Activity Image: Static (the default) always shows the same picture, the
one picked under Shown Image. Dynamic shows Ilias or Alice, whoever you
picked this playthrough, in the final chapter the route you are on, and
the collab's heroes during the Collaboration Scenario.

    -> Shown Image (Static only): Default (the game's icon), Ilias or
    Alice in her adult form or sealed, the Judgment or Destroyer route's
    logo alone or over its heroines, Chaos or Collaboration Scenario.

    -> Ilias / Alice (Dynamic only): Sealed (the default) or Adult.

    -> Routes (Dynamic only): Layered (the default) shows the Judgment or
    Destroyer route's logo over its heroines, Logo the logo alone.

  Update Check: On (the default) asks GitHub for the mod's latest release
once per game start, and the title screen tells you when it is newer than
yours. Nothing else is sent. Off never asks GitHub.

The options under Activity Image only show while they apply. The options
are the same for every save and are kept in Discord\Settings.ini
(presence, nsfw, all_saves, spoilers, picture, shown_picture,
sealed_sides, layered_routes and update_check), not in your saves.
Update.bat keeps them, extracting a new download by hand resets them.


AFTER UPDATING THE TRANSLATION
------------------------------
A translation update replaces Patch\Patch.rb and with it the mod loader,
so no Patch folder mod is loaded any more. The game keeps working, but
your Discord status stops showing. Put the community's Patch.rb back into
the Patch folder.


UNINSTALL
---------
Close the game and delete Patch\Discord_RPC.rb and the Discord folder.
The mod loader stays for your other mods.


WHAT IT CHANGES
---------------
The mod itself is Patch\Discord_RPC.rb, like any other Patch folder mod,
and the community's mod loader runs it. Only its own files and your saves
(see below) are changed, and Patch\Patch.rb is never touched.
If you delete only the Discord folder, the game still starts normally -
the mod simply does nothing.

Some trivia (enemies defeated, escapes, wipeouts, biggest hit, gold spent,
items synthesized, requests, defeat scenes) is counted per save by the
mod itself, because the game doesn't count those per save. The first six
show the game's counts across all saves unless you set Statistics to
This save. Battle fucks won are counted by the game itself,
across all saves or, with This save, for the save you're playing.

The mod keeps these counters inside your saves, so copies, the autosave
and the game's backup save carry them along. Nothing else in your saves
changes, and they load the same without the mod, which then ignores the
counters. Counting starts when you install the mod; anything that
happened earlier isn't counted. Everything else (battles fought, gold,
playtime, companions and so on) is read from the save itself and covers
the whole playthrough, except affection, which the game shares between
all your saves.

Earlier versions kept the counters in Discord\Stats\. Loading a save
takes its numbers over once; after you've saved each save, you can
delete that folder.


TROUBLESHOOTING
---------------
- Nothing on Discord: check that the mod loader is installed and that
  activity sharing is on. Discord can be started before or after the
  game; the status appears within about 15 seconds. Then look at
  Discord\DiscordPresence.log.
- The status updates at most every 4 seconds (Discord's limit), and not
  while the game window is in the background - the game pauses then.
- Discord\InGame.log only appears if something went wrong inside
  the game. Include it when reporting a problem.


CREDITS
-------
The mod loader is the community's Patch.rb from the MGQ wiki (link above).
It is not included in this download.
