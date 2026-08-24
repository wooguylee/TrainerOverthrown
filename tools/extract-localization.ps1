param(
    [Parameter(Mandatory = $false)]
    [string]$GameDir = 'W:\Games\Overthrown',

    [Parameter(Mandatory = $false)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$pythonPackages = Join-Path $projectRoot '.tools\python-packages'
$requirements = Join-Path $PSScriptRoot 'python-requirements.lock'
$extractor = Join-Path $PSScriptRoot 'extract_unity_localization.py'

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $projectRoot 'translation\source.en.json'
}

$bundleRoot = Join-Path $GameDir 'Overthrown_Data\StreamingAssets\aa\StandaloneWindows64'
$englishBundle = Join-Path $bundleRoot 'localization-string-tables-english(en)_assets_all.bundle'
$sharedBundle = Join-Path $bundleRoot 'localization-assets-shared_assets_all.bundle'

foreach ($requiredPath in @($englishBundle, $sharedBundle, $extractor, $requirements)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "필수 파일이 없습니다: $requiredPath"
    }
}

$env:PYTHONPATH = $pythonPackages
$installedVersion = & python -c "import UnityPy; print(UnityPy.__version__)" 2>$null
if ($LASTEXITCODE -ne 0 -or $installedVersion -ne '1.25.0') {
    New-Item -ItemType Directory -Force $pythonPackages | Out-Null
    & python -m pip install --disable-pip-version-check --target $pythonPackages --requirement $requirements
    if ($LASTEXITCODE -ne 0) {
        throw '고정된 UnityPy 도구 설치에 실패했습니다.'
    }
}

& python $extractor `
    --english-bundle $englishBundle `
    --shared-bundle $sharedBundle `
    --output $OutputPath
if ($LASTEXITCODE -ne 0) {
    throw '영어 문자열 추출에 실패했습니다.'
}
