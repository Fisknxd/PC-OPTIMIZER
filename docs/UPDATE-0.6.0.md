# FrameForge 0.6.0

This update adds nine backed-up switches and two power checks. The app now has **30 direct switches**, seven optional service controls, two individual power-setting actions and the existing power-plan experiments. No external tweak pack is downloaded or executed.

## Easier to use

Home puts Low-end PC and Potato Settings in front of the hardware inventory. Tweaks opens to a gaming-focused selection; ad, theme and cosmetic preferences stay in the other categories. Four main category buttons and one additional-category picker replace the crowded tab row. Every switch has a short description and expandable tradeoffs.

The Low-end PC profile has 17 selectable choices. Optional browser, download and Windows policies are under More choices. Settings already at the requested value are marked and left unchecked. The selection count updates immediately; Apply is disabled when nothing needs changing. Successfully changed originals remain available as a group in Recovery. Browser options that depend on another option are checked before the profile's first write.

## New switches and their actual effects

- **Keep Chrome running after closing:** Off disables Chrome background mode. Close Chrome normally; background app notifications can stop. This can release idle browser resources if background mode was being used.
- **Chrome Memory Saver:** On lets Chrome reclaim inactive-tab memory. Returning tabs may reload; active or excluded tabs are not all discarded.
- **Stronger Chrome memory savings:** On selects maximum savings; Off selects balanced savings. Memory Saver must be enabled first. The profile can select both together. Chrome 126+ is required.
- **Edge sleeping tabs:** On lets inactive tabs sleep to reduce their CPU and memory use. This is already a normal default on many PCs.
- **Let Edge tabs sleep sooner:** On selects five minutes; Off selects fifteen minutes. Sleeping tabs must be enabled first. Edge sleeping-tab policies do not apply to personal Microsoft-account profiles; the UI explains this limitation.
- **Windows Widgets:** Off blocks the optional Windows 11 Widgets experience. It keeps shared components installed and needs sign-out. This removes optional content activity, not a guaranteed game-FPS bottleneck.
- **Let indexing slow down when busy:** On sets DisableBackoff=0, restoring normal indexer backoff. This is useful when another tweak forced continuous full-speed indexing. Search stays enabled. Most untouched systems already use backoff.
- **Limit background update downloads:** On sets the Delivery Optimization percentage limit to 10; Off uses adaptive bandwidth (0). Applies to eligible Microsoft downloads, not Steam, Epic, every network app or local-LAN peers. Updates take longer.
- **Limit manual update downloads:** On selects a 25% foreground limit; Off uses adaptive bandwidth. Useful only while those downloads compete with gaming. It can slow installs started by the user. Existing absolute/hourly policies can affect the effective limit.

Windows device policies are restricted to recognized supported Pro/Enterprise/Education editions. Widgets also requires Windows 11. Home users can use the direct Open Windows download limits button. Browser policies require an installed browser of an appropriate version. Known overriding machine policies disable the user control. The new availability checks also cover the older Edge background/startup-boost setting.

The switch shows the saved local policy. Browser acceptance is a separate check: open Check in browser, press Reload policies, and check for OK. Cloud management, profile/account restrictions and browser changes may prevent a policy taking effect. The app does not equate a registry readback with proof that a browser tab has slept.

## Power checks

- **Allow full CPU speed when plugged in:** Sets the current plan's primary maximum processor performance state to 100%. Helps only if that setting was capped. It does not override thermal limits, firmware, hybrid-core-class settings, boost policy or hardware capability. More heat/fan noise is possible.
- **Try full Wi-Fi power when plugged in:** Sets the existing wireless power policy to maximum performance. Offered only when a Wi-Fi adapter exists. Driver/OEM behavior varies; compare actual connection spikes before keeping it.

Both are Advanced experiments. They use native Windows power APIs, retain the active plan identifier and battery value, and change only the plugged-in index. Missing settings stay unavailable. Already-configured controls are disabled. Undo is on the same card and in Recovery, and refuses if the active plan or saved AC/DC state has changed externally. No OEM power plan is created, replaced or deleted.

