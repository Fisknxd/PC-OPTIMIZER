# PROJECT: FRAMEFORGE — Intelligent Windows Gaming Optimizer

## 1. Project Overview and Primary Objective

Develop a complete, professional, production-quality Windows 10/11 gaming optimization application called **FrameForge**.

FrameForge must be a fully functional, hardware-aware PC optimizer designed to improve gaming performance, reduce unnecessary Windows background resource consumption, improve system responsiveness, reduce avoidable latency, and provide useful Windows quality-of-life improvements.

The primary target audience is PC gamers playing demanding and competitive games such as:

- Fortnite
- Rust
- Counter-Strike 2
- Valorant
- Apex Legends
- Call of Duty
- Minecraft
- Other CPU-intensive and GPU-intensive games

The application must prioritize measurable improvements in:

1. Average FPS.
2. 1% low FPS and frame-time consistency.
3. System responsiveness.
4. CPU and RAM availability.
5. Background resource consumption.
6. Input responsiveness where improvements can be demonstrated.
7. System stability.
8. Windows usability and convenience.

**Critical requirement:** Never assume that a popular optimization tweak actually improves performance. Research the technical evidence, identify its applicable hardware and Windows versions, and only include performance tweaks with a credible mechanism and appropriate supporting evidence.

Avoid placebo optimizations, misleading FPS claims, unsafe registry modifications, and unnecessary system-service disabling.

FrameForge must be a real application with working functionality, not a visual mockup.

---

# 2. Development Strategy — MVP-First Execution

Before writing implementation code, inspect the existing GitHub repository and produce a detailed development plan.

Do not immediately begin implementing random features.

First:

1. Inspect the repository structure, existing code, dependencies, and build configuration.
2. Determine which functionality already exists and what is missing.
3. Research appropriate Windows optimization techniques using official Microsoft, AMD, NVIDIA, and other reputable technical documentation.
4. Select a suitable Windows desktop technology stack.
5. Define the MVP scope, application architecture, and feature dependencies.
6. Identify risky operations and design rollback mechanisms.
7. Establish the implementation order.
8. Create a development roadmap and milestone checklist.
9. Begin implementation after completing the plan.

Use the following development phases.

### Phase 1 — Core MVP

Deliver a working Windows desktop application containing:

- A professional user interface.
- Automatic hardware detection.
- A functional optimization dashboard.
- A searchable optimization catalog.
- Safe Windows cleanup.
- A basic Windows debloat manager.
- Hardware-specific optimization recommendations.
- Working optimization execution.
- A backup and restore system.
- Detailed explanations of available tweaks.
- Persistent settings and optimization history.

This phase must result in an actual usable application.

### Phase 2 — Advanced Gaming Optimization

Expand the MVP with:

- Advanced CPU and GPU recommendations.
- Game-specific optimization profiles.
- Performance monitoring.
- Background process analysis.
- Startup application management.
- Network diagnostics and safe optimization.
- Benchmark comparisons.
- Additional Windows optimization options.
- Advanced Windows quality-of-life features.

### Phase 3 — Final Product and Distribution

Complete:

- Automated hardware-aware configuration.
- Driver and companion-tool integration.
- Polished onboarding.
- Error handling and recovery.
- Automated tests.
- Windows installer or portable executable.
- GitHub Releases distribution.
- A ZIP package containing the optimizer, README, and appropriate companion tools or verified official download links.

**Every phase must be functional and testable before proceeding to the next.**

---

# 3. Hardware Detection and Intelligent Optimization Engine

FrameForge must automatically detect the user's PC components and Windows configuration.

Create a comprehensive system scanner.

### Hardware information

Detect and display:

**CPU**
- Manufacturer and exact processor model.
- Physical core count.
- Logical processor count.
- Base and reported current clock speeds.
- CPU architecture.
- Cache information where available.
- CPU temperature where supported.
- CPU utilization.
- Supported hardware capabilities.

**GPU**
- Manufacturer: NVIDIA, AMD, Intel, or another vendor.
- Exact GPU model.
- Dedicated VRAM.
- Current driver version.
- GPU utilization.
- GPU temperature where supported.
- Supported graphics features.
- Multiple GPU configurations.
- Integrated versus dedicated GPU.

**RAM**
- Total installed memory.
- Available and used memory.
- Memory speed where reliably available.
- Number of installed modules where supported.
- Memory utilization.
- Memory configuration information.

