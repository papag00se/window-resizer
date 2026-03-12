# Window Resizer PRD

Last updated: 2026-03-12

## Summary

Build a per-user Windows background app that stays running, waits for Visual Studio Code windows to open, and automatically rearranges all eligible VS Code windows. The same layout must also be available on demand from a tray icon without waiting for a new window event.

## Problem

When multiple VS Code windows are open, keeping them aligned to a consistent width, height, and left-to-right order is manual and repetitive. The desired layout is stable and mechanical, so it should be enforced by a background utility instead of repeated by hand.

## Goals

- Run as an always-on background app in the signed-in user's session.
- Detect when a new VS Code window appears and apply the layout automatically.
- Expose a tray icon action that reapplies the layout on demand.
- Seed the layout width to `1823` physical pixels, taken from the active VS Code window on 2026-03-12, and allow it to be changed in Settings.
- Resize each eligible VS Code window to the full working-area height of the target monitor.
- Place windows from left to right using a heuristic that approximates open order from observed window events, with stable fallbacks for pre-existing windows.
- Use hide/show sequencing to push Explorer's grouped VS Code preview order toward that same sequence, but only during startup recovery with multiple pre-existing windows and on `Arrange Now`.
- Evenly distribute the left edges across the target monitor while keeping each window width fixed.
- Avoid elevation, admin-only setup, network access, and external services.

## Non-Goals

- Managing non-VS Code applications.
- Supporting macOS or Linux.
- Creating multiple saved layouts or workspace-specific rules.
- Providing a full desktop UI beyond the tray icon and lightweight notifications.
- Rearranging windows continuously while the user drags or resizes them manually.

## MVP Assumptions

- The app targets visible top-level windows owned by `Code.exe` and `Code - Insiders.exe`.
- The default window width is `1823` physical pixels.
- That default came from the active VS Code window titled `prd.md - window-resizer - Visual Studio Code` measured on 2026-03-12.
- The target monitor is the monitor containing the trigger window:
  - Automatic trigger: the newly shown VS Code window.
  - Manual trigger: the foreground VS Code window, if one exists; otherwise the most recently active VS Code window.
- All eligible VS Code windows are moved onto the target monitor before the final layout is applied.
- The app uses the monitor working area, not the full monitor bounds, so the layout respects the taskbar.
- If `1823` exceeds the target monitor working width, the width is clamped to the working width for that run.

## Users

- Single-user desktop workflow focused on juggling multiple VS Code windows on one monitor at a time.

## User Stories

- As a user, I want the app to start with Windows and stay out of the way so I do not have to remember to launch it.
- As a user, I want opening a new VS Code window to trigger the layout automatically so the arrangement stays consistent.
- As a user, I want to click the tray icon to reapply the layout at any time so I can recover the arrangement after manual changes.
- As a user, I want to change the layout width in a small settings UI when my preferred width changes.
- As a user, I want the windows ordered in a way that closely matches how I opened them so the layout stays intuitive.

## Functional Requirements

### FR-1 Background Process

- The app must run as a single-instance background process with no main window.
- The app must expose a tray icon while it is running.
- The app must log enough local diagnostic data to explain why a layout run succeeded or failed.

### FR-2 Startup and Durability

- The app must be configurable to start automatically when the user signs in.
- The default install/startup path should keep the app running in the interactive user session.
- The app should relaunch automatically after an unexpected process failure.

### FR-3 Automatic Trigger

- The app must watch for new VS Code top-level windows becoming visible.
- After a qualifying window appears, the app must debounce briefly and then apply the layout once the window frame is stable.
- Repeated shell events for the same window must coalesce into a single layout run.
- Automatic new-window runs must not hide/show windows to re-sequence the taskbar preview group.

### FR-4 Manual Trigger

- Clicking the tray icon must allow the user to apply the layout immediately even if no new window opened.
- `Arrange Now` must prefer the current left-to-right screen order of eligible VS Code windows, using the current window left edge as the primary sort key so the user can manually reorder windows before invoking it.
- `Arrange Now` must hide and re-show eligible VS Code windows in the resolved arrange order before applying the final bounds.
- The tray menu must include:
  - `Arrange Now`
  - `Settings...`
  - `Run at Sign-in` toggle
  - `Exit`

