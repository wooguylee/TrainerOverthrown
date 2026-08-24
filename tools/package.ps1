[CmdletBinding()]
param(
    [string]$GameDir = 'W:\Games\Overthrown'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
& (Join-Path $PSScriptRoot 'build.ps1') -GameDir $GameDir -Configuration Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$releaseRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot '.artifacts\release'))
$repoPrefix = $repoRoot.TrimEnd('\') + '\'
if (-not $releaseRoot.StartsWith($repoPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Unsafe release directory: $releaseRoot"
}
if (Test-Path -LiteralPath $releaseRoot) {
    Remove-Item -LiteralPath $releaseRoot -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseRoot -Force | Out-Null

$packageRoot = Join-Path $releaseRoot 'VVooOverthrown-win-x64'
New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot '.artifacts\publish\app\VVooOverthrown.exe') -Destination $packageRoot

$payloadRoot = Join-Path $repoRoot '.artifacts\payload'
if (Test-Path -LiteralPath $payloadRoot -PathType Container) {
    Copy-Item -LiteralPath $payloadRoot -Destination (Join-Path $packageRoot 'payload') -Recurse
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'README.md') -Destination $packageRoot
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs\project-journal.md') -Destination $packageRoot

$packagePrefix = $packageRoot.TrimEnd('\') + '\'
$manifestFiles = Get-ChildItem -LiteralPath $packageRoot -File -Recurse | ForEach-Object {
    if (-not $_.FullName.StartsWith($packagePrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Package file escaped package root: $($_.FullName)"
    }
    [ordered]@{
        path = $_.FullName.Substring($packagePrefix.Length)
        length = $_.Length
        sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    }
}
$manifest = [ordered]@{
    product = 'VVooOverthrown'
    version = '0.1.0'
    buildProfile = 'Unity 6000.1.10f1'
    files = @($manifestFiles)
}
$manifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8

$zipPath = Join-Path $releaseRoot 'VVooOverthrown-win-x64.zip'
Compress-Archive -LiteralPath $packageRoot -DestinationPath $zipPath -CompressionLevel Optimal
$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
Set-Content -LiteralPath "$zipPath.sha256" -Value "$zipHash  VVooOverthrown-win-x64.zip" -Encoding ascii

Write-Output "Release package: $zipPath"
Write-Output "SHA-256: $zipHash"
