<#
.SYNOPSIS
    Creates a small multi page PDF file that is used for the manual smoke tests and for the
    automated tests of HyperPDF.

.DESCRIPTION
    The generated file has a correct cross reference table, one page per text line and the
    document information dictionary of HyperPDF. No third party tool is required.

    Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#>
[CmdletBinding()]
param(
    [string]$OutputPath = (Join-Path $env:TEMP 'HyperPDF-Sample.pdf'),
    [int]$PageCount = 3,
    [string]$Title = 'HyperPDF Sample Document',
    [string]$Author = 'Anwarul Haque (@HYPERMX7)'
)

$ErrorActionPreference = 'Stop'

if ($PageCount -lt 1) { $PageCount = 1 }

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

$objects = New-Object System.Collections.Generic.List[string]

# 1: catalog, 2: pages, 3: font, 4: info, then page/content pairs.
$pageObjectNumbers = @()
$contentObjectNumbers = @()
$nextObjectNumber = 5
for ($i = 0; $i -lt $PageCount; $i++) {
    $pageObjectNumbers += $nextObjectNumber
    $contentObjectNumbers += ($nextObjectNumber + 1)
    $nextObjectNumber += 2
}

$objects.Add('<< /Type /Catalog /Pages 2 0 R >>')
$kids = ($pageObjectNumbers | ForEach-Object { "$_ 0 R" }) -join ' '
$objects.Add("<< /Type /Pages /Kids [$kids] /Count $PageCount >>")
$objects.Add('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>')
$objects.Add("<< /Title ($Title) /Author ($Author) /Creator (HyperPDF) /Producer (HyperPDF 1.0.0) >>")

for ($i = 0; $i -lt $PageCount; $i++) {
    $pageNumber = $i + 1
    $objects.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents $($contentObjectNumbers[$i]) 0 R >>")

    $text = "HyperPDF sample page $pageNumber"
    $stream = "BT /F1 28 Tf 60 740 Td ($text) Tj ET`nBT /F1 14 Tf 60 700 Td (Developed by Anwarul Haque (@HYPERMX7) | Contact: 7739399007) Tj ET"
    $objects.Add("<< /Length $($stream.Length) >>`nstream`n$stream`nendstream")
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($outputDirectory) -and -not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$bytes = New-Object System.Collections.Generic.List[byte]
function Add-Bytes {
    param([string]$Text)
    $bytes.AddRange([System.Text.Encoding]::GetEncoding(28591).GetBytes($Text))
}

Add-Bytes "%PDF-1.7`n"

$offsets = New-Object System.Collections.Generic.List[int]
for ($i = 0; $i -lt $objects.Count; $i++) {
    $offsets.Add($bytes.Count)
    Add-Bytes "$($i + 1) 0 obj`n$($objects[$i])`nendobj`n"
}

$xrefOffset = $bytes.Count
$count = $objects.Count + 1
Add-Bytes "xref`n0 $count`n"
Add-Bytes "0000000000 65535 f `n"
for ($i = 0; $i -lt $objects.Count; $i++) {
    Add-Bytes ('{0:D10} 00000 n ' -f $offsets[$i])
}
Add-Bytes "trailer`n<< /Size $count /Root 1 0 R /Info 4 0 R >>`nstartxref`n$xrefOffset`n%%EOF`n"

[System.IO.File]::WriteAllBytes($OutputPath, $bytes.ToArray())
Write-Host "Sample PDF written to $OutputPath ($PageCount pages, $($bytes.Count) bytes)."