**Storage**
- SSD, NVMe, or HDD identification.
- Drive capacity.
- Available storage.
- Storage utilization.
- Drive health where supported.
- TRIM support and status where applicable.

**Operating System**
- Windows edition.
- Windows build and version.
- System architecture.
- Current power plan.
- Windows security configuration.
- Startup application inventory.
- Background service information.
- Installed application inventory.
- Relevant gaming configuration.

**Additional information**
- Motherboard model.
- BIOS/UEFI version.
- Network adapter.
- Active display resolution.
- Monitor refresh rate.
- Connected displays.
- Supported display features.

Use documented Windows APIs, WMI/CIM, DXGI, or other appropriate reliable interfaces.

Do not display fabricated or estimated hardware information as measured data.

Unsupported sensors must show "Unavailable" rather than misleading values.

## Hardware-Aware Optimization Rules

The application must use the detected hardware to determine which optimizations are applicable.

Examples:

- AMD GPU detected: show AMD-specific features and compatible recommendations.
- NVIDIA GPU detected: show NVIDIA-specific features and compatible recommendations.
- Intel GPU detected: show applicable Intel graphics options.
- AMD Ryzen X3D CPU detected: use an appropriate X3D-aware recommendation profile.
- Lower-memory systems: prioritize background memory consumption.
- HDD detected: prioritize appropriate storage maintenance and startup improvements.
- NVMe SSD detected: avoid unnecessary disk-related tweaks.
- Multiple GPUs detected: allow the user to select the intended gaming GPU.
- Laptop detected: account for battery, thermal, and manufacturer power restrictions.

Do not apply the same optimizations blindly to every PC.

Every optimization must declare hardware, software, and operating-system compatibility requirements.

Unsupported tweaks must be hidden or clearly marked unavailable.

---

# 4. Intelligent Optimization Recommendation Engine

Create a rule-based recommendation engine.

After scanning the computer, FrameForge should produce a personalized optimization plan.

Each recommendation must include:

- Optimization name.
- Short explanation.
- Relevant hardware or Windows condition.
- Expected benefit category.
- Evidence level.
- Risk level.
- Administrator requirements.
- Restart requirements.
- Current configuration.
- Proposed configuration.
- Reversibility.
- Reason the optimization was recommended.

Example:

**Optimization:** Disable unnecessary startup applications

**Reason:** Several nonessential applications launch automatically and consume background resources.

**Potential benefit:** Reduced startup resource consumption and improved memory availability.

**Risk:** Low, provided required applications remain enabled.

**Action:** Review recommended startup applications.

Do not invent precise FPS improvement percentages.

Where possible, estimate performance changes using actual before-and-after measurements.

## Optimization Categories

Create a searchable catalog with the following categories:

1. Gaming Performance.
2. CPU Optimization.
3. GPU Optimization.
4. RAM and Memory.
5. Windows Debloat.
6. Startup Optimization.
7. Background Processes.
8. Storage and Cleanup.
9. Network and Connectivity.
10. Input and Responsiveness.
11. Windows Services.
12. Registry Configuration.
13. Power Management.
14. Windows Quality of Life.
15. Privacy and Background Activity.
16. Game-Specific Profiles.
17. Advanced and Experimental.

Every category must contain genuine working features, not placeholder buttons.

---

# 5. Windows Debloat and Application Management

Implement a safe, intelligent Windows debloating system.

The purpose is to reduce unnecessary background activity without damaging Windows.

## Application Classification

Classify installed applications into three groups.

### Group A — Generally Safe to Remove

Optional applications that are not required for Windows operation.

Examples may include certain preinstalled promotional applications, unused consumer applications, and optional bundled software.

Even in this group, verify that the application is not required by another installed feature.

### Group B — User Decision Required

Applications that may be useful depending on the user.

Examples:

- OneDrive.
- Xbox-related applications.
- Microsoft Teams.
- Phone Link.
- Manufacturer utilities.
- Third-party launchers.
- Optional Windows features.
- Preinstalled media applications.

Never remove these automatically without explicit user approval.

### Group C — Protected Components

Never automatically remove or disable critical components such as:

- Windows Update infrastructure.
- Microsoft Defender security infrastructure.
- Windows Security.
- Essential networking services.
- Windows Installer.
- Core authentication services.
- Microsoft Store dependencies.
- Windows servicing infrastructure.
- Hardware driver dependencies.
- Critical system services.

