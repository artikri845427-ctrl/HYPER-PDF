# Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#
# Builds the native Shell extensions (thumbnail provider and preview handler) for x64 and x86.
#
# Requirements:
#   * Visual Studio Build Tools with the "Desktop development with C++" workload
#   * CMake 3.20 or newer
#
# The script builds both architectures, copies the matching PDFium library next to the
# extensions and writes the result to build\native\<architecture>.

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$BuildToolsPath = 'C:\BuildTools',
    [string]$CMakePath = '',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$sourceDirectory = Join-Path $repoRoot 'src\ThumbnailProvider'
$buildRoot = Join-Path $repoRoot 'build'
$cmakeRoot = Join-Path $buildRoot 'cmake'
$pdfiumDirectory = Join-Path $buildRoot 'pdfium'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $buildRoot 'native'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Resolve-CMake {
    if ($CMakePath -and (Test-Path -LiteralPath $CMakePath)) {
        return $CMakePath
    }

    $command = Get-Command cmake -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidate = Get-ChildItem -Path (Join-Path $env:TEMP 'opencode\dl') -Filter 'cmake.exe' -Recurse -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($candidate) {
        return $candidate.FullName
    }

    throw 'CMake was not found. Install CMake or pass -CMakePath.'
}

function Resolve-VcVars {
    $candidates = @(
        (Join-Path $BuildToolsPath 'VC\Auxiliary\Build\vcvarsall.bat'),
        (Join-Path $BuildToolsPath 'VC\Auxiliary\Build\vcvars64.bat')
    )

    foreach ($candidate in $candidates) {
        if (Test-Path -LiteralPath $candidate) {
            return $candidate
        }
    }

    throw "The Visual Studio build environment was not found below '$BuildToolsPath'."
}

$cmake = Resolve-CMake
$vcvars = Resolve-VcVars
Write-Step "CMake: $cmake"
Write-Step "Build environment: $vcvars"

if (-not (Test-Path -LiteralPath (Join-Path $pdfiumDirectory 'x64\pdfium.dll'))) {
    Write-Step 'The PDFium binaries are missing, running tools\fetch-pdfium.ps1'
    & (Join-Path $PSScriptRoot 'fetch-pdfium.ps1') -Version '158.0.8111' -OutputDirectory $pdfiumDirectory
}

$architectures = @(
    @{ Name = 'x64'; Platform = 'x64'; VsArch = 'amd64' },
    @{ Name = 'x86'; Platform = 'Win32'; VsArch = 'x86' }
)

foreach ($architecture in $architectures) {
    $buildDirectory = Join-Path $cmakeRoot $architecture.Name

    Write-Step "Configuring $($architecture.Name)"
    $configure = "call `"$vcvars`" $($architecture.VsArch) >nul && `"$cmake`" -S `"$sourceDirectory`" -B `"$buildDirectory`" -A $($architecture.Platform)"
    cmd.exe /c $configure
    if ($LASTEXITCODE -ne 0) {
        throw "The CMake configuration for $($architecture.Name) failed with exit code $LASTEXITCODE."
    }

    Write-Step "Building $($architecture.Name)"
    $build = "call `"$vcvars`" $($architecture.VsArch) >nul && `"$cmake`" --build `"$buildDirectory`" --config $Configuration --parallel"
    cmd.exe /c $build
    if ($LASTEXITCODE -ne 0) {
        throw "The build for $($architecture.Name) failed with exit code $LASTEXITCODE."
    }

    $target = Join-Path $OutputDirectory $architecture.Name
    if (Test-Path -LiteralPath $target) {
        Remove-Item -LiteralPath $target -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Path $target -Force | Out-Null

    foreach ($dll in @('HyperPDFThumbnailProvider.dll', 'HyperPDFPreviewHandler.dll')) {
        $source = Join-Path $buildDirectory "bin\$Configuration\$dll"
        if (-not (Test-Path -LiteralPath $source)) {
            $source = Join-Path $buildDirectory "$dll"
        }

        if (-not (Test-Path -LiteralPath $source)) {
            throw "The build did not produce '$dll'."
        }

        Copy-Item -LiteralPath $source -Destination (Join-Path $target $dll) -Force
    }

    Copy-Item -LiteralPath (Join-Path $pdfiumDirectory "$($architecture.Name)\pdfium.dll") `
        -Destination (Join-Path $target 'pdfium.dll') -Force

    Write-Step "The $($architecture.Name) extensions are available in '$target'" -ForegroundColor Green
}

Write-Step 'The native Shell extensions are ready.' -ForegroundColor Green
