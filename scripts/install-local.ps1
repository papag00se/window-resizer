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

Start-Process -FilePath $exePath

Write-Output "Installed to $installPath"
Write-Output "Launched $exePath"