Maintain a documented protection list.

## Debloat Interface

Display:

- Application name.
- Publisher.
- Installation size where available.
- Background activity where measurable.
- Startup status.
- Removal risk.
- Dependencies.
- Reason for the recommendation.

Allow users to select individual applications for removal.

Show a confirmation dialog before uninstalling anything.

Provide a preview of every proposed removal.

Never uninstall applications silently.

Use supported uninstall mechanisms and provide clear error reporting.

Do not classify software as useless solely because it is not a Microsoft component.

---

# 6. Gaming Performance Optimization

Implement evidence-based Windows gaming optimizations.

Investigate and conditionally support:

- Windows Game Mode configuration.
- Game Bar and background recording preferences.
- Startup process management.
- Background application activity.
- Appropriate power-plan selection.
- Display refresh-rate verification.
- GPU preference settings.
- Fullscreen and windowed gaming settings.
- Hardware-accelerated GPU scheduling compatibility.
- Graphics driver configuration guidance.
- Unnecessary overlay identification.
- Game-specific graphics configuration recommendations.
- Background download identification.
- Shader cache maintenance when justified.
- Memory pressure analysis.
- Storage free-space monitoring.
- Relevant Windows gaming configuration.

Every option must be supported by the user's hardware and Windows build.

Do not assume disabling Game Mode, enabling ultimate performance, or changing GPU scheduling always improves FPS.

Some settings must be presented as controlled A/B experiments rather than universal recommendations.

## CPU-Specific Optimization

Include:

- CPU usage monitoring.
- Identification of CPU-heavy background processes.
- Power-plan inspection.
- Thermal throttling detection where supported.
- CPU bottleneck indicators.
- Processor-specific compatibility rules.
- Safe background workload reduction.

For AMD Ryzen X3D processors, avoid unverified core-parking, scheduler, affinity, or registry modifications.

Do not perform automatic overclocking, undervolting, or voltage changes.

## GPU-Specific Optimization

### AMD Systems

Support:

- AMD GPU detection.
- Radeon driver information.
- AMD Software: Adrenalin Edition integration where supported.
- Graphics configuration recommendations.
- Radeon Anti-Lag compatibility guidance.
- Radeon Chill configuration guidance.
- Frame-rate limiting recommendations.
- FreeSync configuration checks where supported.
- Shader cache information.
- Driver update links.

### NVIDIA Systems

Support:

- NVIDIA GPU detection.
- NVIDIA driver information.
- NVIDIA App integration where supported.
- NVIDIA Reflex guidance for supported games.
- Low Latency Mode compatibility guidance.
- G-SYNC configuration checks.
- Frame-rate limiting recommendations.
- Driver update links.

Never assume AMD and NVIDIA settings are interchangeable.

Avoid unsupported automatic driver-setting modifications.

---

# 7. Game-Specific Optimization Profiles

Create dedicated optimization profiles for popular games.

Prioritize:

- Fortnite.
- Rust.
- Counter-Strike 2.
- Valorant.
- Apex Legends.
- Minecraft.
- Call of Duty.

Each game profile must provide relevant recommendations based on the detected hardware.

## Fortnite Profile

Include:

- CPU-bound versus GPU-bound analysis.
- Performance Mode compatibility guidance.
- DirectX renderer selection guidance.
- Frame-rate cap recommendations.
- Resolution and refresh-rate checks.
- Background process optimization.
- NVIDIA Reflex recommendations when supported.
- AMD-specific latency recommendations when supported.
- Shader compilation and stutter considerations.
- Frame-time consistency analysis.
- Competitive versus visual-quality presets.

## Rust Profile

Include:

- Memory utilization analysis.
- CPU and GPU bottleneck identification.
- Storage performance considerations.
- Graphics quality recommendations.
- Frame-time monitoring.
- Background memory reduction.
- Safe configuration recommendations.

## Profile Requirements

Each game profile must:

1. Detect whether the game is installed where practical.
2. Identify supported configuration locations.
3. Back up configuration files before editing.
4. Provide a preview of proposed changes.
5. Explain the expected visual-quality tradeoffs.
6. Allow settings to be restored.
7. Avoid unsupported or anti-cheat-sensitive modifications.

Never inject code into games, manipulate anti-cheat systems, or alter protected game processes.

---

# 8. Performance Monitoring and Benchmarking

Create a performance monitoring dashboard.

Display supported live metrics such as:

