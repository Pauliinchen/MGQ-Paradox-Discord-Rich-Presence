#----------------------------------------------------------------
#  Update.ps1
#
#  Changelog:
#      Paulinchen  2026-09-27: Created
#
#----------------------------------------------------------------

# Updates the mod in the game folder above this one to the latest release on GitHub, keeping the
# options in Settings.ini. Update.bat runs it.

$ErrorActionPreference = 'Stop'

# Invoke-WebRequest slows to a crawl while it draws its progress bar.
$ProgressPreference = 'SilentlyContinue'

$LatestReleaseUrl = 'https://api.github.com/repos/Pauliinchen/MGQ-Paradox-Discord-Rich-Presence/releases/latest'
$UserAgent        = 'MGQ-Paradox-Discord-RPC'
$ReleaseZip       = 'MGQ-Paradox-Discord-RPC-*.zip'

$ModDir   = $PSScriptRoot
$GameDir  = Split-Path $ModDir -Parent
$GameExe  = Join-Path $GameDir 'Game.exe'
$Dll      = Join-Path $ModDir 'DiscordPresence.dll'
$Settings = Join-Path $ModDir 'Settings.ini'

# The uninstaller versions before 1.3 left in the mod folder.
$LegacyUninstaller = Join-Path $ModDir 'Uninstall.exe'

# A line of Settings.ini that holds an option: indent, key, the equals sign with its spaces, value.
$OptionLine = '(?m)^([ \t]*)([A-Za-z_]+)([ \t]*=[ \t]*)([^\r\n]*)'

# Reads the version of the installed DLL.
#
# Returns the version, or $null when there is no DLL or it is a development build like 0.0.0-dev.
function Get-InstalledVersion {
    if (-not (Test-Path $Dll)) {
        return $null
    }

    $version = (Get-Item $Dll).VersionInfo.ProductVersion.Split('+')[0]
    if ($version.Contains('-')) {
        return $null
    }

    return [version]$version
}

# Reports whether the game in this folder is running, which locks the DLL.
function Test-GameRunning {
    $name = [IO.Path]::GetFileNameWithoutExtension($GameExe)
    return [bool](Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $GameExe })
}

# Reads the options of Settings.ini.
#
# Returns a hashtable of the values by key, empty when there is no Settings.ini.
function Read-Options {
    $options = @{}
    if (Test-Path $Settings) {
        foreach ($line in [regex]::Matches([IO.File]::ReadAllText($Settings), $OptionLine)) {
            $options[$line.Groups[2].Value] = $line.Groups[4].Value.Trim()
        }
    }

    return $options
}

# Puts option values back into Settings.ini. Keys it no longer has are dropped, and its comments
# and new keys stay as they are.
#
# $Options: the values by key, as Read-Options returned them.
function Restore-Options([hashtable]$Options) {
    $text = [IO.File]::ReadAllText($Settings)
    $restored = [regex]::Replace($text, $OptionLine, {
        param($line)
        $key = $line.Groups[2].Value
        if (-not $Options.ContainsKey($key)) {
            return $line.Value
        }

        return $line.Groups[1].Value + $key + $line.Groups[3].Value + $Options[$key]
    })

    [IO.File]::WriteAllText($Settings, $restored)
}

try {
    if (-not (Test-Path $GameExe)) {
        throw "Game.exe is not in $GameDir. Keep this script in the Discord folder next to Game.exe."
    }

    # Windows PowerShell 5.1 may still default to TLS 1.0, which GitHub refuses.
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    Write-Host 'Looking for the latest release . . .'
    $release = Invoke-RestMethod -Uri $LatestReleaseUrl -UserAgent $UserAgent -UseBasicParsing
    $latest = [version]$release.tag_name.TrimStart('v')
    $installed = Get-InstalledVersion

    if ($installed -and $installed -ge $latest) {
        Write-Host "The mod is up to date ($installed)."
        return
    }

    $zip = $release.assets | Where-Object { $_.name -like $ReleaseZip } | Select-Object -First 1
    if (-not $zip) {
        throw "Release $latest has no download yet. Try again in a few minutes."
    }

    $from = if ($installed) { $installed } else { 'an unknown version' }
    Write-Host "Updating from $from to $latest."

    while (Test-GameRunning) {
        Read-Host 'The game is running. Close it, then press Enter' | Out-Null
    }

    $download = Join-Path $env:TEMP $zip.name
    Write-Host "Downloading $($zip.name) . . ."
    Invoke-WebRequest -Uri $zip.browser_download_url -OutFile $download -UserAgent $UserAgent -UseBasicParsing

    $options = Read-Options
    Expand-Archive -Path $download -DestinationPath $GameDir -Force
    Restore-Options $options
    Remove-Item $download

    if (Test-Path $LegacyUninstaller) {
        Remove-Item $LegacyUninstaller
    }

    Write-Host "Updated to $latest. Your options are kept."
    Write-Host "What's new: $($release.html_url)"
}
catch {
    Write-Host "The update failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
