# Window Resizer

Windows tray application for arranging Visual Studio Code windows to a consistent width, full working-area height, and heuristic open-order alignment.

## Ordering

- Primary order: first-seen VS Code window-open sequence observed by the app during the current session
- Fallback order for pre-existing windows: process start time, then PID, then window handle
- Manual `Arrange Now` override: current on-screen left-edge order, so you can reorder windows first and then reapply the layout
- Taskbar preview synchronization: hide/show ordering is applied only on startup recovery with multiple pre-existing windows and on `Arrange Now`

## Solution Layout

- `src/WindowResizer.App`: Windows Forms application shell
- `src/WindowResizer.Core`: shared core library
- `tests/WindowResizer.Core.Tests`: unit tests
- `tests/WindowResizer.App.IntegrationTests`: Windows integration tests

## Requirements

- .NET 8 SDK
- Windows desktop runtime

## Commands

```powershell
dotnet build WindowResizer.sln
dotnet test WindowResizer.sln
.\scripts\install-local.ps1
```

## Settings

- User settings are stored at `%AppData%\WindowResizer\settings.json`
- Current persisted values:
  - `windowWidthPx`
  - `runAtSignIn`

## Local Install

- Install script: `.\scripts\install-local.ps1`
- Install location: `%LocalAppData%\WindowResizer\current`
