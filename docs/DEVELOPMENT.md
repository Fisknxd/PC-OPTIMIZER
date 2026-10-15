# Developer guide

Use the existing checkout. Cloud tasks are already isolated; do not create a worktree unless requested.

Projects: Core (net10.0, portable policy and execution), Windows (net10.0-windows, platform boundary), App (WPF presentation, async orchestration and observable view model), Tests (portable xUnit). `MainWindow` contains view composition; operations and policies live outside the UI. JSON is local storage, not a scripting mechanism.

SDK is pinned in global.json. Restore uses checked-in NuGet lockfiles. To intentionally update dependencies, run a non-locked restore, review the lockfile diff, then restore with --locked-mode. The repository includes no secrets. Windows SDK implicit references are restored through NuGet. Never disable TLS, package verification or change checksums to bypass a mismatch.

On Windows:

```
dotnet restore FrameForge.sln --locked-mode
dotnet build FrameForge.sln -c Release --no-restore
dotnet test tests/FrameForge.Tests -c Release --no-build
dotnet run --project src/FrameForge.App -c Release
pwsh -File scripts/build-release.ps1
```

On the prepared Linux cloud machine: `source scripts/dev-env.sh`, then the same build and portable test commands, and `bash scripts/build-release.sh`. WPF cross-targeting is enabled; execution requires Windows. XDG_RUNTIME_DIR is necessary because .NET 10 named mutexes otherwise use the read-only user home. No service is required.

## Adding an operation

1. Establish documented mechanism, OS/hardware/version constraints, and credible evidence. Separate convenience from performance and experiments. Record sources in EVIDENCE.md.
2. Add a compiled Catalog entry with a stable ID, risk, kind, exact target, benefit/evidence, restart/admin metadata and compatibility. Do not add arbitrary commands or imported scripts.
3. Extend the platform adapter only for explicit, validated targets and typed states. Read exact existence/type/value before applying; preserve absence. Check a protected-system deny policy before adding any future service/module operation.
4. Implement verification and rollback. The operation engine must persist the original before touching Windows. Refuse stale previews and restore conflicts. Do not discard failed/prepared history.
5. Add fake-boundary tests for failures, permission refusal, journal-before-write, rollback and OS/hardware mismatch. Add disposable Windows VM integration steps. Never execute destructive host changes in unit tests.

Power plans use powercfg, parse GUIDs independently of language, require an existing target plan, capture original GUID, verify and restore. Laptop/unknown-chassis automatic power experiments are blocked. User shell settings only support DWORD/absent backups; an unexpected registry type is rejected.

Cleanup on Windows opens a nonshared native handle, validates previewed volume/file identity, size, timestamp, regular-file attributes and single hard link, then marks THAT handle for deletion. No path-based deletion after approval. Ancestor reparse checks and conservative age/exclusion policy run again. External concurrent operations, filesystem/provider bugs and hardware failures still require VM testing and normal backups. Linux cleanup execution exists only for isolated temporary-directory policy tests.

Appx uses fixed encoded PowerShell scripts with validated package identities, no profile, noninteractive execution, bounded timeout and captured exit status. Inventory and policy are refreshed before removal. Unknown packages are protected. A timed-out removal can complete; rescan before retrying. Never install a package automatically as an alleged rollback.

## Build/release

build-release scripts restore with locked mode, build, run portable tests, publish self-contained win-x64 and invoke package.py. The package validator requires the native x64 PE launcher, dependency/runtime files, README, license and documentation. ZIP contains SHA256SUMS.txt and third-party notices. No driver binary is bundled.

CI builds/tests on Ubuntu and Windows, performs a nondestructive Windows WPF launch/close smoke test, packages on Windows, and uploads artifacts. A v* tag triggers release publication only after that job succeeds. CI execution, tag creation and publication are not implied by writing the workflow. Add code signing and complete VM/hardware acceptance before distributing as stable.