- CPU utilization.
- GPU utilization.
- RAM usage.
- GPU memory usage.
- Disk activity.
- CPU and GPU temperature where available.
- Background process resource consumption.
- System uptime.
- Power mode.

Add optional frame-time and FPS monitoring through a supported, compatible telemetry integration.

If FPS data cannot be collected reliably, show that the feature is unavailable rather than inventing measurements.

## Before-and-After Comparison

Allow users to compare their system before and after optimization.

Track:

- Startup application count.
- Background process count.
- Idle RAM consumption.
- Idle CPU usage.
- Storage space recovered.
- Selected system configuration changes.
- FPS and frame-time statistics when valid benchmark data is available.

Provide charts and historical comparisons.

Do not claim that reducing process count automatically improves gaming performance.

Clearly distinguish actual measurements from theoretical benefits.

---

# 9. System Cleanup Utility

Build a safe Windows cleanup tool.

Support:

- Temporary file cleanup.
- Recycle Bin cleanup.
- User cache cleanup.
- Application cache cleanup where safe.
- Windows temporary directory cleanup.
- Old log file identification.
- Browser cache cleanup with user approval.
- Large file discovery.
- Storage usage analysis.
- Duplicate file identification as an optional feature.
- Windows-supported storage maintenance operations.

## Cleanup Safety

Before deleting files:

- Calculate recoverable storage space.
- Show a categorized preview.
- Identify locked or active files.
- Avoid deleting important user data.
- Exclude personal documents and game saves by default.
- Request confirmation for sensitive cleanup categories.

Do not automatically delete shader caches or other caches whose rebuilding could increase stutter or load times.

Never describe disk cleanup as an automatic FPS increase.

---

# 10. Network and Latency Diagnostics

Add a network optimization section.

Include:

- Network adapter detection.
- Current connection type.
- Connection speed information.
- DNS configuration display.
- Network latency testing.
- Packet loss testing.
- Jitter measurements.
- Background bandwidth usage.
- Network adapter driver information.
- Connection troubleshooting tools.

Optional features:

- DNS benchmark comparisons.
- Network configuration reset.
- Network adapter power-management review.
- Background download identification.

Do not implement arbitrary TCP registry tweaks, disable network security features, or claim that DNS changes automatically reduce in-game ping.

Any network configuration change must be reversible.

---

# 11. Windows Quality-of-Life Features

FrameForge should also improve everyday Windows usability.

Create a dedicated Quality of Life section.

Include:

- Dark/light mode controls.
- File Explorer preference shortcuts.
- File extension visibility.
- Hidden file visibility.
- Taskbar configuration shortcuts.
- Windows notification preferences.
- Startup application controls.
- Default application shortcuts.
- Clipboard history settings.
- Storage Sense configuration.
- Display refresh-rate shortcuts.
- Mouse settings shortcuts.
- Windows animation preferences.
- Power settings shortcuts.
- Screenshot configuration guidance.
- Multi-monitor configuration shortcuts.
- Windows update status.
- Optional privacy settings.

These features should be separate from performance tweaks.

Clearly distinguish convenience features from actual performance optimizations.

---

# 12. Backup, Restore, and Safety Architecture

Safety is a core product requirement.

Before modifying the system, FrameForge must create a recoverable change record.

Where supported and appropriate, offer Windows restore-point creation.

Implement an internal backup system for:

- Modified registry values.
- Changed service startup settings.
- Changed power configurations.
- Modified game configuration files.
- Relevant application settings.
- Previous optimization states.

## Restore Features

Provide:

- Undo individual optimization.
- Undo all changes from a selected session.
- Restore previous configurations.
- Export backup information.
- View optimization history.
- Retry failed restoration.
- Restart-required tracking.

Every optimization must be implemented as a structured operation with:

- Unique identifier.
- Compatibility checks.
- Current-state detection.
- Apply function.
- Verify function.
- Rollback function where technically possible.
- Error handling.
- Risk classification.

Operations that cannot be fully reversed must be clearly identified and require explicit confirmation.

Do not claim that uninstalling software or deleting files can always be reversed.

Never disable Windows security protections as part of routine gaming optimization.

---

# 13. User Interface and Experience

Design a modern, visually polished Windows desktop application.

The interface should feel like premium PC utility software rather than an outdated Windows script launcher.

## Visual Design

Use:

