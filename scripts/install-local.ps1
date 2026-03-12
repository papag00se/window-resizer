param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\WindowResizer.App\WindowResizer.App.csproj"
$stagingPath = Join-Path $repoRoot "artifacts\publish\WindowResizer.App"
$installRoot = Join-Path $env:LOCALAPPDATA "WindowResizer"
$installPath = Join-Path $installRoot "current"
$exePath = Join-Path $installPath "WindowResizer.App.exe"
$startMenuProgramsPath = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs"
$shortcutPath = Join-Path $startMenuProgramsPath "Window Resizer.lnk"

if (Test-Path $stagingPath) {
    Remove-Item -Recurse -Force $stagingPath
}

dotnet publish $projectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=false `
    -o $stagingPath

$running = Get-Process -Name "WindowResizer.App" -ErrorAction SilentlyContinue
if ($running) {
    $running | Stop-Process -Force
    Start-Sleep -Milliseconds 500
}

New-Item -ItemType Directory -Force -Path $installPath | Out-Null
Copy-Item -Path (Join-Path $stagingPath "*") -Destination $installPath -Recurse -Force

New-Item -ItemType Directory -Force -Path $startMenuProgramsPath | Out-Null
$wshShell = New-Object -ComObject WScript.Shell
$shortcut = $wshShell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installPath
$shortcut.Description = "Window Resizer tray application"
$shortcut.Save()

Start-Process -FilePath $exePath

Write-Output "Installed to $installPath"
Write-Output "Created Start Menu shortcut $shortcutPath"
Write-Output "Launched $exePath"
