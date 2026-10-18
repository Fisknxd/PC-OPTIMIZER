# FrameForge 0.7.0 status

The current build retains 30 switches, seven optional services, two power checks and the Fortnite/Rust Potato profiles. It adds persistent profile selections and presets, automatic browser prerequisites, session-based recovery, compensated grouped registry writes, interrupted group recovery and direct PresentMon CSV imports. Sidebar navigation, option labels and checkbox styling are simpler.

See [0.7.0 changes and limits](UPDATE-0.7.0.md). 212 portable tests passed; two privilege-dependent tests were skipped. WPF workflow/contrast/compact-layout tests pass. Native Windows acceptance verifies disposable game-config and registry-fixture writes plus real read-only hardware/setting checks. No actual Windows/browser policies, power plans, services or installed game files were changed by acceptance.

Source is synchronized on the GitHub development branch for release tagging. Existing saved preferences, benchmarks and history stay compatible. Older p99 calculations are labelled and not directly compared against the new method.

No game FPS/input-latency improvement was measured. Actual policy/power/service mutations across supported Windows editions and the earlier real restore-point creation test remain pending. This is an unsigned, self-contained Windows x64 engineering preview. UAC permission is requested at launch; cancellation allows standard access.
