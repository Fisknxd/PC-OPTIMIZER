# ◈ FrameForge

A native Windows gaming and system-management toolkit built with .NET 10 and WPF. FrameForge reads real hardware data, offers reviewed configuration changes, and preserves the original state of supported settings. It never promises a universal FPS increase.

**Status: 0.8.0 engineering preview.** Cross-compilation and portable tests do not establish Windows runtime safety or MVP acceptance. Run the [Windows acceptance checklist](docs/WINDOWS-ACCEPTANCE.md) in disposable VMs before using mutating features on a primary PC. See [scope and limitations](docs/STATUS.md). Advanced phases remain gated on MVP acceptance.

## What works in the implementation

- Responsive search without rebuilding cards, bounded background Windows reads, remembered navigation/input, clearer filters and labels, batched live logs, and worker-prepared large benchmark imports. Stronger browser policies recheck prerequisites immediately before a write.

- Session-based recovery, compensated grouped registry writes, persistent profile selections, automatic prerequisites, and direct PresentMon CSV imports. See the 0.8.0 notes for exact supported formats and recovery limits.

- Thirty backed-up switches, short setup guides based on the supplied feature list, optional administrator launch, native Windows restore-point creation with verification, and a custom app icon. See [0.8.0 changes and validation limits](docs/UPDATE-0.8.0.md).

- **Services** adds seven reviewed Pause/restore switches with saved startup modes, dependency checks and verified undo. **App priority** offers bounded desktop-app scheduling experiments. **Watch changes** shows actual native events and streamed PowerShell output. **Potato Settings** applies saved Fortnite/Rust graphics profiles in one click with file backups and merge-safe restore. **Low-end PC** applies selected background-work and desktop changes with grouped undo.

- **Tweaks** exposes real On/Off switches for file extensions, hidden files and app theme, with live state, one-step confirmation, verification and exact undo. Manual Windows controls are grouped separately. Review mode is available beside the controls.

- Home puts Low-end PC and Potato Settings first. A gaming-focused view, compact category picker, expandable tradeoffs, dependency checks and already-configured states simplify choosing changes. New browser memory, indexing and update-download controls use documented policies. Two native plugged-in power checks preserve the current plan and battery values.
- Opt-in native CPU/RAM monitoring at one-second intervals, a rolling 120-sample chart and CSV export. Monitoring stops on navigation, other actions and closing.
- Benchmark library with labels, test notes, hardware context, baseline comparisons and CSV summary export. Resizing charts preserve isolated stutters. Import errors include line numbers; BOM and quoted values work.
- Cancellable hardware scans with progress. Cancelling retains the previous complete snapshot.

- On-demand WMI inventory: CPU, GPU vendor/driver, memory/modules, storage, board/BIOS, startup entries, display report, network/DNS and security-provider inventory. Unsupported data is labeled Unavailable.
- Searchable compatibility-aware catalog, Basic/More choices/Experiments option levels, individual previews, and a hardware-aware manual review plan plus a small optional Smart Optimize change list. No experimental actions run automatically.
- Thirty reversible gaming, mouse, notification and desktop switches, plus a desktop-only Balanced-plan experiment. **These are not advertised as proven gaming improvements.** Gaming/capture/GPU/startup/privacy controls open the supported Windows interfaces for manual review.
- Write-ahead, atomically saved JSON history, read-back verification, individual/session rollback, interruption recovery, conflict detection, audit export and subjective impact ratings.
- Per-user Appx inventory with narrow optional and user-decision lists; protected components and packages with dependents are blocked. Explicit individual uninstall confirmation, inventory backup, result verification. No automatic uninstall rollback.
- Preview and selected deletion of old regular .tmp/.temp/.log files in the standard user TEMP. Age/path/link/metadata checks, Windows file-identity verification, locked-file skipping, audit trail. No browser/shader cache deletion, personal-folder cleanup or automatic mass deletion.
- AMD/NVIDIA/Intel-specific guidance, seven game profiles, selectable gaming GPU, limited local game marker detection, measured resource snapshots, benchmark-history persistence and frame-time CSV import/chart. No injection or protected game-file changes; supported user graphics configurations are edited explicitly through Potato Settings.
- Bounded user-selected ICMP diagnostics, built-in Windows utilities, and official driver links. No proprietary installers bundled.

## Build and test

Windows 10 22H2 (build 19045) / Windows 11 x64, .NET **SDK 10.0.402** to develop. The packaged application includes its own runtime. The supported OS list does not extend Microsoft's lifecycle/security support for Windows 10.

```powershell
dotnet restore FrameForge.sln --locked-mode
dotnet build FrameForge.sln -c Release --no-restore
dotnet test tests/FrameForge.Tests -c Release --no-build
dotnet run --project src/FrameForge.App -c Release
```

For Linux development, first `source scripts/dev-env.sh` and test the portable test project. WPF execution requires Windows.

```bash
bash scripts/build-release.sh
```

On Windows, `pwsh -File scripts/build-release.ps1`. Both produce `artifacts/FrameForge-Desktop.zip` (all files plus an update helper that preserves the previous application folder in a dated backup), `artifacts/FrameForge-Source.zip`, and `artifacts/FrameForge-Universal.zip`, a self-contained Windows x64 folder package, README, documentation, notices and SHA-256 manifest. Windows CI additionally launches and closes the WPF window with `--smoke-test` (no system writes). A smoke test is not functional acceptance.

## Safety and data

Requests administrator permission at launch; cancelling keeps standard access. FrameForge installs no scheduled tasks or background service and includes no analytics, arbitrary shell input or automatic driver installation. No changes to security software, Update, servicing, affinity, HPET, timers, voltages or anti-cheat. External controls are explicitly distinguished from verified internal changes.

State is stored under `%LOCALAPPDATA%\FrameForge`. Never delete the journal while relying on undo. History can contain local paths and machine details; review before sharing. No secrets are requested. Prepared/failed records remain visible after interrupted operations. Registry backup types are validated; unknown operations cannot be executed through history.

Start with **Scan PC**, review **Tweaks**, and approve only changes you understand. Use **Recovery** to undo supported changes. Cleanup and app removal are irreversible in FrameForge and require separate confirmation. Refresh Explorer/sign out manually if a shell preference is not yet visible; FrameForge does not restart the shell.

## Documentation

- [0.8.0 changes and pending checks](docs/UPDATE-0.8.0.md).
- [Development plan](docs/PLAN.md) — architecture, risks, dependencies, milestones and acceptance gates.
- [Evidence register](docs/EVIDENCE.md) — mechanisms, exact registry keys, vendor constraints and source limitations.
- [Developer guide](docs/DEVELOPMENT.md) — extension contracts, tests and release procedure.
- [Windows validation](docs/WINDOWS-ACCEPTANCE.md) — VM and real-hardware checklist.
- [User guide](README.txt), [implementation status](docs/STATUS.md), [MIT license](LICENSE).

Performance varies by workload. No FrameForge FPS improvement has been measured or claimed.
