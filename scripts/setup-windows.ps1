Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is required but was not found."
}

Write-Host "Restoring dependencies..."
dotnet restore "$repoRoot\GogGameDownloader.sln"

Write-Host "Building project..."
dotnet build "$repoRoot\src\GogGameDownloader.csproj" -c Release --no-restore

Write-Host "Setup complete."
