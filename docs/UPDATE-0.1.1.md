# 0.1.1 engineering preview update

Changes requested from actual Windows screenshots:

- Explicit dark main-window background and font properties. An implicit Window style did not reach the derived MainWindow, which left pale text on a white canvas.
- Custom dark dropdown and item templates, hover/selection/focus states, dark list selections, readable disabled controls and search placeholders.
- Larger body/caption/control text, more line spacing, wrapping action rows and a responsive search row. Long technical catalog metadata is available in an expandable detail section.
- Initial read-only scan, a visible progress indicator and shorter scan feedback. No background polling is added.
- Hardware health findings use actual available RAM, CPU samples and typed disk free-space readings. They are review prompts, never invented FPS gains or automatic process termination.
- Update-FrameForge.cmd launches a compiled update flow. It can update the existing Desktop app or a selected installation. The entire previous folder is preserved in a dated backup; copying is staged and hashed before swapping directories. Unrelated folders, links and nested-source updates are refused. LocalAppData settings/history are not changed.
- The Windows validation command renders all menus, opens dropdowns, verifies a 4.5:1 text contrast minimum, and saves screenshots. The Windows release script/workflow gates packaging on this check.

Validation scope:

- The portable test suite includes the previous settings/rollback/protection/cleanup checks plus 12 updater and health tests.
- Linux can compile WPF and test filesystem/policy logic but cannot execute the Windows UI, Appx, registry, powercfg, native deletion, updater dialogs or Windows system changes.
- The user requested a new preview download after local testing. The update is prepared on a separate preview branch; main and the previous release remain unchanged. The preview release workflow requires the automated Windows menu/contrast checks to pass before publishing. Physical-hardware/system-operation acceptance remains pending and is not implied by publication.
- Run `pwsh -File scripts/build-release.ps1` on a disposable Windows x64 machine for the automated Windows/menu/packaging checks. Confirm actual screenshots match a readable dark theme, keyboard navigation works, and test update/install against a copy of the old app.
- Before claiming every feature verified, complete docs/WINDOWS-ACCEPTANCE.md, including reviewed apply/undo, stale/conflicting changes, cleanup locked/link/replaced-file cases, protected Appx behavior and real vendor/laptop scans. No fake-boundary unit suite establishes physical hardware behavior.

Local verification results for this update:

| Check | Outcome |
|---|---|
| Locked restore and Release solution build | Passed, zero warnings/errors |
| Portable .NET suite | 50 passed, 0 failed, 0 skipped |
| Packaging failure-case suite | 3 passed |
| Self-contained Windows x64 publish | Passed |
| ZIP completeness, CRC and SHA-256 verification | Passed |
| Actual Windows menu rendering/contrast/screenshots | Unrun: Windows host required |
| Actual Windows updater dialog/install/rollback | Unrun: Windows host required |
| Full Windows system-operation/hardware acceptance | Unrun: disposable Windows 10/11 and supported hardware required |
| Preview publication | Gated by the Windows CI checks; verify the linked workflow/release outcome |
