# Window Resizer Technical Spec

Last updated: 2026-04-02

## Overview

This app is a hidden Windows desktop process with a tray icon, a shell-event observer for session ordering, a VS Code window enumerator, and a layout engine. It runs in the interactive user session and applies the same layout on demand using a saved width setting, seeded to `1823` physical pixels.

The simplest fit is a .NET 8 Windows Forms app:

- WinForms provides a stable message loop and `NotifyIcon` support for a background tray app.
- P/Invoke covers the needed Win32 APIs for window enumeration, monitor queries, DWM inspection, and positioning.
- The app stays per-user and unelevated.

## Technology Choices

- Runtime: .NET 8 for Windows
- UI shell: WinForms `ApplicationContext` with no visible main form
- Tray UI: `System.Windows.Forms.NotifyIcon`
- Native interop: P/Invoke into `user32.dll`, `dwmapi.dll`, and `shcore.dll` as needed
- Config: JSON file under `%AppData%\WindowResizer\settings.json`
- Logs: text or JSON lines under `%LocalAppData%\WindowResizer\logs\`

## External References

- `SetWinEventHook`: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook
- `EnumWindows`: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumwindows
- `DwmGetWindowAttribute`: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/nf-dwmapi-dwmgetwindowattribute
- `DWMWINDOWATTRIBUTE`: https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
- `NotifyIcon`: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/notifyicon-component-windows-forms
- Taskbar grouping and thumbnails: https://learn.microsoft.com/en-us/windows/win32/shell/taskbar-extensions
- Task Scheduler restart on failure: https://learn.microsoft.com/en-us/windows/win32/taskschd/taskschedulerschema-restartonfailure-settingstype-element

## Architecture

### 1. Bootstrap Layer

Responsibilities:

- enforce single instance
- load settings
- create the hidden application context
- initialize tray icon and menu
- register shell/window event hooks for ordering observation
- recreate the tray icon after Explorer restarts

Suggested types:

- `Program`
- `TrayApplicationContext`
- `SingleInstanceGuard`
- `AutoArrangeController`

### 2. Settings Layer

Responsibilities:

- load and save persisted user settings
- store startup preference
- keep the persisted settings model minimal

Suggested schema:

```json
{
  "windowWidthPx": 1823,
  "runAtSignIn": true
}
```

Notes:

- The default `windowWidthPx` is `1823`.
- That default came from the active VS Code window titled `prd.md - window-resizer - Visual Studio Code` measured on 2026-03-12.
- Persist settings atomically so a width change cannot leave a partially written settings file.

### 3. Event Layer

Responsibilities:

- listen for relevant shell accessibility events
- record first-seen ordering for eligible VS Code windows

Primary approach:

- Use `SetWinEventHook` with out-of-context hooks because Microsoft documents that the caller thread must have a message loop and because the hook is appropriate for receiving window events without injecting into the target process.
- Listen at minimum for:
  - `EVENT_OBJECT_SHOW`
  - `EVENT_OBJECT_LOCATIONCHANGE` only if needed during validation

Implementation notes:

- Filter aggressively to top-level windows from `Code.exe` and `Code - Insiders.exe`.
- Ignore events from child objects and non-window object IDs.
- Do not queue arrange work from this path.
- Do not treat foreground activation as a creation/open signal; taskbar clicks and normal focus changes must not change the recorded order.

Rejected approach:

- Do not use `RegisterShellHookWindow` as the primary trigger path. Microsoft marks it as not intended for general use, so it is a poor durability choice for a user utility.

### 4. Window Discovery Layer

Responsibilities:

- enumerate all eligible VS Code windows at arrange time
- inspect visibility, minimized state, cloaking, and monitor placement

Primary APIs:

- `EnumWindows`
- `GetWindowThreadProcessId`
- `GetWindowRect`
- `GetWindowPlacement`
- `IsWindowVisible`
- `GetClassName`
- `DwmGetWindowAttribute(hwnd, DWMWA_CLOAKED, ...)`

Eligibility rules:

- top-level only
- visible
- not minimized
- not cloaked
- process image name matches supported VS Code executables
- skip owned tool windows and helper surfaces that do not represent a user workspace window

### 5. Ordering Layer

Responsibilities:

- produce a deterministic left-to-right order for eligible VS Code windows that approximates user open order
- optionally synchronize Explorer's grouped preview order to that same sequence when the trigger explicitly allows it

Design decision:

- Use the app's own observed VS Code window-open events as the primary ordering source.

Why:

- Win32 does not expose a supported historical creation timestamp for `HWND`s.
- The app already receives VS Code window-open events through `SetWinEventHook`, which provides a direct session-local signal for first-seen order.
- For windows that already existed before the app saw them, process metadata is available and deterministic.

Implementation rule:

- Maintain a session-local first-seen sequence keyed by `HWND`.
- On arrange, sort eligible windows in this order:
  - observed first-seen sequence
  - process start time
  - process ID
  - window handle
- Do not fail the arrange run simply because a window predates observation; use the deterministic fallback chain instead.

Taskbar-group synchronization rule:

- Add a visibility-order synchronizer that hides windows in resolved order and shows them again in that same order without activation.
- Use this synchronizer only for explicit `Arrange Now`.

Rationale:

- Live validation on this machine showed that Explorer's grouped VS Code preview order followed the hide/show sequence.
- Restricting the behavior to manual arrange avoids needless flicker during routine shell-event observation.

Manual arrange ordering override:

- Manual `Arrange Now` should prefer the current on-screen left-to-right order of eligible windows.
- Use current window bounds from discovery and sort manual runs by:
  - current left edge
  - current top edge
  - heuristic order index as the final tie-breaker

Z-order normalization rule:

- Manual `Arrange Now` must normalize Z order after the final rectangles are applied.
- The desired stack is:
  - left-most window top-most
  - then each subsequent window directly beneath it from left to right

### 6. Layout Layer

Responsibilities:

- compute target monitor, effective width, and per-window rectangles
- move and resize windows deterministically

Target monitor selection:

- Manual trigger: monitor of the foreground VS Code window, otherwise monitor of the last active eligible VS Code window

Working area:

- Use the monitor working area from `GetMonitorInfo`, not full monitor bounds, so the layout respects the taskbar and reserved desktop areas.

Width calculation:

1. Read `windowWidthPx` from settings.
2. Validate it as a positive integer.
3. Clamp it to the target monitor working width.

```text
effectiveWidthPx = min(windowWidthPx, workingArea.Width)
```

Position calculation:

```text
N = window count
L = workingArea.Left
T = workingArea.Top
M = workingArea.Width
W = effectiveWidthPx
H = workingArea.Height

