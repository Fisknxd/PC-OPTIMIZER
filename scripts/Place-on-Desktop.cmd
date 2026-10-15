@echo off
setlocal
set "FRAMEFORGE_BUNDLE_ROOT=%~dp0"
powershell.exe -NoLogo -NoProfile -Command "$ErrorActionPreference='Stop'; try { $source=Join-Path $env:FRAMEFORGE_BUNDLE_ROOT 'FrameForge'; if (-not (Test-Path -LiteralPath (Join-Path $source 'FrameForge.exe'))) { throw 'Extract the entire ZIP before running this file.' }; $desktop=[Environment]::GetFolderPath('Desktop'); if ([string]::IsNullOrWhiteSpace($desktop)) { throw 'Windows Desktop could not be located.' }; $destination=Join-Path $desktop 'FrameForge'; if (Test-Path -LiteralPath $destination) { throw 'A FrameForge folder already exists on Desktop. Rename it first; no existing files were overwritten.' }; Copy-Item -LiteralPath $source -Destination $destination -Recurse -ErrorAction Stop; Write-Host ('Copied all application and source files to: '+$destination); Write-Host 'Open that folder and run FrameForge.exe. Windows validation remains pending.' } catch { Write-Error $_; exit 1 }"
if errorlevel 1 (
 echo Desktop copy failed. See the message above.
 pause
 exit /b 1
)
pause