- Modern dark theme.
- Clean typography.
- Subtle accent colors.
- Rounded panels.
- Consistent spacing.
- Smooth, lightweight animations.
- Clear status indicators.
- Professional icons.
- Responsive window layouts.
- Minimal visual clutter.

Prioritize low resource consumption.

The optimizer must not itself become a significant background performance burden.

## Main Navigation

Include:

**Dashboard**
- System overview.
- Hardware summary.
- Optimization status.
- Recommended actions.
- Recent changes.

**Optimize**
- Searchable optimization library.
- Filters and categories.
- Individual optimization controls.
- Risk information.

**Debloat**
- Installed applications.
- Removal recommendations.
- Application management.

**Gaming**
- Game-specific profiles.
- Gaming recommendations.
- FPS and frame-time tools.

**Monitor**
- Resource usage.
- Hardware information.
- Performance history.

**Cleanup**
- Storage analysis.
- Safe cleanup tools.

**Tools**
- Driver utilities.
- Windows utilities.
- Diagnostic shortcuts.

**Quality of Life**
- Windows customization.
- Convenience settings.

**Restore**
- Backups.
- Undo operations.
- Optimization history.

**Settings**
- Application preferences.
- Updates.
- Logging.
- Advanced options.

## Search and Filtering

Implement a global search feature.

Users must be able to search for optimizations using:

- Feature name.
- Category.
- Description.
- Hardware compatibility.
- Keywords.

Add filters for:

- Recommended.
- Safe.
- Advanced.
- Requires restart.
- AMD.
- NVIDIA.
- Already applied.
- Not applicable.

Each optimization should have a clear status.

Examples:

- Available.
- Applied.
- Recommended.
- Not applicable.
- Restart required.
- Failed.
- Requires administrator privileges.

---

# 14. One-Click Smart Optimization

Implement a **Smart Optimize** feature.

After scanning the PC:

1. Analyze hardware and Windows configuration.
2. Identify relevant optimization opportunities.
3. Generate a personalized action plan.
4. Show all proposed changes.
5. Explain expected benefits and risks.
6. Allow the user to deselect individual changes.
7. Create backups where possible.
8. Apply approved changes.
9. Verify the resulting configuration.
10. Display a detailed completion report.

Do not automatically apply experimental or high-risk tweaks.

One-click optimization must never mean uncontrolled mass registry editing.

Provide three optimization modes:

### Safe Mode

Only low-risk, evidence-supported changes.

### Balanced Mode

Safe changes plus additional optional optimizations requiring review.

### Advanced Mode

More technical options, detailed compatibility information, explicit warnings, and manual confirmation.

Advanced Mode must not bypass system safety restrictions.

---

# 15. Additional Features

Implement the following features after the core MVP is stable.

## Optimization Impact Tracking

Show which settings were changed and whether measured system behavior improved afterward.

Allow users to mark a change as beneficial, neutral, or harmful.

## Bottleneck Analyzer

Analyze CPU, GPU, memory, storage, and thermal metrics to identify potential bottlenecks.

Display findings as diagnostic indicators rather than absolute conclusions.

## Startup Impact Analyzer

Identify resource-heavy startup applications and allow users to manage them.

## Gaming Session Mode

Allow users to activate an optional gaming profile that temporarily applies approved, reversible settings.

Restore temporary settings after the session where reliably supported.

Do not automatically terminate arbitrary user processes.

## Optimization Scheduler

Allow users to schedule safe maintenance operations.

Do not schedule disruptive cleanup or configuration changes during active gaming sessions.

## Driver Health Center

Display:

- Installed driver versions.
- GPU manufacturer.
- Driver update availability when verifiable.
- Official driver download links.
- Driver-related troubleshooting guidance.

Do not automatically install drivers without approval.

## Optimization History

Record:

- Timestamp.
- Optimization name.
- Previous value.
- New value.
- Execution result.
- Rollback availability.

## Application Resource Efficiency

Use event-driven monitoring where possible.

Avoid unnecessary polling, background services, startup tasks, or elevated processes.

FrameForge should not need to remain running for persistent system settings to take effect.

---

# 16. AMD and NVIDIA Distribution Requirements

The finished product must support both AMD and NVIDIA users.

Prefer a **single universal download**.

The application should automatically detect the installed GPU and display the appropriate features.

### Preferred Distribution

FrameForge Universal.zip

Contents:

- FrameForge executable or installer.
- README.txt.
- LICENSE file where applicable.
- Required application resources.
- Documentation.
- Companion tools information.
- Official AMD, NVIDIA, and Windows utility download links.

