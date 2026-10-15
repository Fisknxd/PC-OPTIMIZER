FRAMEFORGE 0.1.1 — WINDOWS X64 ENGINEERING PREVIEW

WHAT IT DOES
FrameForge inventories hardware and Windows configuration, presents reviewed
system-management options, records supported changes, and offers restore.
This is an engineering preview, not a production-validated optimizer.
Gaming performance gains vary. No FrameForge FPS gain has been measured.

REQUIREMENTS / INSTALL
Windows 10 22H2 (build 19045+) or Windows 11, x64. Use a Microsoft-supported
OS/security configuration. No separate .NET runtime required for the ZIP.
Allow at least 500 MB disk space and 4 GB RAM for basic use; gaming needs vary.
Extract the ENTIRE ZIP into Downloads. Close the old app, then run
Update-FrameForge.cmd to update the existing Desktop installation or choose
an existing application folder. The old folder is preserved in a dated backup.
LocalAppData settings and history are kept. For direct launch, open the inner
FrameForge folder and run FrameForge.exe. Keep all DLLs
and resources together. This is a portable folder, not a single-file EXE.
The executable is unsigned; no publisher identity/signing is claimed.

FIRST LAUNCH
Run in a disposable Windows VM first. Click Scan PC. WMI reads CPU, GPU/driver,
RAM, storage and Windows information. Unknown data is shown as Unavailable.
There are no fabricated GPU memory, temperature or FPS readings.
No automatic network access, startup registration, or persistent service.

SMART OPTIMIZE AND MODES
Smart Optimize matches manual gaming/startup/vendor reviews to the scanned PC.
Its automatic changes include only low-risk reversible conveniences.
It does NOT promise to accelerate games. Read each item, select it explicitly,
and approve. Safe mode blocks review/experimental changes. Balanced allows
review options; Advanced additionally exposes a desktop power-plan experiment.
Compatibility and safety checks apply in every mode. Advanced is not a bypass.
Optimize includes supported Windows Settings shortcuts; changes made there
are manual and are NOT automatically backed up or verified by FrameForge.

HARDWARE / GAMES
GPU-specific links/guidance appear for AMD, NVIDIA or Intel hardware. Choose
a gaming GPU on multi-GPU PCs. Reflex and Anti-Lag 2 require supported games,
GPUs and drivers; FrameForge cannot enable them globally. X3D recommendations
avoid unsupported scheduler/affinity/parking changes. Laptop power changes
are manual to respect OEM/thermal restrictions.
Seven game guidance profiles are provided. Automatic game-configuration edits
are unavailable. Limited Steam/user-folder detection does not verify installs.
Use the same scene/settings/version for benchmarks. Monitor can import CSV:
FrameTimeMs
16.67
16.50
... at least 100 positive frame-duration values in milliseconds.
No live FPS overlay is included. No game injection or anti-cheat modification.

DEBLOAT
Scan packages, select ONE app, read publisher/dependencies/classification,
and confirm removal explicitly. This affects only the current Windows user.
Unknown packages and essential/framework/driver/Store/security dependencies
are protected. Some consumer packages are optional; Xbox/Teams/Phone Link
need a personal decision. OneDrive and traditional apps use Installed apps.
Never assume an application is unnecessary because of its publisher.
Removal can lose data/features; automatic undo is unavailable. Reinstall via
the official Store/publisher only if supported and still available.

CLEANUP
Preview regular .tmp/.temp/.log files older than seven days under standard user TEMP.
Select rows (Ctrl/Shift for multiple), review paths, then confirm permanent
deletion. Locked, changed, linked, protected and known sensitive/cache files
are skipped. Custom TEMP paths are not supported for deletion. Documents,
saves and browser/shader caches are not intentional cleanup targets.
Scan capped at 50,000 candidates; inaccessible paths may be skipped.
Use Windows Storage Sense for other categories. Cleanup is irreversible and
is not an automatic FPS boost. Close apps whose temporary data you need.

RESTORE / DATA
Supported setting changes save exact original state BEFORE writes and verify
afterwards. Restore can undo an individual record or session, newest first.
It refuses external-state conflicts and stops session restore on failure.
Prepared/failed/restoring records survive interruption; rescan and retry undo.
Restart/refresh flags are shown per record. Explorer may require manual
refresh/sign-out. The application does not forcibly restart it.
Data: %LOCALAPPDATA%\FrameForge (preferences, history, .bak, audit, snapshots).
Export history for review. Exports can contain paths and hardware details.
Do not discard the data folder if you need rollback. A corrupt journal blocks
startup: close FrameForge, preserve the whole folder, inspect the .bak and
recover manually. No executable configuration imports are supported.
Uninstalls and deleted files cannot be restored by this backup system.
Windows restore-point creation is unavailable. Maintain normal system backups.

ADMIN / TROUBLESHOOTING
The entire app is NOT elevated. Current built-in operations target user-level
settings. A policy may block a write or power plan; read the error and Restore
record rather than running broad scripts as administrator. If WMI/Appx is
unavailable, inspect scan warnings and Windows management health. No data is
invented to hide failures. Network ICMP can be filtered; no reply alone does
not prove loss of connectivity. Close other FrameForge instances first.

KNOWN LIMITATIONS
Windows runtime acceptance and actual AMD/NVIDIA hardware testing are pending.
See docs/WINDOWS-ACCEPTANCE.md and docs/STATUS.md. Temperatures, live FPS,
reliable dedicated VRAM, GPU utilization, per-process CPU/network rate,
automatic driver updates, game-file edits, maintenance scheduler and gaming
session temporary profiles are unavailable. No security/service disabling,
overclocking, timer packs or unsupported network tweaks are implemented.

PRIVACY / OFFICIAL TOOLS
No account, telemetry or automatic updater. State stays local. Opening vendor
links uses your browser; user-selected ping sends ten ICMP requests to that
host. No external utilities or proprietary installers are bundled.
AMD: https://www.amd.com/en/support
NVIDIA: https://www.nvidia.com/en-us/drivers/
Intel: https://www.intel.com/content/www/us/en/download-center/home.html
Microsoft: https://support.microsoft.com/windows
Use Tools for Task Manager, Resource Monitor, Reliability Monitor, dxdiag and
other built-in tools. Driver update availability is not automatically verified.

LICENSE
See LICENSE and licenses/ for bundled runtime/component notices.
