# FrameForge development plan

Repository inspection: no commits, tracked files, build system, or existing functionality on 2026-10-15. This is a greenfield implementation. No existing user code needs migration.

## Architecture and dependencies

- .NET 10 LTS, C# and native WPF. Windows 10 22H2 / Windows 11 x64; an OS version claim does not extend Microsoft's OS lifecycle support.
- Core: portable models, compatibility, catalog/search, execution state machine, atomic JSON journal, cleanup policy, debloat protection, benchmark parsing.
- Windows: WMI/System.Management, HKCU registry adapter, documented powercfg, fixed PowerShell Appx commands, Settings URI allowlist. No downloaded binaries, arbitrary scripts, service, startup task, or automatic elevation.
- App: low privilege WPF navigation, explicit previews, per-item selection and confirmation, async work, on-demand samples.
- Tests: xUnit, fake system boundary, temporary directories. Never modify the host Windows configuration in unit tests.
- Distribution: self-contained win-x64 folder and universal ZIP, official links instead of bundled drivers.

## Milestones and dependencies

1. Foundation: solution, core data contracts, catalog and compatibility. Requires SDK and NuGet restore.
2. MVP: scanner and unknown-value handling; dashboard/search; reviewed registry and power actions; journal persisted before writes; post-write verification; conflict-aware rollback; settings/history; cleanup preview; protected Appx inventory and explicit uninstall.
3. Diagnostics: snapshot comparison, top resource consumers, startup inventory and supported Settings management, adapter/DNS and bounded ping, hardware-specific and game-specific guidance, official utility links, QoL shortcuts. Game-file edits remain unavailable until per-game version/schema validation exists.
4. Distribution: automated tests, Windows CI smoke checks, docs, self-contained ZIP and integrity manifest.
5. Windows acceptance: launch on disposable Windows 10/11 VMs; scan real AMD/NVIDIA/multi-GPU/laptop systems; apply/restore approved settings; cleanup link/lock races; removal refusal; manual accessibility and packaging test. Linux cross-compilation is not Windows runtime validation. Do not call the MVP accepted until these checks pass.

## Operation and recovery design

Every supported mutating optimization has a stable ID, Windows compatibility guard, current and proposed typed values, benefit/evidence/risk/admin/restart metadata, read/apply/verify/restore and error reporting. Journal records before and after state to durable JSON before applying anything. Interrupted and failed operations remain restorable. Undo refuses to overwrite externally changed settings unless the current state matches the expected applied state. Serialize writes and protect history from arbitrary imported actions. Restore only compiled catalog IDs; backups are data, never executable configuration.

Power-plan selection is a controlled experiment, excluded from Smart Optimize and blocked on laptops. Registry changes are HKCU convenience preferences, never sold as FPS tweaks. Security infrastructure and essential services are not modified. No affinity, HPET, TCP, voltage, scheduler, timer or blanket service tweaks.

Cleanup is irreversible, explicitly confirmed, confined to old regular .tmp/.temp/.log files under this user's TEMP, excludes reparse points and known sensitive/cache paths, revalidates preview metadata, skips locked/changed files. Deleting user data and shader caches is not supported. Debloat is per-user and allowlisted; dependency/framework/resource/non-removable/unknown components protected by default; Xbox/OneDrive/Teams/Phone Link require user choice and are not automatically removed. No bulk unattended removals.

## Research and evidence

Read official upstream documentation through GitHub mirrors because Microsoft/vendor website requests returned proxy 403. See EVIDENCE.md. Documentation supports mechanisms, not guaranteed performance gains. No game FPS claims without matched benchmark measurements.

## Validation and release gates

Required locally: core tests exercising compatibility, unknown hardware, journal-before-write, apply/verify/rollback failure and conflict, persistence, catalog filters, cleanup exclusions and symlinks, protected packages, benchmark rejection, cross-build and ZIP structure.
Required on Windows: executable launch, real WMI/registry/powercfg/Appx results, reviewed apply/undo, locked-file handling and UI workflows. CI can build/package and run nondestructive Windows startup checks; manual disposable VM checks remain documented.
Optional: actual game FPS/1% low measurements and temperatures require supported telemetry/hardware. Unsupported measurements show Unavailable. Advanced features must not bypass MVP acceptance gates.
