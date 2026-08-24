[CmdletBinding()]
param(
    [string]$GameDir = 'W:\Games\Overthrown',
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    [switch]$ValidateOnly
)

$ErrorActionPreference = 'Stop'
$repoRoot = [System.IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$gameRoot = [System.IO.Path]::GetFullPath($GameDir)
$dotnetExe = Join-Path $repoRoot '.tools\dotnet-sdk\dotnet.exe'

function Assert-SupportedGameBuild {
    $expected = [ordered]@{
        'Overthrown.exe' = '41A3938AEC61589E85C14FC16394D558B84A568B218799C7981A2936B68D2B1D'
        'GameAssembly.dll' = '28FFF76B50ED06FC0343EC218B9465AABBE927B40655D6E36F5A5DFEE7B15B1A'
        'Overthrown_Data\il2cpp_data\Metadata\global-metadata.dat' = 'D6B15EB0DAA94C16E818619872CF313544BAF81A46D68AFE23DA45334E56BA3B'
    }

    foreach ($entry in $expected.GetEnumerator()) {
        $path = Join-Path $gameRoot $entry.Key
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Missing supported-build file: $($entry.Key)"
        }
        $actual = (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
        if ($actual -ne $entry.Value) {
            throw "Unsupported build fingerprint: $($entry.Key)"
        }
    }
}

function Assert-PathUnderRepo {
    param([Parameter(Mandatory)][string]$Path)
    $resolved = [System.IO.Path]::GetFullPath($Path)
    $prefix = $repoRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the repository: $resolved"
    }
    return $resolved
}

& (Join-Path $PSScriptRoot 'bootstrap-dotnet.ps1') | Out-Host
Assert-SupportedGameBuild
Write-Output 'Supported Overthrown build verified.'
if ($ValidateOnly) {
    exit 0
}

$solution = Join-Path $repoRoot 'VVooOverthrown.slnx'
& $dotnetExe restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $dotnetExe build $solution --no-restore --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& $dotnetExe test $solution --no-restore --no-build --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& python -m unittest (Join-Path $repoRoot 'tests\tools\test_localization_extractor.py')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$toolManifest = Get-Content -LiteralPath (Join-Path $repoRoot 'tools\tool-manifest.json') -Raw |
    ConvertFrom-Json
$bepInExRoot = Join-Path $repoRoot '.artifacts\bepinex'
$bepInExCacheValid = $true
foreach ($required in $toolManifest.bepInEx.requiredFiles) {
    $requiredPath = Join-Path $bepInExRoot $required.path
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf) -or
        (Get-FileHash -LiteralPath $requiredPath -Algorithm SHA256).Hash -ne $required.sha256) {
        $bepInExCacheValid = $false
        break
    }
}
if (-not $bepInExCacheValid) {
    & (Join-Path $PSScriptRoot 'fetch-bepinex.ps1')
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

& (Join-Path $PSScriptRoot 'stage-helper.ps1') -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& $dotnetExe run --project (Join-Path $repoRoot 'tools\VVooOverthrown.LocalizationTool') `
    --configuration $Configuration `
    --no-build `
    -- validate `
    (Join-Path $repoRoot 'translation\source.en.json') `
    (Join-Path $repoRoot 'translation\ko.json') `
    (Join-Path $repoRoot 'translation\coverage.json')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$publishRoot = Assert-PathUnderRepo (Join-Path $repoRoot '.artifacts\publish\app')
if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

& $dotnetExe publish (Join-Path $repoRoot 'src\VVooOverthrown.App\VVooOverthrown.App.csproj') `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $publishRoot `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Copy-Item -LiteralPath (Join-Path $repoRoot '.artifacts\payload') `
    -Destination (Join-Path $publishRoot 'payload') `
    -Recurse

$publishedExe = Join-Path $publishRoot 'VVooOverthrown.exe'
if (-not (Test-Path -LiteralPath $publishedExe -PathType Leaf)) {
    throw "Published executable is missing: $publishedExe"
}

Write-Output "Published executable: $publishedExe"
