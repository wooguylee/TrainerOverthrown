[CmdletBinding()]
param(
    [string]$GameDir = 'W:\Games\Overthrown'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$gameRoot = [IO.Path]::GetFullPath($GameDir)
$gamePrefix = $gameRoot.TrimEnd('\') + '\'
$manifestPath = Join-Path $repoRoot '.artifacts\analysis-loader-manifest.json'
if (Get-Process Overthrown -ErrorAction SilentlyContinue) {
    throw '게임 실행 중에는 분석 payload를 제거할 수 없습니다.'
}
if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
    throw "분석 설치 manifest가 없습니다: $manifestPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$verifiedTargets = @()
foreach ($entry in $manifest.Files) {
    $target = [IO.Path]::GetFullPath((Join-Path $gameRoot $entry.RelativePath))
    if (-not $target.StartsWith($gamePrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "게임 폴더 밖의 제거 경로입니다: $target"
    }
    if (-not (Test-Path -LiteralPath $target -PathType Leaf)) {
        throw "분석 설치 파일이 없습니다: $target"
    }
    $actualHash = (Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash
    $actualLength = (Get-Item -LiteralPath $target).Length
    if ($actualHash -ne $entry.Sha256 -or $actualLength -ne $entry.Length) {
        throw "분석 설치 후 변경된 파일은 제거하지 않습니다: $target"
    }
    $verifiedTargets += $target
}

$pluginRoot = [IO.Path]::GetFullPath((Join-Path $gameRoot 'BepInEx\plugins\VVooOverthrown'))
if (-not $pluginRoot.Equals(
        [IO.Path]::GetFullPath('W:\Games\Overthrown\BepInEx\plugins\VVooOverthrown'),
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "예상하지 않은 분석 plugin 경로입니다: $pluginRoot"
}

foreach ($target in $verifiedTargets) {
    Remove-Item -LiteralPath $target -Force
}
if (Test-Path -LiteralPath $pluginRoot) {
    Remove-Item -LiteralPath $pluginRoot -Recurse -Force
}

Write-Output "Removed analysis loader files: $($verifiedTargets.Count)"
Write-Output "Preserved generated interop: $(Test-Path -LiteralPath (Join-Path $gameRoot 'BepInEx\interop\GameAssembly.dll'))"