At startup:

1. Detect GPU manufacturer.
2. Detect CPU and other hardware.
3. Select the appropriate hardware profile.
4. Enable supported optimization features.
5. Hide incompatible controls.
6. Display relevant companion tools.

### Alternative Distribution

If a universal package is technically impractical, create:

- FrameForge-AMD.zip
- FrameForge-NVIDIA.zip

The AMD package must include AMD-specific optimization support.

The NVIDIA package must include NVIDIA-specific optimization support.

Both versions must retain the same core Windows optimization functionality.

Do not bundle proprietary NVIDIA or AMD installers without confirming redistribution rights.

Prefer official download links or approved distribution methods.

Never download and execute third-party binaries silently.

---

# 17. Companion Tools and Utilities

Include a useful Tools section.

Depending on the user's hardware, provide access to relevant utilities such as:

**AMD**
- AMD Software: Adrenalin Edition.
- AMD chipset driver information.
- Official AMD support resources.

**NVIDIA**
- NVIDIA App.
- Official NVIDIA driver downloads.
- NVIDIA graphics configuration resources.

**General**
- Windows Task Manager.
- Resource Monitor.
- Windows Event Viewer.
- Reliability Monitor.
- Windows Storage settings.
- Windows Update.
- DirectX Diagnostic Tool.
- System Information.
- Official hardware diagnostic utilities.

Where third-party tools are included, verify their source, licensing, and compatibility.

Avoid bundling unnecessary utilities that duplicate existing Windows functionality.

---

# 18. Recommended Technical Architecture

Choose a suitable technology stack for a Windows desktop application.

Prefer a modern native or near-native Windows approach.

Consider:

- C# with .NET 8 or newer supported LTS version.
- WPF or WinUI 3.
- MVVM architecture.
- Windows APIs and documented system interfaces.
- SQLite or lightweight JSON storage.
- Structured logging.
- Modular optimization services.

Use a different stack only if there is a clear technical justification.

Avoid building the finished product as a website disguised as a desktop application unless the tradeoffs justify it.

## Suggested Architecture

Organize the application into modules:

- UI and Navigation.
- Hardware Detection.
- Compatibility Engine.
- Optimization Registry.
- Optimization Execution.
- Debloat Management.
- Cleanup Services.
- Gaming Profiles.
- Performance Monitoring.
- Backup and Restore.
- Logging and Diagnostics.
- Settings and Persistence.
- Updates and Distribution.

Keep UI code separate from system-modification logic.

Use least-privilege execution.

Only request administrator privileges when an approved operation requires elevation.

Do not run the entire application as administrator unnecessarily.

Never execute arbitrary shell commands supplied through untrusted configuration files.

---

# 19. Optimization Quality Standards

Each optimization must satisfy the following requirements:

1. It performs a real operation.
2. It has a documented purpose.
3. It checks whether the operation applies to the current PC.
4. It reads the current state before making changes.
5. It explains the proposed modification.
6. It handles failures.
7. It verifies whether the change succeeded.
8. It provides rollback when technically possible.
9. It does not silently weaken system security.
10. It avoids unsupported performance claims.

Do not add hundreds of meaningless tweaks simply to make the application appear feature-rich.

Prioritize useful, verified functionality.

For registry-based features, document the exact registry location, supported Windows versions, potential side effects, and reversal procedure.

Avoid generic registry optimization packs and unsupported timer, scheduler, HPET, or memory-management tweaks.

---

# 20. README and Documentation

Create a complete README.txt in the distribution ZIP.

Include:

- What FrameForge does.
- Supported Windows versions.
- Minimum system requirements.
- Installation instructions.
- First-launch instructions.
- Hardware scanning explanation.
- How Smart Optimize works.
- Explanation of optimization modes.
- How to remove Windows bloat safely.
- How to use game-specific profiles.
- How to restore changes.
- How to troubleshoot errors.
- Administrator permission requirements.
- Known limitations.
- Privacy and security information.
- Official driver utility links.
- Disclaimer that performance gains vary by system.

Also create developer documentation explaining:

- Project architecture.
- Build requirements.
- Build commands.
- Testing procedures.
- Release packaging.
- How to add new optimization modules.

---

# 21. Testing and Validation

Implement automated tests where practical.

Test at minimum:

