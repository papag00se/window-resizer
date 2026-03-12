# TODO

Last updated: 2026-03-12

## Scope Coverage

### Active Phases

- `P1` Foundation and project bootstrap
- `P2` Window discovery and layout engine
- `P3` Tray app and settings UX
- `P4` Automatic arrange trigger and ordering
- `P5` Startup, packaging, and local installation
- `P6` End-to-end validation and documentation finish

### Gap Coverage

- `docs/spec/gaps.md` does not exist as of 2026-03-12, so there are no active gap IDs to map yet.

## Task Queue

- [x] `T001` Bootstrap the .NET Windows tray application solution and test projects.
  - Phase: `P1`
  - Depends on: none
  - Deliverables: solution file, app project, unit test project, e2e/integration test project, baseline build/test wiring

- [x] `T002` Implement configuration and settings persistence for `windowWidthPx` and `runAtSignIn`.
  - Phase: `P1`
  - Depends on: `T001`
  - Deliverables: settings model, atomic save/load, validation rules, tests

- [x] `T003` Implement the pure layout engine for monitor working-area placement and width clamping.
  - Phase: `P2`
  - Depends on: `T001`
  - Deliverables: rectangle calculation logic, unit tests for happy path and edge cases

- [ ] `T004` Implement VS Code window discovery and eligibility filtering.
  - Phase: `P2`
  - Depends on: `T001`
  - Deliverables: Win32 enumeration, filtering for visible top-level VS Code windows, tests

- [ ] `T005` Implement the tray application shell with `Arrange Now`, `Settings...`, `Run at Sign-in`, and `Exit`.
  - Phase: `P3`
  - Depends on: `T002`
  - Deliverables: hidden app context, tray icon, menu wiring, basic notifications, tests where feasible

- [ ] `T006` Implement the Settings dialog for editing `windowWidthPx`.
  - Phase: `P3`
  - Depends on: `T002`, `T005`
  - Deliverables: modal settings form, validation, persistence integration, tests

- [ ] `T007` Implement manual arrange execution that discovers windows and applies computed bounds.
  - Phase: `P3`
  - Depends on: `T003`, `T004`, `T005`
  - Deliverables: arrange pipeline, failure handling, integration tests with real windows where feasible

- [ ] `T008` Implement automatic VS Code window-open detection with debounce.
  - Phase: `P4`
  - Depends on: `T004`, `T007`
  - Deliverables: WinEvent hook integration, stable event filtering, tests

- [ ] `T009` Implement taskbar-order resolution for VS Code windows and fail-closed behavior when order is incomplete.
  - Phase: `P4`
  - Depends on: `T004`
  - Deliverables: order resolver, shell/UIA mapping, unit/integration tests

- [ ] `T010` Integrate taskbar ordering into arrange execution for both manual and automatic runs.
  - Phase: `P4`
  - Depends on: `T007`, `T008`, `T009`
  - Deliverables: ordered arrange flow, notifications on order failure, regression tests

- [ ] `T011` Implement startup registration and durability behavior for per-user sign-in launch and restart-on-failure.
  - Phase: `P5`
  - Depends on: `T002`, `T005`
  - Deliverables: scheduled task integration or documented fallback, toggle wiring, tests where practical

- [ ] `T012` Add packaging and local install flow for this machine.
  - Phase: `P5`
  - Depends on: `T010`, `T011`
  - Deliverables: publish/install script or installer path, machine-local install location, tray app launch verification

- [ ] `T013` Complete end-to-end validation, update docs, and verify the installed app works on this machine.
  - Phase: `P6`
  - Depends on: `T012`
  - Deliverables: full test pass, docs refresh, install verification notes, final cleanup