if N == 1:
  X[0] = L
else:
  step = (M - W) / (N - 1)
  X[i] = round(L + i * step)

Y[i] = T
Rect[i] = (X[i], Y[i], W, H)
```

Application:

- Use `SetWindowPos` without changing z-order.
- When taskbar synchronization is enabled, perform hide/show ordering first.
- Apply final bounds in heuristic window order from left to right.
- After manual arrange applies the final bounds, apply a follow-up Z-order normalization pass.
- Suppress redraw churn where practical, but do not introduce a second animation system.

### 7. Tray UX Layer

Tray interactions:

- Primary click: `Arrange Now`
- Context menu minimum:
  - `Arrange Now`
  - `Settings...`
  - `Run at Sign-in`
  - `Exit`

Notifications:

- Use balloon tips or modern toast notifications only for short operational status.
- Do not spam on successful arrange runs.

Settings window:

- Implement as a small modal WinForms dialog.
- Keep the surface minimal:
  - label: `Window width (px)`
  - numeric input seeded from `windowWidthPx`
  - `Save`
  - `Cancel`
- Use an integer-only control such as `NumericUpDown`.
- On `Save`, validate, persist settings, close the dialog, and let the next arrange run use the new width.
- Do not add extra configuration controls in this pass.

### 8. Startup and Durability

Preferred startup model:

- Create a per-user Scheduled Task that runs at user logon and is configured to restart on failure.

Why:

- It stays in the interactive session.
- Task Scheduler supports restart-on-failure semantics directly.
- It is more durable than a plain `Run` registry entry for an app expected to stay running all day.

Required task settings:

- trigger: at logon of current user
- run only when user is logged on
- allow start on demand
- restart on failure enabled

Startup behavior:

- After loading settings and creating services, enumerate eligible VS Code windows once to seed ordering state.
- Do not run an automatic startup arrange.

Fallback:

- If task creation is deferred for MVP, a `Run` registry entry is acceptable temporarily, but that should be documented as a durability downgrade.

### 9. Logging

Each arrange run should log:

- trigger type: manual
- trigger HWND
- target monitor
- discovered eligible HWNDs
- resolved heuristic order
- effective width and height
- applied rectangles
- failure reason if aborted

Retention:

- keep small rolling logs under the user profile
- no remote upload

## Failure Handling

Fail closed when the app cannot trust its inputs:

- No eligible windows: no-op and log.
- Invalid `windowWidthPx` in persisted settings: show an error, do not arrange, and require correction through Settings or the settings file.
- Invalid monitor info: abort and notify.
- Explorer restart: rebuild tray icon and continue ordering observation.

This is intentional. The app should not silently substitute a different ordering rule because that would violate the core requirement.

## Security Notes

- Run unelevated.
- Do not inject into other processes.
- Do not expose IPC or a listening socket in MVP.
- Do not require admin rights for normal layout runs.
- Prefer code signing before broad installation because tray apps that manage windows can look suspicious to endpoint controls when unsigned.

## Testing Plan

### Unit Tests

- `LayoutEngine` computes expected rectangles for:
  - single window
  - multiple windows with no overlap
  - multiple windows with overlap
  - width clamp when saved `windowWidthPx` exceeds working width

- `EligibilityFilter` excludes:
  - minimized windows
  - cloaked windows
  - non-VS Code processes

- `SettingsStore` preserves and reloads `windowWidthPx` and startup settings.
- `SettingsValidator` rejects zero, negative, and non-integer width values.

### Integration Tests

- With a small harness that creates real top-level test windows, verify that the app can:
  - enumerate windows
  - select a target monitor
  - apply computed bounds correctly

### Manual End-to-End Checks

- Start app at sign-in and verify tray icon presence.
- Open `Settings...`, change `Window width (px)`, save, and verify the next arrange run uses the new width.
- Left-click the tray icon and verify it behaves the same as `Arrange Now`.
- Open additional VS Code windows and verify no automatic arrange run occurs.
- Open VS Code windows in a known sequence and verify the next arrange run matches that observed-open sequence.
- Click `Arrange Now` and verify the grouped preview order follows the resolved left-to-right sequence.
- Restart Explorer and verify the tray icon returns and arrange still works.

## Delivery Sequence

1. Build the hidden tray app shell and settings store.
2. Implement VS Code window discovery and manual `Arrange Now`.
3. Add the Settings dialog and persist `windowWidthPx`, seeded to `1823`.
4. Implement width clamp behavior.
5. Implement shell-event ordering observation with deterministic fallback.
6. Implement manual `Arrange Now` tray interaction and layout application.
7. Add startup registration and restart-on-failure support.
8. Add tests for layout, eligibility, and settings persistence.