- Hardware detection.
- AMD versus NVIDIA compatibility selection.
- Unsupported hardware handling.
- Optimization applicability.
- Current-state detection.
- Apply and verification behavior.
- Rollback behavior.
- Failed operations.
- Administrator permission handling.
- Configuration persistence.
- Search and filtering.
- Cleanup exclusion rules.
- Debloat protection rules.
- Release packaging.

Use mocks or abstractions for destructive system operations during automated testing.

Never run destructive system changes as part of ordinary unit tests.

Perform integration testing in disposable Windows virtual machines where appropriate.

Document which features require real hardware testing.

## Performance Validation

Where possible, compare before-and-after results under similar conditions.

Record:

- Hardware configuration.
- Windows version.
- Driver version.
- Game version.
- Graphics settings.
- Benchmark procedure.
- Average FPS.
- 1% low FPS.
- Frame-time consistency.

Do not claim performance gains without supporting measurements.

---

# 22. GitHub Repository and Release Requirements

The GitHub repository must contain:

- Complete source code.
- Solution and project files.
- Dependency declarations.
- Build instructions.
- Test projects.
- README.md.
- User-facing README.txt.
- License information.
- Release packaging scripts.
- GitHub Actions build workflow.
- GitHub Actions test workflow.
- Release artifact generation.

Configure GitHub Actions to:

1. Restore dependencies.
2. Compile the application.
3. Run automated tests.
4. Package the application.
5. Generate the final ZIP.
6. Publish release artifacts when an appropriate release tag is created.

Prefer a self-contained Windows x64 build where practical.

The release must contain a working executable or installer, not only source code.

Do not claim that a build or test passed unless it actually ran successfully.

---

# 23. MVP Acceptance Criteria

The first complete MVP is acceptable only when:

- The Windows application launches successfully.
- The UI is functional and visually consistent.
- The application detects real PC specifications.
- AMD and NVIDIA systems receive appropriate recommendations.
- Users can search and filter optimization features.
- Supported optimization actions actually modify the intended setting.
- Users can review changes before execution.
- The debloat manager distinguishes optional applications from protected components.
- Cleanup operations show previews and protect personal data.
- Backup and restore functionality works for supported reversible changes.
- Optimization history is saved.
- Errors are handled without crashing the application.
- The application does not automatically weaken Windows security.
- A release ZIP can be generated.
- The ZIP includes the working application and README.txt.

Advanced features must not delay completion of this functional MVP.

---

# 24. Final Execution Instructions for Codex

Treat this project as a real Windows software engineering task.

Do not produce only a concept, pseudocode, mockup, or static frontend.

Build the actual application.

Follow this execution order:

**Step 1:** Inspect the existing GitHub repository.

**Step 2:** Research relevant Windows and GPU optimization mechanisms using reputable technical sources.

**Step 3:** Produce a development plan with architecture, dependencies, implementation phases, risks, and testing strategy.

**Step 4:** Create the application structure and core services.

**Step 5:** Implement the working MVP.

**Step 6:** Implement hardware-aware optimization and compatibility checks.

**Step 7:** Implement the debloat manager, cleanup utility, and backup system.

**Step 8:** Implement gaming profiles, performance diagnostics, and additional quality-of-life features.

**Step 9:** Polish the interface and improve usability.

**Step 10:** Add automated testing, documentation, and release packaging.

**Step 11:** Build the application and run all available tests.

**Step 12:** Produce the final downloadable Windows ZIP artifact.

After each phase:

- Summarize completed functionality.
- Identify remaining work.
- Report build and test results.
- Document known limitations.
- Continue with the next logical implementation phase.

Do not stop after creating the initial project structure.

Do not replace functional requirements with fake demonstration data.

Do not create nonfunctional buttons or settings simply to fill the interface.

If a feature is not yet implemented, clearly mark it as unavailable rather than pretending it works.

**Final objective:** Deliver a polished, reliable, hardware-aware Windows gaming optimizer that provides genuine system management functionality, evidence-based performance improvements, safe debloating, game-specific recommendations, and useful Windows quality-of-life tools in one professional application.

The highest priorities are:

1. Real functionality.
2. System safety and stability.
3. Hardware-specific correctness.
4. Measurable gaming performance improvements.
5. Clean and intuitive user experience.
6. Reliable rollback and recovery.
7. Complete, downloadable Windows distribution.

Build FrameForge to meet these requirements without sacrificing reliability for an inflated feature count.