### FR-5 Width Setting

- The app must persist a `window width` setting in physical pixels.
- The default value must be `1823`.
- The app must expose a small Settings dialog that allows the user to edit that width.
- The Settings dialog must validate that the width is a whole-number pixel value greater than zero.
- Saving Settings must persist the new width and use it for the next arrange run.
- If the saved width is wider than the target monitor working area, the width must be clamped for that run only.

### FR-6 Window Eligibility

- Only visible, non-minimized, non-cloaked, top-level VS Code windows are eligible.
- Tool windows, hidden windows, and non-interactive helper windows must be ignored.

### FR-7 Ordering

- Eligible windows must be ordered by the app's recorded first-seen VS Code window-open sequence for the current session.
- If an eligible window existed before the app observed it, the app must fall back in this order:
  - process start time
  - process ID
  - window handle
- The ordering heuristic must be deterministic for a given set of observed windows.
- If the app starts and more than one eligible VS Code window already exists, it must run a single startup recovery arrange that also hide/show-syncs the Explorer preview order.
- If the app starts and zero or one eligible VS Code window exists, it must skip startup recovery reordering.

### FR-8 Placement

- Every eligible window must be resized to:
  - Height: target monitor working-area height
  - Width: saved `window width` setting for the run, after any needed clamp
- Windows must be placed left to right using evenly spaced left edges across the target monitor working area.
- For `N` windows, working area left `L`, working width `M`, and effective width `W`:
  - If `N = 1`, `X0 = L`
  - If `N > 1`, `step = (M - W) / (N - 1)` and `Xi = round(L + i * step)`
- `Y` is always the top of the target monitor working area.
- Overlap is allowed when `N * W` exceeds the working width. Preserving the chosen width is more important than preventing overlap.

### FR-9 Notifications

- The app must show short tray notifications for:
  - arrange failure
- Successful automatic arrange runs do not need a notification unless the user explicitly enables verbose notifications later.

### FR-10 Settings UX

- `Settings...` must open a small modal dialog owned by the tray app.
- The dialog must contain:
  - `Window width (px)` input
  - `Save`
  - `Cancel`
- `Save` must close the dialog after persisting a valid width.
- `Cancel` must close the dialog without changing the saved width.

## Quality Requirements

- Layout should apply quickly enough that the new VS Code window settles into place within about 500 ms after the shell reports it as visible, excluding unusually slow system conditions.
- The app must remain responsive while waiting for shell events.
- The app must not require admin privileges during normal operation.
- The app must keep configuration and logs in the user profile, not in the install directory.

## Edge Cases

- No eligible VS Code windows: no-op with log entry, no error notification.
- One eligible window: resize to target width and full working-area height, aligned to the left edge.
- Mixed monitor sizes: all eligible windows are placed on the trigger monitor for that run.
- Mixed DPI monitors: width remains whatever physical pixel width the user saved, so perceived size can vary between monitors.
- Explorer/taskbar restart: the tray icon must reappear and event listening must continue without user action.

## Acceptance Criteria

- With the app running, opening a new VS Code window causes all eligible VS Code windows to be rearranged on the trigger monitor using the saved width or the clamped working-area width if smaller, and full working-area height.
- Clicking `Arrange Now` from the tray performs the same layout even if no new window has opened.
- Clicking `Arrange Now` also drives the grouped VS Code taskbar preview order toward the same left-to-right order.
- If the user manually reorders VS Code windows on screen and then clicks `Arrange Now`, the resulting arrange order follows those current left edges.
- Changing `Window width (px)` in `Settings...` and clicking `Save` persists the new width and future arrange runs use it.
- When the app has observed VS Code windows opening during the current session, the left-to-right order on screen matches that observed-open sequence.
- Windows that predate the app session are placed deterministically using process start time, then PID, then window handle.
- If the app launches while multiple VS Code windows already exist, it performs one startup recovery arrange that also hide/show-syncs the grouped preview order.
- After signing out and signing back in, the app starts automatically and the tray icon is present.

## Security and Privacy

- The app should request no network permissions and send no telemetry in MVP.
- The app should run unelevated and should not attempt cross-session window management.
- Stored data is limited to local app settings and logs under the current user's profile.
