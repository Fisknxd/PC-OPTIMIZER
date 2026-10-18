# FrameForge 0.3.0

## What changed

The app now asks Windows for administrator access at launch. Only Windows' own approval dialog can grant it. Cancelling opens the app with standard access and locks restore-point creation. The access level is shown in the sidebar and Settings. Relaunch checks that elevation did not switch to a different Windows account, so user preferences are never silently written to another person's account.

The new F/bolt icon is embedded in the executable and the WPF window, including the title bar and taskbar. SVG and high-resolution PNG sources are included. Tweaks use compact two-column cards on larger windows and a single column on smaller ones. Gaming and mouse settings appear first. Recovery, Game setup, Apps, Connection and Settings use shorter descriptions, with technical details and step-by-step guides behind expanders.

## Twelve real switches

Every switch reads current Windows values, offers a short confirmation, saves the previous setting before writing, verifies the result and supports exact original-value undo.

- Game Mode: changes the current user's Game Mode preference. Restart the game and compare; no universal FPS boost is promised.
- Windows game recording: changes both current-user game-capture preferences. Off stops Windows game recording, including clips, rather than claiming to disable only background capture. Each original value, including absence, is saved separately.
- Controller opens Game Bar: changes whether the controller's Xbox button opens Game Bar.
- Mouse acceleration: uses the Windows mouse API. Off removes desktop acceleration; On uses the standard Windows curve. Undo preserves custom original thresholds. Games with raw input usually ignore this setting.
- Menu animations: uses the Windows desktop API to enable or remove menu opening animation. This affects desktop feel, not game latency.
- Windows app animations: enables or removes supported Windows interface animation through the desktop API.
- Faster desktop menus: switches submenu delay to 100 ms. Off uses the usual 400 ms unless an existing custom disabled value is already in place. Undo restores the precise prior delay. No in-game effect is claimed.
- App notification pop-ups: changes the current user's app-banner preference. Windows may need sign-out to reflect it; security services are left alone.
- Game Bar startup tips: changes the older Game Bar welcome-panel preference. This is cosmetic and may not be used by newer Game Bar versions. The control is unavailable when Windows does not already expose that older value.
- File extensions, hidden files and dark app theme: the earlier convenience switches remain, with shorter explanations.

Custom or mixed states are labelled rather than guessed. Unexpected value types are blocked. Machine or account policy can override user preferences; changing a registry preference does not guarantee that every Windows version or game uses it.

## Windows restore points

Recovery creates a real local Windows System Restore point through SRSetRestorePointW, completes the native transaction, then checks the SystemRestore list for a new sequence number and the exact unique description. A successful API return alone is never presented as proof of creation. Windows manages the storage on the PC; the app's JSON history is not a substitute for a restore point.

To avoid Windows silently reusing today's point, creation temporarily sets the documented creation-frequency value to zero. Its original value or absence is saved durably before changing it and restored in a finally block. An interrupted operation retains a recovery record. Recovery can restore that original timing on the next run, with conflict checks if another program changed it.

Administrator access and enabled System Protection for the Windows drive are required. The app provides System Protection and System Restore buttons, a refreshable local point list, and clear failure messages. It never quietly enables protection, deletes points, restores the whole PC, or changes a point-retention policy. A restore point is not a backup of personal files or a guarantee of reinstalling removed Store apps.

## The attached feature list

Direct controls cover Game Mode, capture, mouse acceleration, menu delay, visual animation, notification banners and Game Bar prompts, alongside the existing switches. The list's duplicate registry/user-interface entries share one control.

Thirty short setup guides cover refresh rate, Windows 11 windowed-game optimization, per-game GPU choice, startup apps, chipset drivers, Smart Access Memory, Fortnite V-Sync/render mode/shadows/effects/blur/FPS caps/counter, AMD driver/Chill/sync/FreeSync/overlay/shader cache choices, mouse connection/polling/hubs, Discord, Widgets, cleanup, SSD maintenance, Ethernet, router queues and DNS. Changes that must be made in a game, driver tool, monitor or router are labelled manual guides and do not appear as fake automatic switches.

The optional Store-app list adds Media Player, Movies & TV, Feedback Hub and Tips. Individual confirmation, dependency checks and protection of shared Windows/security components remain. Cleanup stays limited to selected old regular temporary files; no shader-cache purges or personal-file sweeps were added.

## Verification

Portable tests exercise every toggle in both directions, custom mouse-state undo, grouped recording backups, stale previews, no-op behavior and original recovery. Restore-point coordinator tests exercise verified creation, false success, API failure, access denial, creation-frequency preservation, interrupted recovery and external conflicts. Windows UI tests click all twelve controls, test cancellation and undo, render compact layouts and the real confirmation window, and verify the icon loads. Read-only Windows integration reads all native desktop settings and hardware/resource counters.

Real administrator approval and local restore-point creation require an interactive Windows session. See `artifacts/ui-checks/real-restore-point.txt` in the development build for the actual local creation result. A failed or unavailable real test must not be described as verified.

## Primary references

- [Windows settings reference](https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-windows-11)
- [Windows desktop preferences API](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-systemparametersinfoa)
- [Native restore-point creation and frequency behavior](https://learn.microsoft.com/en-us/windows/win32/sr/calling-srsetrestorepoint)
- [CreateRestorePoint behavior](https://learn.microsoft.com/en-us/windows/win32/sr/createrestorepoint-systemrestore)
- [Windowed game optimization](https://support.microsoft.com/en-us/windows/hardware/display-graphics/optimizations-for-windowed-games-in-windows-11)
