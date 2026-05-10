Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$requiredDotnetMajor = 10
$requiredDotnetChannel = "$requiredDotnetMajor.0"
$localAppData = $env:LOCALAPPDATA
if ([string]::IsNullOrWhiteSpace($localAppData)) {
    $localAppData = [Environment]::GetFolderPath([System.Environment+SpecialFolder]::LocalApplicationData)
}

if ([string]::IsNullOrWhiteSpace($localAppData)) {
    $localAppData = Join-Path $HOME ".local\share"
}

$dotnetInstallDir = Join-Path $localAppData "Microsoft\dotnet"
$script:dotnetCommand = $null

function Invoke-Dotnet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments
    )

    & $script:dotnetCommand @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Resolve-DotnetCommand {
    $command = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $localDotnet = Join-Path $dotnetInstallDir "dotnet.exe"
    if (Test-Path $localDotnet) {
        return $localDotnet
    }

    return $null
}

function Get-InstalledSdkMajors {
    param(
        [Parameter(Mandatory = $true)]
        [string]$DotnetCommand
    )

    $sdkList = & $DotnetCommand --list-sdks
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to query installed .NET SDKs. Ensure the dotnet SDK is installed and working correctly."
    }

    return @(
        $sdkList |
            ForEach-Object {
                if ($_ -match '^(\d+)\.') {
                    [int]$Matches[1]
                }
            }
    )
}

function Add-DotnetToPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$PathToAdd
    )

    $pathEntries = @($env:PATH -split ';' | Where-Object { $_ })
    if ($pathEntries -notcontains $PathToAdd) {
        $env:PATH = "$PathToAdd;$env:PATH"
    }
}

function Install-DotnetSdk {
    Write-Host ".NET SDK $requiredDotnetChannel not found. Installing..."

    $winget = Get-Command winget -ErrorAction SilentlyContinue
    if ($winget) {
        & $winget.Source install `
            --id "Microsoft.DotNet.SDK.$requiredDotnetMajor" `
            --exact `
            --accept-package-agreements `
            --accept-source-agreements `
            --silent

        if ($LASTEXITCODE -eq 0) {
            return
        }

        Write-Warning "winget install failed with exit code $LASTEXITCODE. Falling back to dotnet-install.ps1."
    }

    New-Item -ItemType Directory -Force -Path $dotnetInstallDir | Out-Null
    $installerPath = Join-Path ([System.IO.Path]::GetTempPath()) "dotnet-install.ps1"
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $installerPath
    & powershell -NoProfile -ExecutionPolicy Bypass -File $installerPath -Channel $requiredDotnetChannel -InstallDir $dotnetInstallDir -NoPath
    if ($LASTEXITCODE -ne 0) {
        throw "Automatic .NET SDK installation failed with exit code $LASTEXITCODE."
    }

    Add-DotnetToPath -PathToAdd $dotnetInstallDir
}

$script:dotnetCommand = Resolve-DotnetCommand
$sdkMajors = @()

if ($script:dotnetCommand) {
    $sdkMajors = Get-InstalledSdkMajors -DotnetCommand $script:dotnetCommand
}

if (-not ($sdkMajors | Where-Object { $_ -ge $requiredDotnetMajor })) {
    Install-DotnetSdk
    $script:dotnetCommand = Resolve-DotnetCommand
    if (-not $script:dotnetCommand) {
        throw "Automatic .NET SDK installation completed, but dotnet could not be found on PATH. Restart PowerShell and try again."
    }

    $sdkMajors = Get-InstalledSdkMajors -DotnetCommand $script:dotnetCommand
    if (-not ($sdkMajors | Where-Object { $_ -ge $requiredDotnetMajor })) {
        throw "This project targets .NET $requiredDotnetMajor.0, but setup could not locate .NET SDK $requiredDotnetMajor or newer after installation."
    }
}

Write-Host "Restoring dependencies..."
Invoke-Dotnet @("restore", "$repoRoot\GogGameDownloader.sln")

Write-Host "Building project..."
Invoke-Dotnet @("build", "$repoRoot\src\GogGameDownloader.csproj", "-c", "Release", "--no-restore")

Write-Host "Setup complete."
