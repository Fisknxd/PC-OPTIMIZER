FRAMEFORGE 0.8.0 — WINDOWS X64 PREVIEW

START
Extract the entire ZIP. Open FrameForge/FrameForge.exe and keep its DLLs
alongside it. The runtime is included. Windows 10 build 19045+ or Windows 11,
x64, is required. Use Update-FrameForge.cmd to replace a prior Desktop copy;
the previous folder is kept as a dated backup. Local saved history is retained.

ADMINISTRATOR ACCESS
Windows asks permission at launch. Approve to allow administrator features.
Cancel to open with standard access. Access is shown in the sidebar. The app
rejects elevation into a different Windows account to protect user settings.

TWEAKS
Run Scan PC, then open Tweaks. Every control has a short explanation.
Review the requested change before confirming. Previous values are saved and
the result is checked. Recovery > Undo session restores a whole tuning session; individual Undo is in its details. Choose
More choices for settings with tradeoffs. Unsupported settings are disabled
with a reason. Custom/default states are labelled rather than guessed.

There are 30 direct tweak controls, seven optional-service Pause switches,
two plugged-in power checks, app priority experiments and a live changes window with actual PowerShell
output and native-operation verification. Potato Settings applies saved graphics profiles for Fortnite and Rust in one
click. Game configs are backed up first and can be restored on the game page
or in Recovery. Close the game first. Unsupported fields are skipped.
Low-end PC offers 17 selectable background-work and desktop choices with undo. Choose Recommended or Browser memory as a starting selection. Your choices are remembered. Clear selection remains cleared. Stronger browser choices include their prerequisites. Already-configured settings are left unchecked. Open More choices for browser, download and Windows controls. Browser policies require acceptance in chrome://policy or edge://policy; Edge sleeping-tab policies exclude personal Microsoft-account profiles. Power checks preserve your existing plan and battery settings.

Game Mode, recording, mouse, notifications and desktop settings have direct
controls. Game setup contains 30 short guides for the supplied feature list.
Game, driver, monitor and router choices remain manual, clearly labelled steps.
Desktop animation and menu settings do not claim to reduce in-game input delay.

RECOVERY
Create restore point uses Windows System Restore, then checks the PC's local
Windows restore-point list for the new point. It needs administrator permission
and System Protection enabled for the Windows drive. Windows manages storage;
FrameForge's change-history JSON is not a Windows restore point.

The creation-frequency setting is saved before temporary adjustment and restored
on completion or failure. Recovery can finish an interrupted setup. Restore
points protect system settings, not personal files or guaranteed app reinstall.
The real creation test in this run was NOT VERIFIED because administrator
approval was declined. In-memory coordinator and recovery UI tests passed.

APPS AND CLEANUP
App removal is individual and checked for protected components and dependencies.
It can delete app data. Cleanup previews old selected temporary files and skips
changed/locked/sensitive files. Neither action has an Undo button in FrameForge.

MEASUREMENT
Live stats has optional CPU/RAM sampling, CSV export and frame-time comparisons.
Game FPS comparisons use imported PresentMon or FrameTimeMs files. Pick one app/process/stream; unlike interval types are kept separate. p99 uses nearest-rank calculations; legacy p99 results are labelled. There is no injected
overlay. No universal FPS, ping or input-delay improvement is promised.

LOCAL DATA
Settings and undo history: %LOCALAPPDATA%/FrameForge. Keep that folder while
relying on Undo. No account, analytics or persistent background service. Inspect
exported reports before sharing because they can include local hardware/paths.

VERIFICATION
Release build passed with no warnings. 150 tests passed; 2 symlink privilege
tests were skipped. Windows control, service round-trip UI, recovery UI and read-only native checks
passed. See Source/docs/UPDATE-0.8.0.md and the validation bundle for details.

Service writes and app priority writes still need live acceptance on supported
PCs. Their journal/undo logic is tested with simulated states. Installed game files and Windows settings on
this PC were not modified for release validation. The real Windows file
backend was tested against disposable graphics-config fixtures. Read UPDATE-0.8.0 for scope.

SMOOTHER DAILY USE
Search keeps existing controls and focus. Use categories and current-state filters
to find a tweak. Refresh rereads Windows; applying always checks the real current
state. Navigation keeps each page position, guide selections and connection input.
Live changes batches output and lets you pause Follow latest to read earlier steps.
Large imported recordings are prepared in a background worker.
