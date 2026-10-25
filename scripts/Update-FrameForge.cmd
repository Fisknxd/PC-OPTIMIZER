@echo off
setlocal
if not exist "%~dp0FrameForge\FrameForge.exe" (
 echo Extract the entire ZIP before running this updater.
 pause
 exit /b 1
)
start "" /wait "%~dp0FrameForge\FrameForge.exe" --update-desktop
if errorlevel 1 (
 echo The update did not complete. Existing files and backups were preserved.
 pause
 exit /b 1
)
