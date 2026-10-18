# FrameForge 0.5.0

Potato Settings now applies real game configurations in one click. It replaces the old manual checklist. This release also adds a selectable low-end PC profile, four direct tweak switches, two optional service controls and a desktop High performance plan experiment.

## One-click Potato Settings

The page discovers the current user's Fortnite settings and installed Rust Steam libraries. Select Potato or Sharper potato, inspect the proposed changes if wanted, and press Apply Potato Settings. There is no checklist and no additional confirmation dialog. Close the game first; the app refuses edits while the game is running. Missing installations/configurations have a Locate config option. Ambiguous installations require choosing the active config rather than guessing.

Fortnite: Performance feature level, lowest saved quality groups including texture, shadows, shading and landscape, supported costly lighting effects off, and 50% internal render scale. Sharper potato uses 75%. Existing rendering preferences are adjusted only where saved. Rust: minimum saved terrain/object/tree/decoration/particle/water detail, current shadow preset, optional effects off and 50% render scale; Sharper potato uses 75%. In both games, changing the internal render scale preserves the chosen output resolution. 50% in each dimension represents one quarter of the rendered pixel count, not a claim of four times the FPS.

Only known entries already exposed by the game's config are changed. Required schema markers must be present. Unknown/missing optional entries are listed as skipped; removed commands are not inserted. Unrelated binds, account options, mouse settings, audio and other config values are preserved. No Engine.ini pack, material replacement, shader injection, anti-cheat edit, out-of-range grass distance or game executable modification is included.

### Rust's old flat-terrain mode

Facepunch documented shader level 100 as a legitimate built-in potato mode in 2017, then deliberately added ground texture detail to reduce the visibility advantage of smooth terrain. Those historical articles do not establish current support. If an existing legacy config exposes graphics.shaderlod, the profile can set its documented minimum of 100. The current Rust configuration inspected on this PC does not expose that control. It is skipped rather than invented. Most detail controls here were already at their minimum, so the preview correctly showed one remaining change: render scale. Fortnite's preview showed four remaining changes.

Normal config values are the chosen boundary for this implementation, not a promise of ban immunity. Current game updates, launch overrides and servers may clamp or ignore options. The app verifies the saved file; actual rendering and FPS still require checking in the game. It cannot promise the exact appearance of a promotional video.

## Backups and restore

Before writing, the exact original bytes and the changed fields are journaled to LocalAppData/FrameForge/game-graphics-history.json. The app preserves UTF-8/UTF-16 encoding, BOMs, line endings, comments and unrelated entries. Windows writes use an exclusive handle, real file identity, content hash checks, link/hard-link/system-file checks and a disk flush. Read-only protection is honored. No read-only lock is added to game configs.

Restore original graphics is available on the game page and in Recovery. It restores exact bytes if the file still matches the applied version. If the game saved unrelated settings afterward, restore merges only the fields FrameForge changed. It stops on conflicting graphics changes, newer overlapping profile records, invalid backups or running games. A file copy of the original can be exported for manual recovery. An interrupted partial write may need that original file copy if a safe merge cannot be established.

## Low-end PC profile

Low-end PC has one prominent Apply selected low-end profile button and per-option explanations. It preflights all chosen settings before the first write, checks them again at application, saves each original, stops on the first failure, and offers grouped restore. Successfully applied changes remain recoverable if a later change fails.

Initially selected: stop Edge startup boost/background mode, turn off unused Windows game recording, reduce Windows transparency, remove app animations and remove menu animations. Edge is allowed to close normally; the app does not terminate a browser or discard tabs. This can release background browser memory once closed. Desktop drawing changes mainly affect desktop overhead and feel, not a proven reduction in game input delay.

Optional, initially unchecked: deny supported Windows app background work, disable update peer sharing, or try the installed High performance power plan. App background denial can stop notifications/sync. Peer sharing off preserves normal HTTP Windows updates. Both machine policies require administrator access and a recognized supported Windows policy edition. High performance requires a desktop, an existing plan and Advanced mode, and may increase heat/power use. OEM plans are not replaced or fabricated. These choices are also available individually in Tweaks.

Services adds optional Windows diagnostics (DiagTrack) and file/mail indexing (WSearch) controls. Indexing pause is intended to compare measured indexing load; it can make Windows/Outlook search slower. No SysMain blanket disable, shared-service-host merging, Defender/Update removal, pagefile disable, memory purge, fixed CPU affinity, timer/HPET pack or router-priority claim is added.

There are 21 direct tweak options and seven optional service controls. Unsupported Windows preferences remain unavailable with a reason.

## Inspiration and evidence

WinUtil's categories and definitions were inspected as reference data, not downloaded and executed. Registry/policy values were checked against Microsoft documentation and implemented within FrameForge's own backup/verification boundary. EXM's published category descriptions and support material informed grouping and one-click flows; its proprietary implementation was not inspected or copied. Neither tool's marketing is treated as proof of FPS gains.

- [WinUtil source reviewed](https://github.com/ChrisTitusTech/winutil/blob/07ccd8e2e755a706f31569808b31f5b77acad6a9/config/tweaks.json).
- [EXM](https://exmtweaks.com/en-eu) and [its usage guide](https://support.exmtweaks.com/en/using-exm).
- [Epic's current Performance mode guidance](https://www.epicgames.com/help/c-202300000001636/c-202300000001719/a202300000013484), including the saved user settings location and supported renderer arguments.
- [Epic's scalability reference](https://dev.epicgames.com/documentation/en-us/unreal-engine/scalability-reference-for-unreal-engine). This documents engine quality groups; it is not proof that every Fortnite option is honored in every release.
- [Facepunch: original potato shader](https://rust.facepunch.com/news/devblog-148), [the subsequent terrain detail change](https://rust.facepunch.com/news/devblog-149) and [current shadow rework](https://rust.facepunch.com/news/shipshape).
- [Microsoft Edge background policy](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-browser-policies/backgroundmodeenabled) and [startup boost](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/startupboostenabled).
- [Microsoft app-background policy](https://learn.microsoft.com/windows/client-management/mdm/policy-csp-privacy#letappsruninbackground), including supported editions and side effects.
- [Microsoft Delivery Optimization modes](https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference).

## Verification

150 portable tests passed; two privilege-dependent symbolic-link tests were skipped. Coverage includes both games and both render-scale profiles, byte-exact restoration, UTF-8 BOM/UTF-16 preservation, comments/unrelated entries, missing/duplicate/invalid fields, no invented Rust commands, running-game/stale/replaced-file rejection, failed journals, ignored writes, backup validation, conflict-aware merged restore, overlapping profiles and low-end profile failure/grouped undo.

WPF checks cover 14 pages, nine dropdowns and eight compact layouts. Isolated UI tests click apply/restore for Fortnite, Rust and the low-end profile, all 21 direct switches and all seven service controls. The real Windows file backend is tested against disposable Rust-shaped config fixtures for write/readback, merge restore, stale contents, replaced file identity and read-only protection. Installed Fortnite and Rust files are read/parsed/hashed without changing them. All 28 generated service script variants parse successfully. Three packaging tests cover distribution validation.

No installed game was launched and no game FPS improvement, visual equivalence or ban outcome was measured. Native registry/service/priority write acceptance remains pending across supported systems. The prior real Windows restore-point attempt remains unverified after elevation was declined; it was not repeated. Existing releases and local user history are preserved.
