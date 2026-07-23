#Requires -Version 5
<#
    SelectSpeak setup.
    Builds a standalone SelectSpeak.exe and puts a "SelectSpeak" shortcut on your Desktop.

    Usage:
        Double-click setup.cmd
      or
        powershell -ExecutionPolicy Bypass -File setup.ps1
        powershell -ExecutionPolicy Bypass -File setup.ps1 -StartWithWindows   # also launch at login
#>
param(
    [switch]$StartWithWindows
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj = Join-Path $root 'SelectSpeak\SelectSpeak.csproj'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "The .NET SDK ('dotnet') was not found. Install .NET 10 from https://dotnet.microsoft.com/download then re-run."
}

# Close any running instance first — a running SelectSpeak.exe is locked, so the build can't
# overwrite it and you'd silently keep the old build (and old icon).
$running = Get-Process -Name SelectSpeak -ErrorAction SilentlyContinue
if ($running) {
    Write-Host 'Closing the running SelectSpeak so it can be rebuilt...' -ForegroundColor Yellow
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 500
}

Write-Host 'Building SelectSpeak (first build can take a minute)...' -ForegroundColor Cyan
dotnet publish $proj -c Release -p:PublishSingleFile=true --self-contained true -r win-x64 | Out-Null

$exe = Join-Path $root 'SelectSpeak\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish\SelectSpeak.exe'
if (-not (Test-Path $exe)) { throw "Build finished but the exe was not found at:`n$exe" }

function New-Shortcut([string]$LinkPath) {
    $shell = New-Object -ComObject WScript.Shell
    $sc = $shell.CreateShortcut($LinkPath)
    $sc.TargetPath = $exe
    $sc.WorkingDirectory = Split-Path $exe
    $sc.Description = 'SelectSpeak - select text anywhere and have it read aloud'
    $sc.IconLocation = "$exe,0"
    $sc.Save()
}

$desktop = [Environment]::GetFolderPath('Desktop')
New-Shortcut (Join-Path $desktop 'SelectSpeak.lnk')
Write-Host "Created a 'SelectSpeak' shortcut on your Desktop." -ForegroundColor Green

if ($StartWithWindows) {
    $startup = [Environment]::GetFolderPath('Startup')
    New-Shortcut (Join-Path $startup 'SelectSpeak.lnk')
    Write-Host 'SelectSpeak will now also start automatically when you sign in.' -ForegroundColor Green
}

# Nudge Windows to rebuild the icon cache so the new icon shows without a sign-out.
try { & ie4uinit.exe -show } catch {}

Write-Host ''
Write-Host 'Done! Double-click the Desktop icon to start SelectSpeak.' -ForegroundColor Green
Write-Host 'Select text anywhere, then press Ctrl+Alt+S to read it. Right-click the tray icon for Settings.' -ForegroundColor Green
Write-Host 'If a taskbar/Start pin still shows the old icon, unpin and re-pin it (Windows caches pinned icons).' -ForegroundColor DarkGray
