# 0.1.2 — simpler controls

- Quick switches are the default feature view. File extensions, hidden files (Balanced mode), and app dark theme have real On/Off controls based on current HKCU values.
- A click is approval for that individual switch. The original value/absence is durably journaled before either an On or Off write, and the result is read back. Unknown values/types are refused. No background bulk changes.
- Off explicitly writes the disabled value. Undo restores the exact original value, including an absent Windows-default preference. External changes are protected from stale previews and restore overwrites.
- Windows default, On, Off and Unavailable are distinct states. Controls refresh after changes, scans and returning to the window.
- External Settings/vendor pages remain clearly labeled Open actions. Power-plan experiments still need review; app removal and cleanup still need irreversible-action confirmation. No fake on/off controls for these actions.
- Favorites persist across sessions; one-click Undo is visible below every page; Ctrl+K searches, Escape clears the search, and Ctrl+Z outside text fields undoes the latest saved change.
- Dashboard shortcuts, shorter cards, optional details, a dismissible tip, app search, protected-app filtering, and cleanup Select all/Clear selection reduce repetitive steps.

Registry switch values:

| ID | HKCU target | On | Off |
|---|---|---|---|
| extensions | Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced / HideFileExt | DWORD 0 | DWORD 1 |
| hidden | same / Hidden | DWORD 1 | DWORD 2 |
| dark | Software\Microsoft\Windows\CurrentVersion\Themes\Personalize / AppsUseLightTheme | DWORD 0 | DWORD 1 |

These are Windows convenience preferences, not measured FPS optimizations. They do not disable security features, protected-file hiding, services, or anti-cheat. Explorer may need manual refresh/sign-out.

Validation adds portable switch-direction, backup/undo, stale-state, failure, malformed-value and preference-migration tests. Windows UI checks exercise actual WPF toggle/Undo/favorite controls using an isolated in-memory settings adapter, then render all pages and check dropdown contrast. UI automation is not real registry/Appx/power/physical-hardware acceptance. Full Windows/hardware acceptance remains docs/WINDOWS-ACCEPTANCE.md.
