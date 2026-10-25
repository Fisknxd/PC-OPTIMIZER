# Windows acceptance gate — not executed on the Linux development machine

Run on disposable Windows 10 22H2 and Windows 11 x64 VMs with snapshots. Do not run destructive tests on a primary PC. Record exact OS build, runtime, VM/hardware, GPU and driver, command result and screenshots. Mark each item passed/failed/skipped separately. Build/tests passing is not MVP acceptance.

- [ ] Extract all ZIP files; compare SHA256SUMS.txt; launch FrameForge.exe without an SDK or administrator token.
- [ ] Verify all pages, resizing, DPI scaling, keyboard navigation, focus, contrast, long names and dialogs. Confirm no fake data or clickable unimplemented features.
- [ ] Scan; compare CPU/cores/RAM/board/storage/GPU driver with System Information and Windows Settings. Verify unavailable WMI providers produce warnings. Test standard/non-admin and policy-managed users.
- [ ] AMD, NVIDIA, Intel, hybrid GPU and laptop scans select compatible guidance; choose intended gaming GPU; unknown hardware never receives invented data. Check X3D guidance on real hardware.
- [ ] Search/filter by feature/category/vendor; confirm restart and not-applicable filters and Safe/Balanced/Advanced restrictions.
- [ ] Preview an absent extension DWORD, apply, inspect HKCU exact value, confirm journal original absence, undo and confirm absence. Repeat with existing DWORD 0/1. Unexpected REG_SZ must be refused. No machine-wide writes.
- [ ] Apply dark theme / hidden files with explicit selection; verify values and Explorer behavior; undo. No automatic Explorer restart.
- [ ] On a desktop VM, choose Advanced and preview Balanced power plan, verify powercfg state and undo original GUID. Verify missing plan fails; laptop/unknown chassis blocks automatic plan writes.
- [ ] Change a setting externally after applying; undo must report conflict without overwriting. Undo newest session first. Inject registry policy denial and verify recoverable failure.
- [ ] Terminate only this test instance after prepared history persists; restart, inspect prepared/failed record, undo safely. Corrupt a copied history document and verify fail-closed startup without data loss; recover .bak manually.
- [ ] TEMP preview excludes new files, symlinks/junctions (including ancestor links), read-only/system files, shader/browser/save directories, hard links and outside-root paths. Confirm custom TEMP disables deletion.
- [ ] Select a disposable old file; preview, edit/replace it, then approve; changed identity/metadata must be skipped. Lock a file with no sharing and confirm skip. Confirm unselected files survive. Record deletion audit and no false rollback claim.
- [ ] Inventory Appx as standard user; verify Store, App Installer, frameworks, security, unknown packages and dependencies are protected. On a disposable user profile only, explicitly remove one unused allowlisted app and verify absence, clear irreversible notice and audit. No all-user/deprovision operation.
- [ ] Verify no hidden background service, scheduled task, updater, analytics or startup entry. Closing is blocked during an active operation; no arbitrary process termination.
- [ ] Snapshot comparison uses comparable workloads. Import known frame-duration CSV; verify formulas, NaN/zero rejection and source labeling. Live FPS/temperature displays remain Unavailable.
- [ ] Test local game marker detection, manual selection and all profiles; no game files/anti-cheat changes. Verify vendor driver links and Settings URIs on both OS versions. External setting changes are not mislabeled backed-up.
- [ ] User-selected ICMP test against a known endpoint; distinguish filtered responses from disconnection. No DNS/TCP/security changes.
- [ ] Measure idle app working set/CPU and action latency; no continuous polling. Record actual results rather than asserting low overhead without evidence.
- [ ] Collect controlled actual game benchmarks only on supported physical hardware; record scene, repetitions, graphics, cap, driver/game versions and average/1% low/frame-time data. No gains may be claimed from lower process count alone.

Phase 2/3 advanced behavior and stable distribution remain gated until the required MVP items pass. Live sensor integrations and auto-edit game profiles need their own compatibility/integration tests before implementation is exposed.
