[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$expectedVersion = '10.0.302'
$toolsRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot '.tools'))
$dotnetDir = [System.IO.Path]::GetFullPath((Join-Path $toolsRoot 'dotnet-sdk'))

function Test-DotnetSdk {
    param([Parameter(Mandatory)][string]$InstallDirectory)

    $candidateExe = Join-Path $InstallDirectory 'dotnet.exe'
    if (-not (Test-Path -LiteralPath $candidateExe -PathType Leaf)) {
        return $false
    }

    $versionOutput = @(& $candidateExe --version 2>$null)
    return $LASTEXITCODE -eq 0 -and ($versionOutput -join '').Trim() -eq $expectedVersion
}

if (Test-DotnetSdk -InstallDirectory $dotnetDir) {
    Write-Output $expectedVersion
    exit 0
}

$expectedDotnetDir = [System.IO.Path]::GetFullPath((Join-Path $repoRoot '.tools\dotnet-sdk'))
if (-not $dotnetDir.Equals($expectedDotnetDir, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe .NET SDK destination: $dotnetDir"
}

$installId = [Guid]::NewGuid().ToString('N')
$stagingDir = Join-Path $toolsRoot "dotnet-sdk-install-$installId"
$installer = Join-Path $env:TEMP "dotnet-install-$installId.ps1"

try {
    New-Item -ItemType Directory -Path $toolsRoot -Force | Out-Null
    Invoke-WebRequest -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
    & $installer -Version $expectedVersion -InstallDir $stagingDir -NoPath
    $installerSucceeded = $?
    if (-not $installerSucceeded) {
        throw "The .NET SDK $expectedVersion installer returned a failure."
    }
    if (-not (Test-DotnetSdk -InstallDirectory $stagingDir)) {
        throw "The .NET SDK $expectedVersion installation failed validation."
    }

    if (Test-Path -LiteralPath $dotnetDir) {
        Remove-Item -LiteralPath $dotnetDir -Recurse -Force
    }
    Move-Item -LiteralPath $stagingDir -Destination $dotnetDir
}
finally {
    if (Test-Path -LiteralPath $installer) {
        Remove-Item -LiteralPath $installer -Force
    }
    if (Test-Path -LiteralPath $stagingDir) {
        Remove-Item -LiteralPath $stagingDir -Recurse -Force
    }
}

Write-Output $expectedVersion
