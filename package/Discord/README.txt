Monster Girl Quest! Paradox RPG - Discord Rich Presence
=======================================================

Shows what you're doing in Monster Girl Quest! Paradox RPG on your
Discord profile: where you are and what you're doing (exploring,
traveling, in combat, Labyrinth of Chaos floor and rare points), rotating
trivia about your playthrough, and your party leader's level, race and
class when someone hovers the picture.


INSTALL
-------
1. Extract the download into your Monster Girl Quest! Paradox RPG
   folder (the one that contains Game.exe). It should then look like
   this:

      Game.exe
      DiscordPatcher.bat
      Discord\
      Patch\
      ...

2. Close the game, double-click DiscordPatcher.bat and choose "Yes".
   If Windows warns that it protected your PC, click "More info" and then
   "Run anyway": the setup is new and not signed.

3. Start the game as usual. Discord (the desktop app) needs to be running,
   and "Share your detected activities" must be enabled in Discord's
   Settings > Activity Privacy.


AFTER UPDATING THE TRANSLATION
------------------------------
A translation update removes this mod's loader. The game keeps working,
but your Discord status stops showing. Just double-click DiscordPatcher.bat
again and choose "Yes".


UNINSTALL
---------
Double-click DiscordPatcher.bat and choose "No" (= Uninstall), then delete
the Discord folder and DiscordPatcher.bat.


WHAT IT CHANGES
---------------
Only one file outside its own folder: a small loader block is added to the
end of Patch\Patch.rb (and that file's first line, a checksum, is updated).
A copy of your original file is kept as Patch\Patch.rb.backup.
If you delete the Discord folder without uninstalling, the game still
starts normally - the loader simply does nothing.

Your save files are never changed. Some trivia (enemies defeated, escapes,
wipeouts, biggest hit, gold spent, items synthesized) is counted per save
by the mod itself, because the game only counts those across all saves.
Those counters are kept in Discord\Stats\ (one small file per save slot),
so deleting the Discord folder removes them too. A save starts counting
the first time you save it with the mod installed; before that, and for
anything that happened earlier, these counters are 0. Everything else
(battles fought, gold, playtime, companions and so on) is read from the
save itself and covers the whole playthrough.


TROUBLESHOOTING
---------------
- Nothing on Discord: check that activity sharing is on. Discord can be
  started before or after the game; the status appears within about 15
  seconds. Then look at Discord\DiscordPresence.log.
- The status only updates about every 15 seconds (Discord's limit), and not
  while the game window is in the background - the game pauses then.
- Discord\InGame.log only appears if something went wrong inside
  the game. Include it when reporting a problem.
