Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$publishDir = Join-Path $repoRoot "dist\win-x64"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\GogGameDownloader"
$startMenuDir = Join-Path $env:APPDATA "Microsoft\Windows\Start Menu\Programs\Gog Game Downloader"
$shortcutPath = Join-Path $startMenuDir "GOG Game Downloader.lnk"
$exePath = Join-Path $installDir "GogGameDownloader.exe"

function Invoke-Dotnet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Write-Host "Publishing win-x64 build..."
Invoke-Dotnet @(
    "publish",
    "$repoRoot\src\GogGameDownloader.csproj",
    "-c", "Release",
    "-r", "win-x64",
    "--self-contained", "false",
    "-o", $publishDir
)

Write-Host "Installing to $installDir..."
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Get-ChildItem -Path $installDir -Force | Remove-Item -Recurse -Force
Copy-Item -Path (Join-Path $publishDir "*") -Destination $installDir -Recurse -Force

Write-Host "Creating Start Menu shortcut..."
New-Item -ItemType Directory -Force -Path $startMenuDir | Out-Null
$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = $exePath
$shortcut.WorkingDirectory = $installDir
$shortcut.IconLocation = "$exePath,0"
$shortcut.Save()

Write-Host "Install complete."
Write-Host "Launch from Start Menu: GOG Game Downloader"
