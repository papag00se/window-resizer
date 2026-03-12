# Local Install

Last updated: 2026-03-12

## Install Command

```powershell
.\scripts\install-local.ps1
```

## Install Location

- `%LocalAppData%\WindowResizer\current`

## What The Script Does

- publishes `src/WindowResizer.App`
- copies the published files into the local install directory
- stops an existing `WindowResizer.App` process if one is already running
- launches the installed executable

## Latest Verification

- Date: 2026-03-12
- Installed executable: `C:\Users\jland\AppData\Local\WindowResizer\current\WindowResizer.App.exe`
- Verified running process path matched the installed executable
- Verified `dotnet test WindowResizer.sln` passed after installation
