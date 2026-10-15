# Mechanisms and evidence register

Reviewed 2026-10-15. Sources establish behavior and compatibility, not measured FrameForge FPS gains.

| Area | Official source | Decision |
|---|---|---|
| Runtime | https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md | .NET 10 is LTS. Ship self-contained; no runtime installer needed. |
| CPU | https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/CIMWin32Prov/win32-processor.md | WMI reports model, cores, logical processors, clocks/cache. Reported clock is not a thermal/bottleneck measurement. |
| Appx removal | https://github.com/MicrosoftDocs/windows-powershell-docs/blob/main/docset/winserver2025-ps/Appx/Remove-AppxPackage.md | Use only current-user explicit removal; no all-user switch and no guaranteed rollback. |
| Appx inventory | https://github.com/MicrosoftDocs/windows-powershell-docs/blob/main/docset/winserver2025-ps/Appx/Get-AppxPackage.md | Current-user inventory requires no all-user administrative scope. |
| GPU inventory | https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/CIMWin32Prov/win32-videocontroller.md | AdapterRAM is uint32; exact dedicated VRAM remains unavailable. |
| Filesystem | https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/symbolic-links.md | Links point elsewhere. Reject reparse paths during cleanup, including ancestor directories. |
| NVIDIA | https://github.com/NVIDIAGameWorks/Streamline/blob/main/docs/ProgrammingGuideReflex.md | Reflex low-latency requires supported in-game integration; recommendations are manual and game-specific. No injection or global Reflex switch. |
| AMD | https://github.com/GPUOpen-LibrariesAndSDKs/AntiLag2-SDK/blob/main/README.md | Anti-Lag 2 requires game developer integration, compatible GPU/driver/API. Never force unsupported configuration. |

Additional API references for developer review (direct pages were blocked by the onboarding proxy, so these have not been independently fetched here):

- https://learn.microsoft.com/windows-hardware/design/device-experiences/powercfg-command-line-options — enumerate plans, capture active GUID, set existing plan, verify, restore original; controlled experiment only.
- https://learn.microsoft.com/windows/apps/develop/launch/launch-settings-app — official Settings URIs, preferred for gaming/privacy/driver-dependent options.

Registry convenience operations (Windows 10/11 shell preferences; practical behavior still needs VM validation):

| HKCU subkey / DWORD | Proposed | Purpose / side effect | Reversal |
|---|---|---|---|
| Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced / HideFileExt | 0 | Show known extensions; changes Explorer presentation, no FPS claim | Restore exact original value/type or delete originally absent value |
| same / Hidden | 1 | Show hidden items; may expose distracting application files, never changes protected OS file visibility | Same |
| Software\Microsoft\Windows\CurrentVersion\Themes\Personalize / AppsUseLightTheme | 0 | Dark application theme; theme-aware apps only | Same |

These shell implementation preferences are isolated from performance actions and labeled observational evidence. Unsupported policy-managed writes may fail verification. Explorer may need refreshing/sign-out; FrameForge never restarts Explorer forcibly.

Game Mode, capture, HAGS, GPU preference, refresh rate, startup, privacy and services are opened in supported Windows Settings/management interfaces. They are not silently altered. No changes to Defender, Update, Store dependencies, anti-cheat, system services, arbitrary network registry values, overclocking, or voltage.
