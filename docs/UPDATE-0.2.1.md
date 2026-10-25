# FrameForge 0.2.1 — visible feature controls

Open **Features** in the sidebar, **Feature toggles** in the header, or **Manage feature toggles** on Dashboard. The shortcut always clears search and opens the switches.

Three supported settings have real two-way controls: **Show file extensions**, **Show hidden files**, and **Dark application theme**. Their On/Off labels are read from Windows. An absent registry value is labelled **Windows default**; an unexpected value disables the switch and shows the reason.

Flip a switch to review the current and proposed values. A single Turn on/Turn off confirmation replaces the old checkbox selection workflow for these controls. Cancel keeps the state unchanged. Confirmed changes retain write-ahead original-state backups, stale-preview checks and read-back verification. The displayed switch changes only after verification. Turning off writes the explicit disabled setting; Restore returns the exact prior value, including an absent default.

The review-mode selector is beside the controls. Hidden-file changes require Balanced or Advanced mode; the explanation remains visible in Safe mode. Refresh feature states re-reads Windows after external changes. Search covers all feature sections.

**Windows controls** groups Game Mode, startup, graphics, notifications and other manual controls with clearly labelled Open Windows settings buttons. Official vendor guidance is labelled separately. **Power experiment** retains the existing Advanced-mode desktop power-plan preview.

Verification includes unit tests for enable/disable targets, original-value restoration, absent defaults, already-off no-ops, external conflicts, unexpected values and failed write verification. Windows UI integration clicks all three switches through the actual handler, exercises cancellation and both directions, validates Safe-mode gating, confirms shortcut discovery and performs reverse-order undo. UI writes use an in-memory settings adapter and isolated journal; no real Windows settings are changed by the test.
