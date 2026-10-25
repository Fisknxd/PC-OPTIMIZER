# FrameForge 0.4.0

This release adds optional-service controls, app priority experiments, a live changes window, more registry preferences and guided Potato Settings for Fortnite and Rust.

## Services

Services has five individual Pause switches: Fax, Downloaded Maps Manager, Retail Demo, Windows Media Player network sharing and Print Spooler. Each card explains the feature you lose. Printing includes Print to PDF. Missing services are unavailable. These are optional capabilities, not a claim that every user should disable them.

Pause stops the service without forcing dependents and sets startup to Disabled until restored. The exact previous startup mode, including delayed automatic startup, and running/stopped state are journaled before changes. Switching Pause off restores that record. Recovery offers the same undo. Administrator access, a supported Windows scan and Balanced/Advanced review mode are required. Already-disabled services without a FrameForge backup cannot be automatically enabled. Active dependent services block stopping. A command failure triggers an attempt to restore the original state; rollback failures are reported with the saved values preserved.

Windows Defender, updates, networking, audio, drivers, anti-cheat and arbitrary services are not offered. A service's priority is its process priority; shared hosts can contain multiple services. This release excludes service processes from priority experiments instead of raising shared host priorities.

## App priority

App priority lists eligible open desktop apps in the current session. Choose Below normal, Normal, Above normal or High. Above normal is the initial choice. High has an explicit contention warning; Real-time is unavailable. Windows and service processes, inaccessible apps and known anti-cheat paths are excluded. Access denial does not trigger a bypass.

Changes are saved before writes, then read back. Process ID, executable path and start time bind each record to that exact running process. Undo rejects reused IDs, external priority changes and older overlapping records. A priority change lasts until the app exits. Raising priority can worsen audio, input or frame pacing under CPU contention; there is no measured improvement claim.

## Watch changes

Watch changes opens a separate live console-style window. Confirmed actions can open it automatically, with a saved preference to turn this off. It shows real native-operation events, saved originals, verification results, actual executed PowerShell scripts and streamed standard output/error. It does not animate Settings switches or display pretend commands. Service commands are executed by Windows PowerShell with a fixed allowlist; native registry/desktop changes use their existing APIs. Logs can be saved to a chosen text file. The in-memory feed is bounded. App removal, cleanup summaries and restore-point milestones also appear.

## Registry preferences

Five HKCU preferences were added: advertising-ID personalization, Windows tips, Start app suggestions, Settings suggested content and picture lock-screen tips. Together with existing switches there are 17 direct tweak controls. All have short descriptions and exact-value undo. These are clutter/privacy choices, not registry FPS hacks. New controls require an existing supported preference instead of inventing a missing registry value. Start suggestions is Windows 10 only. Content delivery behavior varies with Windows version, Spotlight, account and policy; sign-out may be needed. Registry readback verifies the stored value, not every Windows visual effect.

## Potato Settings

Fortnite and Rust each have a six-step guided setup using normal in-game graphics controls. Open the launcher, follow the steps, record original settings and mark progress. Notes and checkmarks are local. Checkmarks explicitly represent user-reported progress, not verified game changes. Resetting the checklist does not restore graphics; use the saved originals inside the game. No game configuration files are modified.

Fortnite uses its official Performance rendering mode, low quality, optional resolution reductions and launcher-managed removal of high-resolution textures. Rust uses the lowest offered quality preset, current Shadow Quality controls, optional effects and normal screen resolutions. Recent Rust versions reorganized shadows and impose a minimum extra-shadow-light setting; no below-minimum console value is supplied.

The stripped materials/terrain appearance in promotional videos cannot be established as safe from screenshots. No material replacement, shader editing, injected renderer, vegetation stripping, protected-file editing or anti-cheat changes were added. Using built-in options is the conservative implementation choice; it is not a guarantee against bans or changing game/server rules. Neither the screenshots' appearance nor a 60-to-240-FPS gain is promised.

## Verification and limits

Release checks include 125 passing portable tests and two privilege-dependent symlink skips, WPF contrast/layout checks for all 13 pages and seven compact layouts, all 17 tweak click paths, simulated service cancel/pause/restore click paths, checklist persistence, restore-point coordinator/UI tests and activity rendering. Three packaging tests validate the distribution logic.

Read-only Windows checks read native settings, five service states and eligible desktop app priorities, parse all 20 generated service write/restore script variants, and run a real streamed PowerShell status command. They also exercise hardware inventory and live CPU/RAM monitoring. No Windows services, game graphics, registry preferences or app priorities on the user's PC were changed for acceptance testing. Service mutations and priority writes still need live administrator/user-app acceptance across supported PCs. The prior real restore-point creation attempt remains unverified because elevation was declined; it was not repeated.

## Official references checked 2026-10-16

- [Epic: enabling Fortnite Performance mode](https://www.epicgames.com/help/c-1/a20197720?lang=en-US).
- [Epic: troubleshooting low FPS](https://www.epicgames.com/help/c-34254770/a26749910). Hardware-dependent results; optional high-resolution textures are managed by the launcher.
- [Facepunch: Rust shadow settings rework](https://rust.facepunch.com/news/shipshape). Shadow presets and minimum extra shadow lights.
- [Facepunch: quality dropdown](https://rust.facepunch.com/news/coupling-the-rails) and [older Super Potato reference](https://rust.facepunch.com/news/zipline-update). Labels have changed over time.
- [Facepunch: Terms of Service](https://facepunch.com/legal/tos/) and [modding guidelines](https://facepunch.com/legal/modding). These do not establish that arbitrary competitive graphics modifications are permitted.
- [Microsoft: process priority classes](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processpriorityclass?view=net-10.0). High and Real-time have substantial scheduling tradeoffs.
- [Microsoft: service configuration guidance](https://learn.microsoft.com/en-us/windows/iot/iot-enterprise/optimize/services). Capability consequences are useful context; IoT guidance is not treated as a blanket gaming recommendation.
