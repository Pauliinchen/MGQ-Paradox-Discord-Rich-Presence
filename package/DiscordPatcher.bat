@echo off
rem ----------------------------------------------------------------
rem   DiscordPatcher.bat
rem
rem   Changelog:
rem       Paulinchen  2026-09-25: Created
rem
rem ----------------------------------------------------------------

if not exist "%~dp0Discord\DiscordPresenceSetup.exe" (
  echo The "Discord" folder is missing next to this file.
  echo Extract the whole download into your game folder ^(the one with Game.exe^).
  pause
  exit /b 1
)
start "" "%~dp0Discord\DiscordPresenceSetup.exe"
