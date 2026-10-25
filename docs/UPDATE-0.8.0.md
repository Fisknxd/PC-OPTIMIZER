# FrameForge 0.8.0 — smoother everyday use

This update keeps the 30 backed-up switches, seven optional service controls, two power checks and Fortnite/Rust graphics profiles. It improves the app itself and the reliability of existing tweaks. It does not add speculative timer, security-disabling or unsupported FPS packs.

## UI and navigation

The sidebar has clearer selected states and keyboard focus. Expanders match the dark theme. The header keeps Scan PC and Live changes accessible without a redundant Tweaks button. Review-level and process-priority dropdowns show readable names instead of internal record dumps. Normal screens use Basic, More choices and Experiments consistently.

Search filters existing cards rather than rebuilding the page, rereading Windows and losing focus. Categories also change visibility without reconstructing cards. Category guidance and secondary dropdown selections stay in sync; choosing a category clears the search deliberately. All states / On / Off / Needs review filters distinguish a saved state from blocked availability. Unclassified manual controls do not masquerade as pending switches. Hidden cards leave no empty grid rows. Clear and Esc make search easy to reset. Pending search is stopped when navigating elsewhere.

Navigation retains a page's scroll position. Game-guide choices and connection input survive navigation for the app session. Recovery search is debounced. Preference changes are committed in memory after they save successfully; the review picker restores its previous selection if saving fails.

## Responsive setting checks and actions

Read-only setting probes run away from WPF's input thread with at most three workers. Visible switches load on demand. Successful reads may be reused for eight seconds while moving around the app; Refresh and actions invalidate that display cache. Queued probes cancel when their page goes away, and page-generation checks stop stale results from updating a new view. The cache never authorizes a write: preview/apply still checks actual current settings through the operation engine.

Native engine preview, apply and undo calls run on workers; dialogs and control updates remain on the dispatcher. The existing busy guard and engine mutation gate continue to serialize changes. The progress/status area gets an input/render opportunity before work starts. Low-end previews preserve the existing semantics for custom-but-equivalent values, detect an already-active power plan by its correct Guid type, and block administrator-only choices under standard access.

## Tweak reliability

Stronger Chrome Memory Saver and Edge sleeping-tab policies now recheck their base policy inside OperationEngine.Apply, after confirmation and immediately before any journal/write. A base setting changed in the confirmation interval cannot silently leave a newly applied stronger setting ineffective. Turning an ineffective stronger policy off remains available. Profile application orders the base settings before their dependents, regardless of supplied selection order.

Windows utility timeouts cover process exit and both redirected output streams. A child holding a pipe open cannot keep the UI waiting without a timeout. Output sizes and live display lines are bounded; failed/cancelled readers have their errors observed. Changes still require the existing write/readback and recovery rules. No actual policy, service, power or game-file mutation was performed on the user's installation by this validation.

The prerequisite assumptions were checked against [Chromium MemorySaverModeSavings](https://chromium.googlesource.com/chromium/src/+/HEAD/components/policy/resources/templates/policy_definitions/Miscellaneous/MemorySaverModeSavings.yaml), [Chrome Memory Saver policy](https://chromeenterprise.google/intl/en_uk/policies/high-efficiency-mode-enabled/) and [Microsoft Edge sleeping-tab policy](https://learn.microsoft.com/en-au/DeployEdge/microsoft-edge-browser-policies/sleepingtabsenabled). Saved browser policy readback remains distinct from browser acceptance; the existing Check in browser workflow remains necessary.

## Logs and large captures

Live changes appends bounded batches every 100 ms at background dispatcher priority instead of scheduling one UI callback/layout/scroll per output line. Follow latest can be switched off, and scrolling upward pauses following. Each window owns its queue/timer/subscriptions, so closing and reopening cannot route old updates into the new window. The display retains at most 200,000 characters, with bounded queue and long-entry messages. Earlier displayed content can eventually be trimmed; Save log exports the entries retained by the existing local activity feed.

PresentMon import parsing, trace statistics, median/budget summaries and peak-preserving chart preparation happen in workers. The display caches that presentation for navigation/resize. Full imported measurements remain the source of statistics; a maximum of 2,048 peak-preserving points is sent to the chart. The synthetic million-frame validation checks exact statistics and retains a slow-frame peak. This remains CSV import, not a game hook, recorder or input-latency measurement.

## Validation and limits

225 portable tests passed; two privilege-dependent symbolic-link tests skipped. New cases cover dependency changes during confirmation, prerequisite order, safe disable, process exit/error/output limits and inherited-pipe timeout. Three packaging failure-case tests also pass.

WPF acceptance checks all 14 pages and 13 dropdowns, compact layouts, all 30 switches, seven services, both power changes, graphics fixtures, profile/session recovery, search identity/focus without setting rereads, no-match and state filters, scroll/input retention, slow read-worker responsiveness/cancellation/cache refresh, standard-access choices and an already-active power plan. Burst-log lifecycle and a synthetic million-frame import are exercised. Automated shutdown preserves failure exit codes, and validation requires fresh reports from every workflow plus the absence of a current error file. A deliberately failed automated run in a separate temporary fixture must return exit code 1 and the expected diagnostic; its expected error never pollutes the successful acceptance reports.

Native checks read real hardware/settings and use disposable config/registry fixtures for apply/failure/undo. Actual Windows restore-point creation, broad policy/service/power acceptance and physical gaming hardware acceptance remain pending. No game FPS or input-latency improvement was measured. This is an unsigned Windows x64 engineering preview. Existing user preferences, benchmark statistics and recovery history remain compatible.
