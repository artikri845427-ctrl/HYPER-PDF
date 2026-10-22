<#
.SYNOPSIS
    Generates the HyperPDF application icon.

.DESCRIPTION
    Draws the HyperPDF logo (a rounded accent square with a white document glyph) and writes
    it as a multi resolution .ico file. The script is deterministic, so the icon can be
    regenerated on every build and in CI.

    Author: Anwarul Haque (@HYPERMX7) | Contact: 7739399007
#>
[CmdletBinding()]
param(
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$scriptRoot = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptRoot)) {
    $scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $scriptRoot '..\assets\icon\HyperPDF.ico'
}

$Accent = [System.Drawing.Color]::FromArgb(255, 47, 85, 212)
$AccentDark = [System.Drawing.Color]::FromArgb(255, 31, 56, 154)
$Sizes = @(16, 24, 32, 48, 64, 128, 256)

function New-LogoBitmap {
    param(
        [int]$Size,
        [System.Drawing.Color]$Background,
        [System.Drawing.Color]$Foreground
    )

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.Clear([System.Drawing.Color]::Transparent)

        # Rounded background square.
        $radius = [float]($Size * 0.22)
        $backgroundPath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $diameter = $radius * 2
        $backgroundPath.AddArc(0, 0, $diameter, $diameter, 180, 90)
        $backgroundPath.AddArc($Size - $diameter, 0, $diameter, $diameter, 270, 90)
        $backgroundPath.AddArc($Size - $diameter, $Size - $diameter, $diameter, $diameter, 0, 90)
        $backgroundPath.AddArc(0, $Size - $diameter, $diameter, $diameter, 90, 90)
        $backgroundPath.CloseFigure()

        $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
            (New-Object System.Drawing.Point(0, 0)),
            (New-Object System.Drawing.Point($Size, $Size)),
            $Background,
            $AccentDark)
        $graphics.FillPath($brush, $backgroundPath)
        $brush.Dispose()
        $backgroundPath.Dispose()

        # Document glyph: a page with a folded corner.
        $pagePath = New-Object System.Drawing.Drawing2D.GraphicsPath
        $left = [float]($Size * 0.28)
        $right = [float]($Size * 0.72)
        $top = [float]($Size * 0.17)
        $bottom = [float]($Size * 0.83)
        $foldX = [float]($Size * 0.57)
        $foldY = [float]($Size * 0.36)

        $pagePath.AddLine($left, $top, $foldX, $top)
        $pagePath.AddLine($foldX, $top, $right, $foldY)
        $pagePath.AddLine($right, $foldY, $right, $bottom)
        $pagePath.AddLine($right, $bottom, $left, $bottom)
        $pagePath.CloseFigure()

        $pen = New-Object System.Drawing.Pen($Foreground, [float][Math]::Max(1.0, $Size * 0.075))
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
        $graphics.DrawPath($pen, $pagePath)
        $pagePath.Dispose()

        # Two content lines inside the page.
        $linePen = New-Object System.Drawing.Pen($Foreground, [float][Math]::Max(1.0, $Size * 0.075))
        $linePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $linePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
        $graphics.DrawLine($linePen, [float]($Size * 0.38), [float]($Size * 0.52), [float]($Size * 0.62), [float]($Size * 0.52))
        $graphics.DrawLine($linePen, [float]($Size * 0.38), [float]($Size * 0.66), [float]($Size * 0.62), [float]($Size * 0.66))
        $linePen.Dispose()
        $pen.Dispose()
    }
    finally {
        $graphics.Dispose()
    }

    return $bitmap
}

$images = @()
foreach ($size in $Sizes) {
    $bitmap = New-LogoBitmap -Size $size -Background $Accent -Foreground ([System.Drawing.Color]::White)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $bitmap.Dispose()
    $images += ,@($size, $stream.ToArray())
    $stream.Dispose()
}

$outputDirectory = Split-Path -Parent $OutputPath
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

$fileStream = [System.IO.File]::Create($OutputPath)
$writer = New-Object System.IO.BinaryWriter($fileStream)

try {
    # ICONDIR
    $writer.Write([uint16]0)      # reserved
    $writer.Write([uint16]1)      # type: icon
    $writer.Write([uint16]$images.Count)

    $offset = 6 + (16 * $images.Count)

    foreach ($image in $images) {
        $size = [int]$image[0]
        $data = [byte[]]$image[1]

        # ICONDIRENTRY
        $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
        $writer.Write([byte]($(if ($size -ge 256) { 0 } else { $size })))
        $writer.Write([byte]0)     # palette
        $writer.Write([byte]0)     # reserved
        $writer.Write([uint16]1)   # colour planes
        $writer.Write([uint16]32)  # bits per pixel
        $writer.Write([uint32]$data.Length)
        $writer.Write([uint32]$offset)

        $offset += $data.Length
    }

    foreach ($image in $images) {
        $data = [byte[]]$image[1]
        $writer.Write($data, 0, $data.Length)
    }
}
finally {
    $writer.Flush()
    $writer.Dispose()
    $fileStream.Dispose()
}

Write-Host "HyperPDF icon written to $OutputPath ($($images.Count) resolutions)."
