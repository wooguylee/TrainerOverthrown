[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '.artifacts'))
$loaderRoot = Join-Path $artifactsRoot 'bepinex'
$payloadRoot = [IO.Path]::GetFullPath((Join-Path $artifactsRoot 'payload'))
$artifactPrefix = $artifactsRoot.TrimEnd('\') + '\'
if (-not $payloadRoot.StartsWith($artifactPrefix, [StringComparison]::OrdinalIgnoreCase)) {
    throw "안전하지 않은 payload 경로입니다: $payloadRoot"
}
if (-not (Test-Path -LiteralPath (Join-Path $loaderRoot 'winhttp.dll') -PathType Leaf)) {
    throw '검증된 BepInEx payload가 없습니다. tools\fetch-bepinex.ps1을 먼저 실행하세요.'
}

if (Test-Path -LiteralPath $payloadRoot) {
    Remove-Item -LiteralPath $payloadRoot -Recurse -Force
}
Copy-Item -LiteralPath $loaderRoot -Destination $payloadRoot -Recurse

$pluginRoot = Join-Path $payloadRoot 'BepInEx\plugins\VVooOverthrown'
$translationRoot = Join-Path $pluginRoot 'translation'
New-Item -ItemType Directory -Force $translationRoot | Out-Null
$helperOutput = Join-Path $repoRoot "src\VVooOverthrown.Helper\bin\$Configuration\net6.0"
foreach ($file in @('VVooOverthrown.Helper.dll', 'VVooOverthrown.Helper.Core.dll')) {
    $source = Join-Path $helperOutput $file
    if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
        throw "Helper 빌드 파일이 없습니다: $source"
    }
    Copy-Item -LiteralPath $source -Destination $pluginRoot
}

foreach ($file in @('source.en.json', 'ko.json', 'coverage.json')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot "translation\$file") -Destination $translationRoot
}

$fileCount = (Get-ChildItem -LiteralPath $payloadRoot -File -Recurse).Count
Write-Output "Staged payload: $payloadRoot ($fileCount files)"
