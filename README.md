# Window Resizer

Windows tray application for arranging Visual Studio Code windows to a consistent width, full working-area height, and taskbar-aligned order.

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
```
