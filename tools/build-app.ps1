# Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#
# Publishes the HyperPDF application.
#
# The default output is a framework dependent build (the .NET 8 Desktop Runtime has to be
# installed on the target machine). Use -SelfContained to produce a standalone build.

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64',
    [string]$OutputDirectory = '',
    [switch]$SelfContained,
    [switch]$SingleFile
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'src\App\HyperPDF.csproj'

if (-not $OutputDirectory) {
    $OutputDirectory = Join-Path $repoRoot 'build\app'
}

$OutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)

function Write-Step([string]$Message) {
    Write-Host "==> $Message" -ForegroundColor Cyan
}

$arguments = @(
    'publish', $project,
    '--configuration', $Configuration,
    '--runtime', $Runtime,
    '--output', $OutputDirectory,
    '--nologo'
)

if ($SelfContained) {
    $arguments += '--self-contained'
}
else {
    $arguments += '--self-contained', 'false'
}

if ($SingleFile) {
    $arguments += '--p:PublishSingleFile=true'
    $arguments += '--p:IncludeNativeLibrariesForSelfExtract=true'
    $arguments += '--p:EnableCompressionInSingleFile=true'
}

Write-Step "Publishing HyperPDF ($Runtime, $Configuration) to '$OutputDirectory'"
& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    throw "The publish failed with exit code $LASTEXITCODE."
}

# The installer links the readme from the program group, the application ships a plain text copy.
$readme = Join-Path $repoRoot 'README.md'
if (Test-Path -LiteralPath $readme) {
    Copy-Item -LiteralPath $readme -Destination (Join-Path $OutputDirectory 'README.txt') -Force
}

Write-Step "The application is available in '$OutputDirectory'" -ForegroundColor Green
