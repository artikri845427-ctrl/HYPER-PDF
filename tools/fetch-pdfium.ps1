# Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#
# Provides the pinned PDFium binaries for the native Shell extensions.
#
# The primary source is the local NuGet cache of bblanchon.PDFium.Win32, because the
# application and the Shell extensions must use exactly the same PDFium build. When the
# package is not available the script downloads the matching release from GitHub.

[CmdletBinding()]
param(
    [string]$Version = '158.0.8111',
    [string]$OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'build\pdfium'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Get-NuGetPackagePath {
    $candidates = @(
        (Join-Path $env:USERPROFILE ".nuget\packages\bblanchon.pdfium.win32\$Version")
    )

    if ($env:NUGET_PACKAGES) {
        $candidates += (Join-Path $env:NUGET_PACKAGES "bblanchon.pdfium.win32\$Version")
    }

    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate)) {
            return $candidate
        }
    }

    return $null
}

function Copy-FromNuGetCache([string]$PackagePath) {
    $architectures = @{
        'x64' = 'win-x64'
        'x86' = 'win-x86'
    }

    foreach ($architecture in $architectures.Keys) {
        $source = Join-Path $PackagePath "runtimes\$($architectures[$architecture])\native\pdfium.dll"
        if (-not (Test-Path -LiteralPath $source)) {
            throw "The PDFium library '$source' does not exist."
        }

        $target = Join-Path $OutputDirectory "$architecture\pdfium.dll"
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $target -Force
        Write-Step "Copied $architecture PDFium from the NuGet cache ($([math]::Round((Get-Item $target).Length / 1MB, 1)) MB)"
    }
}

function Copy-FromGitHubRelease {
    $architectures = @{
        'x64' = 'pdfium-win-x64.tgz'
        'x86' = 'pdfium-win-x86.tgz'
    }

    $tag = [System.Uri]::EscapeDataString("chromium/$Version")
    $url = "https://api.github.com/repos/bblanchon/pdfium-binaries/releases/tags/$tag"

    Write-Step "Downloading the PDFium release $Version from GitHub"
    $release = Invoke-RestMethod -Uri $url -Headers @{ 'User-Agent' = 'HyperPDF-Build' } -UseBasicParsing

    $temporary = Join-Path ([System.IO.Path]::GetTempPath()) ("hyperpdf-pdfium-" + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $temporary -Force | Out-Null

    try {
        foreach ($architecture in $architectures.Keys) {
            $asset = $release.assets | Where-Object { $_.name -eq $architectures[$architecture] } | Select-Object -First 1
            if (-not $asset) {
                throw "The release $Version does not contain $($architectures[$architecture])."
            }

            $archive = Join-Path $temporary $asset.name
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archive -UseBasicParsing

            $extract = Join-Path $temporary $architecture
            New-Item -ItemType Directory -Path $extract -Force | Out-Null
            & tar.exe -xzf $archive -C $extract
            if ($LASTEXITCODE -ne 0) {
                throw "The archive '$archive' could not be extracted."
            }

            $dll = Get-ChildItem -Path $extract -Recurse -Filter 'pdfium.dll' | Select-Object -First 1
            if (-not $dll) {
                throw "The archive '$archive' does not contain a pdfium.dll."
            }

            $target = Join-Path $OutputDirectory "$architecture\pdfium.dll"
            New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
            Copy-Item -LiteralPath $dll.FullName -Destination $target -Force
            Write-Step "Downloaded $architecture PDFium ($([math]::Round((Get-Item $target).Length / 1MB, 1)) MB)"
        }
    }
    finally {
        Remove-Item -LiteralPath $temporary -Recurse -Force -ErrorAction SilentlyContinue
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$packagePath = Get-NuGetPackagePath
if ($packagePath) {
    Write-Step "Using the NuGet package cache '$packagePath'"
    Copy-FromNuGetCache $packagePath
}
else {
    Copy-FromGitHubRelease
}

Write-Step "The PDFium binaries are available in '$OutputDirectory'" -ForegroundColor Green
