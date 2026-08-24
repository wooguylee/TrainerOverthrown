$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$scriptPath = Join-Path $repoRoot 'tools\bootstrap-dotnet.ps1'
$source = Get-Content -LiteralPath $scriptPath -Raw

function Invoke-FakeSuccessfulScriptWithStaleNativeExitCode {
    & cmd.exe /c exit 1
    Write-Output 'installer completed'
}

$output = @(& Invoke-FakeSuccessfulScriptWithStaleNativeExitCode)
$invocationSucceeded = $?
if (-not $invocationSucceeded -or $LASTEXITCODE -ne 1 -or $output.Count -ne 1) {
    throw 'The regression fixture did not reproduce stale LASTEXITCODE behavior.'
}

if ($source -notmatch '\$installerSucceeded\s*=\s*\$\?') {
    throw 'bootstrap-dotnet.ps1 must capture PowerShell invocation success with $?.'
}

if ($source -match '\$LASTEXITCODE\s+-ne\s+0\s+-or') {
    throw 'bootstrap-dotnet.ps1 must not reject a successful script because of stale LASTEXITCODE.'
}

Write-Output 'bootstrap-dotnet regression check passed.'

