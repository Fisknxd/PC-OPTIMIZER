# ◈ FrameForge

A native Windows gaming and system-management toolkit built with .NET 10 and WPF. FrameForge reads real hardware data, offers reviewed configuration changes, and preserves the original state of supported settings. It never promises a universal FPS increase.

**Status: 0.1.0 engineering preview.** Cross-compilation and portable tests do not establish Windows runtime safety or MVP acceptance. Run the [Windows acceptance checklist](docs/WINDOWS-ACCEPTANCE.md) in disposable VMs before using mutating features on a primary PC. See [scope and limitations](docs/STATUS.md). Advanced phases remain gated on MVP acceptance.

## What works in the implementation

- On-demand WMI inventory: CPU, GPU vendor/driver, memory/modules, storage, board/BIOS, startup entries, display report, network/DNS and security-provider inventory. Unsupported data is labeled Unavailable.
- Searchable compatibility-aware catalog, Safe/Balanced/Advanced review modes, individual previews, and a hardware-aware manual review plan plus a small optional Smart Optimize change list. No experimental actions run automatically.
- Three reversible HKCU shell preferences (file extensions, hidden files and app theme), plus a desktop-only Balanced-plan experiment. **These are not advertised as proven gaming improvements.** Gaming/capture/GPU/startup/privacy controls open the supported Windows interfaces for manual review.
- Write-ahead, atomically saved JSON history, read-back verification, individual/session rollback, interruption recovery, conflict detection, audit export and subjective impact ratings.
- Per-user Appx inventory with narrow optional and user-decision lists; protected components and packages with dependents are blocked. Explicit individual uninstall confirmation, inventory backup, result verification. No automatic uninstall rollback.
- Preview and selected deletion of old regular .tmp/.temp/.log files in the standard user TEMP. Age/path/link/metadata checks, Windows file-identity verification, locked-file skipping, audit trail. No browser/shader cache deletion, personal-folder cleanup or automatic mass deletion.
- AMD/NVIDIA/Intel-specific guidance, seven game profiles, selectable gaming GPU, limited local game marker detection, measured resource snapshots, benchmark-history persistence and frame-time CSV import/chart. No injection or game-file modifications.
- Bounded user-selected ICMP diagnostics, built-in Windows utilities, and official driver links. No proprietary installers bundled.

## Build and test

Windows 10 22H2 (build 19045) / Windows 11 x64, .NET **SDK 10.0.402** to develop. The packaged application includes its own runtime. The supported OS list does not extend Microsoft's lifecycle/security support for Windows 10.

```powershell
dotnet restore FrameForge.sln --locked-mode
dotnet build FrameForge.sln -c Release --no-restore
dotnet test tests/FrameForge.Tests -c Release --no-build
dotnet run --project src/FrameForge.App -c Release
```

On this Linux cloud machine, first `source scripts/dev-env.sh`; test only the portable test project. WPF execution requires Windows. The cloud setup uses the existing checkout; no worktree is needed.

```bash
bash scripts/build-release.sh
```

On Windows, `pwsh -File scripts/build-release.ps1`. Both produce `artifacts/FrameForge-Desktop.zip` (all files plus a helper that copies the extracted folder to Desktop without overwriting), `artifacts/FrameForge-Source.zip`, and `artifacts/FrameForge-Universal.zip`, a self-contained Windows x64 folder package, README, documentation, notices and SHA-256 manifest. Windows CI additionally launches and closes the WPF window with `--smoke-test` (no system writes). A smoke test is not functional acceptance.

## Safety and data

Runs without elevation; no scheduled tasks, services, analytics, arbitrary shell configuration or automatic driver installations. No changes to security software, Update, servicing, affinity, HPET, timers, voltages or anti-cheat. External controls are explicitly distinguished from verified internal changes.

State is stored under `%LOCALAPPDATA%\FrameForge`. Never delete the journal while relying on undo. History can contain local paths and machine details; review before sharing. No secrets are requested. Prepared/failed records remain visible after interrupted operations. Registry backup types are validated; unknown operations cannot be executed through history.

Start with **Scan PC**, review **Optimize**, and approve only changes you understand. Use **Restore** to undo supported changes. Cleanup and app removal are irreversible in FrameForge and require separate confirmation. Refresh Explorer/sign out manually if a shell preference is not yet visible; FrameForge does not restart the shell.

## Documentation

- [Development plan](docs/PLAN.md) — architecture, risks, dependencies, milestones and acceptance gates.
- [Evidence register](docs/EVIDENCE.md) — mechanisms, exact registry keys, vendor constraints and source limitations.
- [Developer guide](docs/DEVELOPMENT.md) — extension contracts, tests and release procedure.
- [Windows validation](docs/WINDOWS-ACCEPTANCE.md) — VM and real-hardware checklist.
- [User guide](README.txt), [implementation status](docs/STATUS.md), [MIT license](LICENSE).

Performance varies by workload. No FrameForge FPS improvement has been measured or claimed.
