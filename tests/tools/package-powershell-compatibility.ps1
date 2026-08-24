$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptPath = Join-Path $repoRoot 'tools\package.ps1'
$source = Get-Content -LiteralPath $scriptPath -Raw

if ($source -match '\[System\.IO\.Path\]::GetRelativePath') {
    throw 'package.ps1 uses Path.GetRelativePath, which is unavailable in Windows PowerShell 5.1.'
}

Write-Output 'package PowerShell compatibility check passed.'

