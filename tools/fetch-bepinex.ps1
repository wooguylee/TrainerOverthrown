[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot '.artifacts'))
$destination = [IO.Path]::GetFullPath((Join-Path $artifactsRoot 'bepinex'))
$manifestPath = Join-Path $PSScriptRoot 'tool-manifest.json'
$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
$tool = $manifest.bepInEx

function Assert-ArtifactPath {
    param([Parameter(Mandatory)][string]$Path)
    $resolved = [IO.Path]::GetFullPath($Path)
    $prefix = $artifactsRoot.TrimEnd('\') + '\'
    if (-not $resolved.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "저장소 산출물 밖의 경로를 거부했습니다: $resolved"
    }
    return $resolved
}

$staging = Assert-ArtifactPath (Join-Path $artifactsRoot ("tmp\bepinex-" + [Guid]::NewGuid().ToString('N')))
$archive = Assert-ArtifactPath ($staging + '.zip')
$null = New-Item -ItemType Directory -Force $artifactsRoot
$null = New-Item -ItemType Directory -Force (Split-Path -Parent $archive)

try {
    Invoke-WebRequest -Uri $tool.url -OutFile $archive
    $actualHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
    if ($actualHash -ne $tool.sha256) {
        throw "BepInEx SHA-256 불일치: $actualHash"
    }

    Expand-Archive -LiteralPath $archive -DestinationPath $staging
    foreach ($required in $tool.requiredFiles) {
        $requiredPath = Join-Path $staging $required.path
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
            throw "BepInEx 필수 파일이 없습니다: $($required.path)"
        }
        $requiredHash = (Get-FileHash -LiteralPath $requiredPath -Algorithm SHA256).Hash
        if ($requiredHash -ne $required.sha256) {
            throw "BepInEx 필수 파일 SHA-256 불일치: $($required.path)"
        }
    }

    if (Test-Path -LiteralPath $destination) {
        Remove-Item -LiteralPath (Assert-ArtifactPath $destination) -Recurse -Force
    }
    Move-Item -LiteralPath $staging -Destination $destination
    Write-Output "BepInEx $($tool.version) 검증 완료: $destination"
}
finally {
    if (Test-Path -LiteralPath $archive) {
        Remove-Item -LiteralPath (Assert-ArtifactPath $archive) -Force
    }
    if (Test-Path -LiteralPath $staging) {
        Remove-Item -LiteralPath (Assert-ArtifactPath $staging) -Recurse -Force
    }
}
