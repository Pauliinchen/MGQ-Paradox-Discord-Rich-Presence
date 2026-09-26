Monster Girl Quest! Paradox RPG - Discord Rich Presence
=======================================================

Shows what you're doing in Monster Girl Quest! Paradox RPG on your
Discord profile: where you are and what you're doing (exploring,
traveling, in combat, idle, Labyrinth of Chaos floor and rare points),
rotating trivia about your playthrough (the music that's playing
included), and your party leader's level, race and class when someone
hovers the picture. Requests and defeat scenes only show if you turn on
the NSFW option (see OPTIONS).


REQUIREMENT
-----------
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

Updating works the same way: close the game and extract the new download
over the old one.


OPTIONS
-------
The mod adds two options to the game: in the Mod Config Menu if you have
it installed, in the game's own Config menu otherwise.

[Discord] NSFW: Off (the default) never mentions requests or defeat
scenes on Discord. On shows a request or defeat scene while it plays, and
how often they happened.

[Discord] Statistics: All saves (the default) shows the game's own counts
of enemies defeated, escapes, wipeouts, items synthesized, gold spent and
the biggest hit across every save. This save counts them for the save
you're playing instead, since you installed the mod.

The options are the same for every save and are kept in
Discord\Settings.ini (nsfw and all_saves, 0 or 1), not in your saves.
Updating the mod resets them.


AFTER UPDATING THE TRANSLATION
------------------------------
A translation update replaces Patch\Patch.rb and with it the mod loader,
so no Patch folder mod is loaded any more. The game keeps working, but
your Discord status stops showing. Put the community's Patch.rb back into
the Patch folder.


UNINSTALL
---------
Close the game, double-click Discord\Uninstall.exe and choose "Yes". It
deletes Patch\Discord_RPC.rb, so the mod loader no longer loads the mod;
the loader itself stays for your other mods. Then delete the Discord
folder. Deleting Patch\Discord_RPC.rb and the Discord folder by hand does
the same.
If Windows warns that it protected your PC, click "More info" and then
"Run anyway": the uninstaller is not signed.


WHAT IT CHANGES
---------------
The mod itself is Patch\Discord_RPC.rb, like any other Patch folder mod,
and the community's mod loader runs it. Only its own files and your saves
(see below) are changed, and Patch\Patch.rb is never touched.
If you delete the Discord folder without uninstalling, the game still
starts normally - the mod simply does nothing.

Some trivia (enemies defeated, escapes, wipeouts, biggest hit, gold spent,
items synthesized, requests, defeat scenes) is counted per save by the
mod itself, because the game doesn't count those per save. The first six
show the game's counts across all saves unless you set [Discord]
Statistics to This save.

The mod keeps these counters inside your saves, so copies, the autosave
and the game's backup save carry them along. Nothing else in your saves
changes, and they load the same without the mod, which then ignores the
counters. Counting starts when you install the mod; anything that
happened earlier isn't counted. Everything else (battles fought, gold,
playtime, companions and so on) is read from the save itself and covers
the whole playthrough.

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
