# Validation record — 2026-10-15

Host: Linux x64, .NET SDK 10.0.402. SDK archive SHA-512 verified against Microsoft's official release metadata before extraction. Runtime in self-contained package: Microsoft.NETCore.App and Microsoft.WindowsDesktop.App 10.0.13.

| Check | Result |
|---|---|
| `dotnet restore FrameForge.sln --locked-mode` | Passed |
| `dotnet build FrameForge.sln -c Release --no-restore -m:2` | Passed: 0 warnings, 0 errors |
| `dotnet test tests/FrameForge.Tests -c Release --no-build` | Passed: 38, failed: 0, skipped: 0 |
| `python3 -m unittest discover -s scripts -p 'test_*.py'` | Passed: 3 packaging failure-case tests |
| `dotnet publish src/FrameForge.App -c Release -r win-x64 --self-contained true --no-restore -o artifacts/publish/win-x64` | Passed |
| `python3 scripts/package.py` | Passed: native x64 PE header, required app/runtime files, ZIP CRC and per-file SHA-256 checks |
| `bash scripts/install-cloud.sh` repeated | Passed; all four dependency lockfile hashes unchanged |
| Git state | All application files are new local files in the previously empty checkout. No commits, push, tag or release publication performed. |
| WPF launch and real Windows system operations | **Not run**: this machine is Linux |
| AMD/NVIDIA/Intel/laptop physical hardware | **Not run** |
| Actual game before/after benchmark | **Not run**; no performance-gain claim |
| GitHub Actions | Workflow written; **not run** from this task |
| Cloud configuration | `install_script` and `start_skill` saved as draft; not published or restored in a fresh task |

The suite exercises vendor identification/compatibility, unknown GPUs, laptop gating, catalog search/filters, original-state persistence before writes, journal-failure refusal, verification failure, stale preview refusal, restore conflict/refailure/retry, interrupted-operation recovery, corrupt storage refusal, preferences, Appx protection/dependency rules, cleanup age/path/symlink/sensitive-file exclusions and changed-file skipping, and benchmark calculations/invalid samples. Destructive system operations are mocked; cleanup tests use isolated temporary directories.

An early journal-error test expected IOException; the actual directory-denial exception is UnauthorizedAccessException. Its expectation was corrected while preserving the critical assertion that no system write occurs before a durable journal. The final 38-test run passes.

The built ZIP is an engineering preview. Full requested MVP acceptance remains gated by WINDOWS-ACCEPTANCE.md. Detailed later-phase omissions are listed in STATUS.md; the full product brief is PRODUCT-REQUIREMENTS.md.
