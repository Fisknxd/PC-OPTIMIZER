# FrameForge 0.2.0

This release improves daily usability and performance measurement without expanding automatic system changes.

## Dashboard and navigation

The dashboard separates hardware, CPU/memory utilization, storage and restore status. Interrupted/failed journal entries get a recovery prompt. Reports export measured hardware and findings as JSON. Navigation has grouped sections, a scrollable sidebar, themed scrollbars, keyboard search (Ctrl+K) and debounced filtering. Primary button contrast is checked against actual rendered text.

Store-package inventory now filters by name/publisher and removal eligibility, with visible counts and explicit selection. Full system snapshot baselines and comparison remain available under Hardware inventory.

## Live resource monitoring

Monitor has Live resources, Benchmarks and Hardware inventory views. Native counters collect CPU and physical memory once per second when explicitly started. The last 120 samples stay in memory and export as CSV. Monitoring stops when leaving the view, executing another action or closing. The first CPU interval is not fabricated; paused gaps are not averaged into readings. On systems with more than 64 logical processors, Windows may limit CPU counters to the calling processor group.

## Benchmark workflow

Import one FrameTimeMs column with at least 100 positive finite durations. Imports accept UTF-8 BOM, CRLF and quoted values, report invalid line numbers and enforce 20 MiB / one million frame limits. A failed import retains the previous valid capture.

Statistics include average FPS, slowest-1% mean FPS, empirical p99 duration, median duration and counts above budgets. Charts retain the largest duration per pixel bucket, preserving isolated stutters that the old stride-based chart could miss. Axes show milliseconds; a 16.67 ms guide marks the 60 FPS budget.

Save labels, test notes and the latest scanned hardware with each run. Export the newest 100 saved summaries and compare a baseline against the current capture. Existing 0.1.1 benchmark records remain readable. Raw frames stay in the source CSV and current session; saved summaries do not contain the original trace. Spreadsheet formula prefixes are escaped in exported user text.

## Reliability and verification

Full scans expose progress and cancellation between bounded WMI queries, retaining the previous complete snapshot. An in-progress query must finish or time out first. Status/cancellation remain available while other controls are disabled.

CPU and physical RAM in full scans also use native counters, avoiding unavailable WMI performance providers. The CPU value covers the scan interval, while live monitoring measures successive one-second intervals.

Restore refuses to undo an older record while a newer unresolved record for the same setting exists. Existing external-value conflict checks and original-state journaling remain in place.

Tests cover benchmark units, invalid rows, limits, summary compatibility, spike preservation, median calculation, undo ordering and original behavior. Windows UI checks cover every page, three Monitor views, synthetic chart input, compact layouts and button/dropdown contrast. Read-only integration exercises real WMI scanning, cancelled scans, native counters, sample collection and timer shutdown on navigation. Synthetic input is labelled QA-only in an isolated test data directory.

Two symbolic-link tests are explicitly skipped where Windows symlink privilege is unavailable. A separate junction test exercises cleanup/update link rejection without elevation. No real settings, uninstalls or cleanup were executed on the primary PC. No game FPS improvement is claimed. Broad mutating acceptance and publisher signing remain pending for this engineering preview.
