# FrameForge 0.7.0

This release improves recovery, everyday use and measuring whether a tweak helped. It retains the 30 direct switches, seven optional service controls, two power checks and one-click Fortnite/Rust profiles from 0.6.0.

## A simpler path through the app

Home, Low-end PC, Potato Settings, Tweaks, Live stats and Recovery stay visible in the sidebar. Less-used pages are under More tools. The option levels are now labelled Basic, More choices and Experiments; existing saved Safe/Balanced/Advanced values are still compatible. An unknown saved level falls back to Basic. Checkboxes have larger targets, a matching dark theme, wrapped labels and a visible keyboard-focus border. Unavailable switches keep their explanation visible after a failed refresh.

Low-end PC has Recommended, Browser memory and Clear selection buttons. They only select choices; Apply still makes the actual changes. Your selection is remembered across navigation and launches. An explicitly cleared selection remains cleared. Unsupported/already-configured choices are excluded. Selecting a stronger browser option includes its available prerequisite; clearing the prerequisite clears the dependent option. Required settings that are already configured need no new write. Optional selections expand so they can be inspected before applying. Game Mode joins the initial recommended choices; recording and desktop tradeoffs remain described under their titles.

## Recovery that handles partial work

The Windows recording and Edge background switches each change multiple registry values. Previously, failure after the first value could leave a mixed state that ordinary conflict detection could not undo. The grouped writer now reads and validates originals before writing, checks each write and compensates completed writes on failure. Compensation only touches values still matching this attempt or their saved original. Unexpected external values stay untouched.

The journal distinguishes a verified completed write from an interrupted apply or an actual in-progress restore. Recovery can resume an eligible partial group if every value matches either its recorded original or target. Verified changes with later external edits remain conflicts, including after repeated undo attempts. Prepared/failed legacy records and legacy Restoring records can be inspected under the same narrow per-value rules. Unknown/malformed values are never forced. This is compensating recovery plus a write-ahead journal, not a claim of an atomic Windows registry transaction or power-loss immunity.

Recovery groups changes into sessions, newest first, with an Undo session action. It restores in reverse journal order and stops at the first conflict. Originals for remaining changes stay saved. Active changes, Needs attention and All history filters, local search, individual details and incremental session listing replace the long flat history. Windows restore points and game graphics backups remain available in clearly labelled expandable sections.

## Import real measurement-tool output

Live stats → Benchmarks accepts the original simple FrameTimeMs format and documented PresentMon CSV schemas. Supported PresentMon timing columns are MsBetweenPresents (including legacy msBetweenPresents) and FrameTime from PresentMon 2.x. Application, ProcessID and SwapChainAddress are required. Units are explicit; ambiguous timing columns and duplicate headers are rejected.

Applications, processes and presentation streams are never combined into an artificial higher frame rate. A multi-stream file opens a picker; short streams remain visible but cannot be imported until they contain at least 100 valid intervals. A first zero interval can be excluded and is reported; later zero, negative, missing, infinite or invalid intervals fail the import. BOMs, CRLF, quoted commas and escaped quotes work. Limits remain 20 MiB and one million rows, with bounded row/column/stream counts. Multiline cells are rejected clearly.

The measurement type is saved and exported. Comparisons reject unlike interval types and known different applications. Presentation intervals and CPU frame-start intervals are distinct from displayed FPS and input latency; the UI explains that distinction. This adds import support, not a background game recorder, overlay, injection or bundled third-party executable.

The slow-frame p99 statistic now uses a defined nearest-rank percentile. Earlier builds used an inconsistent tail boundary. Old saved statistics retain a Legacy tail marker. Average/slowest-1% results can still be read, but the app does not directly compare p99 results calculated with different methods.

The schema mapping was checked against the original tool documentation: [current PresentMon console format](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md), [2.3.0 format](https://github.com/GameTechDev/PresentMon/blob/v2.3.0/README-ConsoleApplication.md) and [1.9.2 format](https://github.com/GameTechDev/PresentMon/blob/v1.9.2/README.md). No third-party source or binary is copied into the app.

## Source and release consistency

The GitHub source branch is synchronized with the current project so this release can be tagged against its actual source. CI supports an already-published release without overwriting its files; independently built packages remain available as workflow artifacts. Earlier release files and local user history are preserved.

## Validation

212 portable tests passed; two privilege-dependent symbolic-link tests were skipped. New coverage includes compensated writes, failure after a mutation, external changes during compensation, interrupted/legacy group recovery, resumable failed undo, session conflicts, selection dependencies/persistence, CSV stream identity and malformed data, and the percentile boundary. An outdated guide-text assertion was corrected to reflect the already-implemented Fortnite/Rust profiles and the absence of automatic FPS recording.

WPF checks cover 14 pages, 11 dropdowns, compact layouts, every existing switch/service/profile control, preset selection without Windows writes, remembered/cleared selections, automatic prerequisites, cancelled and confirmed session undo, and choosing only the selected synthetic PresentMon stream. Native Windows checks read real hardware/settings, run CPU/RAM sampling, parse service scripts, and test actual config writes against disposable files. A separate unique current-user registry fixture verifies grouped writes, an injected second-write refusal, compensation and exact restoration of value absence; the fixture is removed afterward. Actual browser/Windows policies, power plans, services and installed game files were not changed by acceptance.

This remains an unsigned Windows x64 engineering preview. No game FPS or input-latency improvement was measured. Live policy/power/service mutation acceptance across supported machines and the earlier actual Windows restore-point creation test remain pending. Normal UAC permission and standard access after cancellation are retained.
