# FrameForge 0.8.0 status

The current preview improves UI responsiveness and existing tweak reliability. Search/categories filter cards in place; Windows probes are bounded and run off the dispatcher; native engine operations leave input responsive. Page position, guide selections and connection input are retained. Review dropdowns and sidebar/expander styles are clearer. Live logs batch updates, and large CSV statistics/chart preparation run in workers.

The 30 switches, seven optional services, two power checks, low-end profile and Fortnite/Rust graphics profiles remain. Stronger browser policies recheck prerequisites immediately before writing, and profiles apply prerequisite settings first. Process timeouts now include output drains. Display caches are short-lived and never replace engine preflight or write/readback checks.

See [0.8.0 changes and validation limits](UPDATE-0.8.0.md). 225 portable tests passed and two privilege-dependent tests skipped. WPF acceptance covers 14 pages, 13 dropdowns, all existing controls and new responsiveness/failure paths; native acceptance uses read-only system checks and disposable config/registry fixtures. No installed game files, real policies, services or power plans were changed by acceptance.

No game FPS/input-latency gain was measured. Actual Windows restore-point creation, broad live policy/power/service mutations and physical hardware acceptance remain pending. Existing local preferences/history remain compatible. Normal UAC permission and standard access after cancellation are retained.