## Sources reviewed

Reviewed on 17 October 2026. These establish mechanisms and supported behavior, not a measured FrameForge FPS uplift.

- [WinUtil source](https://github.com/ChrisTitusTech/winutil/blob/main/config/tweaks.json) and [FAQ](https://winutil.christitus.com/faq/): reference categories and established tweak implementations. No downloaded pack was executed or copied wholesale.
- [EXM](https://exmtweaks.com/en-eu): product workflow reference. Its proprietary implementations were not inspected or treated as evidence of performance.
- [Chrome background mode](https://chromeenterprise.google/policies/background-mode-enabled/), [Memory Saver](https://chromeenterprise.google/intl/en_uk/policies/high-efficiency-mode-enabled/) and [savings levels](https://chromeenterprise.google/intl/nl_nl/policies/memory-saver-mode-savings/).
- [Edge sleeping tabs](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/sleepingtabsenabled) and [timeout](https://learn.microsoft.com/en-us/deployedge/microsoft-edge-policies/sleepingtabstimeout).
- [Widgets policy](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-newsandinterests#allownewsandinterests), [indexer backoff](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-search#disablebackoff), [download policies](https://learn.microsoft.com/en-us/windows/client-management/mdm/policy-csp-deliveryoptimization) and [bandwidth behavior](https://learn.microsoft.com/en-us/windows/deployment/do/waas-delivery-optimization-reference). The installed Windows ADMX definitions were also checked for the download values and DisableBackoff mapping.
- [Maximum processor state](https://learn.microsoft.com/en-us/windows-hardware/customize/power-settings/options-for-perf-state-engine-maxperformance), [PowerReadACValueIndex](https://learn.microsoft.com/en-us/windows/win32/api/powrprof/nf-powrprof-powerreadacvalueindex) and [Intel wireless power guidance](https://www.intel.com/content/www/us/en/support/articles/000046272/wireless/legacy-intel-wireless-products.html).
- [Windows windowed-game optimization](https://support.microsoft.com/en-au/windows/hardware/display-graphics/optimizations-for-windowed-games-in-windows-11) and [supported Settings links](https://learn.microsoft.com/en-us/windows/apps/develop/launch/launch-settings). Windowed-game optimization, refresh rate and choosing the correct GPU remain clearly labelled Windows controls instead of undocumented registry automation.

Timer/HPET packs, disabled security/update services, forced shared-service priority, pagefile removal, standby-memory purges and routine shader-cache clearing are excluded. Raising idle-service priority or disabling an already-idle component is not established evidence of higher FPS.

## Verification and limits

177 portable tests passed; two privilege-dependent symlink tests were skipped. Tests include every switch in both directions, custom numeric-policy undo, power-plan/battery identity conflicts, failed/ignored writes, browser prerequisites, missing-browser apply rejection with retained undo, Windows 10 Widgets gating, and the existing graphics/restore/cleanup/update coverage.

WPF checks exercise all 14 pages, dropdown contrast and compact layouts. Integration clicks all 30 switches, both power controls (cancel/apply/already-configured/undo), browser prerequisites, both Potato profiles, services, profile apply/restore and recovery, using isolated settings adapters. Native Windows checks read actual setting availability and AC/DC power indices, run a hardware scan and monitor, parse generated service scripts, and exercise real file writes only against disposable game-shaped fixtures. Installed game files and actual Windows policies/power plans are not changed by acceptance.

On this PC the primary CPU maximum is already 100% in its existing OEM plan. There is no Wi-Fi adapter. New controls correctly show no change needed or unavailable. The current Windows edition does not meet the device-policy edition allowlist; those controls stay locked. These results are not fabricated boosts.

No game was launched, and no FPS/input-latency uplift, exact promotional potato appearance or anti-cheat outcome was measured. Native policy/power/service mutation acceptance across supported Windows editions remains pending. The earlier real restore-point creation attempt remains unverified after declined elevation. This remains an unsigned engineering preview, with normal UAC permission and standard access on cancellation.
