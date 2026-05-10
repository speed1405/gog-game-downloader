Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$requiredDotnetMajor = 10

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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet SDK is required but was not found."
}

$sdkList = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0) {
    throw "Failed to query installed .NET SDKs. Ensure the dotnet SDK is installed and working correctly."
}

$sdkMajors = @(
    $sdkList |
        ForEach-Object {
            if ($_ -match '^(\d+)\.') {
                [int]$Matches[1]
            }
        }
)

if (-not ($sdkMajors | Where-Object { $_ -ge $requiredDotnetMajor })) {
    throw "This project targets .NET $requiredDotnetMajor.0. Install .NET SDK $requiredDotnetMajor or newer from https://aka.ms/dotnet/download."
}

Write-Host "Restoring dependencies..."
Invoke-Dotnet @("restore", "$repoRoot\GogGameDownloader.sln")

Write-Host "Building project..."
Invoke-Dotnet @("build", "$repoRoot\src\GogGameDownloader.csproj", "-c", "Release", "--no-restore")

Write-Host "Setup complete."
