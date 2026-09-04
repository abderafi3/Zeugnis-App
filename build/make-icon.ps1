<#
.SYNOPSIS
    Builds src/ZeugnisApp.App/Resources/app.ico from one high-resolution logo image.

.DESCRIPTION
    Windows picks a different frame per context: 16 px in the title bar, 32 px in the taskbar,
    48 px in Explorer's medium view, 256 px for large tiles and the installer. An .ico with only
    small frames gets upscaled and looks blurry, so this writes all of them from one source.

    Give it a PNG of at least 256x256 (512 or 1024 is better). It does not have to be square -
    it is fitted into the square frame, never stretched. Then rebuild: the .exe, every window
    and the installer pick the new icon up.

.PARAMETER RemoveBackground
    Makes the flat background around the artwork transparent. An icon on an opaque white square
    looks unfinished on the taskbar and wrong on a dark theme. Only background connected to the
    image border is removed, so white enclosed inside the artwork survives.

.PARAMETER Backdrop
    Draws a filled circle of this colour behind the artwork, sized to what the artwork occupies.
    Use it with -RemoveBackground for a logo drawn in dark line art on a light ground: stripping
    the background outright would leave it invisible on a dark taskbar, and a square of colour
    reads as a pasted sticker, but a disc reads as part of the mark.

.EXAMPLE
    pwsh build/make-icon.ps1 -Source data\logo.png -RemoveBackground -Backdrop White
#>
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [string]$Output = "src/ZeugnisApp.App/Resources/app.ico",
    [int[]]$Sizes = @(16, 24, 32, 48, 64, 128, 256),
    [switch]$RemoveBackground,
    [string]$Backdrop,
    [int]$Tolerance = 40
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
if (-not [System.IO.Path]::IsPathRooted($Source)) { $Source = Join-Path $root $Source }
if (-not [System.IO.Path]::IsPathRooted($Output)) { $Output = Join-Path $root $Output }

if (-not (Test-Path $Source)) { throw "Source image not found: $Source" }

# Fits the source inside a square frame, keeping its aspect ratio and centring it. Stretching a
# 222x237 logo into a square visibly ovals a circular mark.
function Get-Resized([System.Drawing.Image]$image, [int]$size) {
    $bmp = New-Object System.Drawing.Bitmap $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    try {
        $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $g.Clear([System.Drawing.Color]::Transparent)

        $scale = [math]::Min($size / $image.Width, $size / $image.Height)
        $w = [int][math]::Round($image.Width * $scale)
        $h = [int][math]::Round($image.Height * $scale)
        $g.DrawImage($image, [int](($size - $w) / 2), [int](($size - $h) / 2), $w, $h)
    }
    finally { $g.Dispose() }
    return $bmp
}

<#
    Clears the flat background an exported logo usually sits on. A flood fill from the border, so
    only background actually reachable from outside the artwork goes - white inside a shape stays.
    Pixels on the boundary keep partial alpha, which is what stops a hard white fringe appearing
    around the anti-aliased edge once the frame is scaled down.
#>
function Remove-EdgeBackground([System.Drawing.Bitmap]$bmp, [int]$tolerance) {
    $w = $bmp.Width
    $h = $bmp.Height
    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite,
                          [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $stride = $data.Stride
        $pixels = New-Object byte[] ($stride * $h)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)

        # The top-left pixel is the background by definition of "flat background".
        $bgB = $pixels[0]; $bgG = $pixels[1]; $bgR = $pixels[2]

        $visited = New-Object bool[] ($w * $h)
        $queue = New-Object System.Collections.Generic.Queue[int]
        for ($x = 0; $x -lt $w; $x++) { $queue.Enqueue($x); $queue.Enqueue(($h - 1) * $w + $x) }
        for ($y = 0; $y -lt $h; $y++) { $queue.Enqueue($y * $w); $queue.Enqueue($y * $w + $w - 1) }

        while ($queue.Count -gt 0) {
            $i = $queue.Dequeue()
            if ($visited[$i]) { continue }
            $visited[$i] = $true

            $x = $i % $w
            $y = [int](($i - $x) / $w)
            $o = $y * $stride + $x * 4

            $delta = [math]::Abs([int]$pixels[$o] - $bgB) +
                     [math]::Abs([int]$pixels[$o + 1] - $bgG) +
                     [math]::Abs([int]$pixels[$o + 2] - $bgR)
            if ($delta -gt $tolerance * 3) { continue }

            # Fully background in the middle, tapering to opaque as the artwork's edge is reached.
            $pixels[$o + 3] = [byte]([math]::Min(255, [int](255 * $delta / ($tolerance * 3))))

            if ($x -gt 0)      { $queue.Enqueue($i - 1) }
            if ($x -lt $w - 1) { $queue.Enqueue($i + 1) }
            if ($y -gt 0)      { $queue.Enqueue($i - $w) }
            if ($y -lt $h - 1) { $queue.Enqueue($i + $w) }
        }

        [System.Runtime.InteropServices.Marshal]::Copy($pixels, 0, $data.Scan0, $pixels.Length)
    }
    finally { $bmp.UnlockBits($data) }
}

function ConvertTo-PngFrame([System.Drawing.Bitmap]$bmp) {
    $ms = New-Object System.IO.MemoryStream
    try {
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
        # Comma-wrapped so PowerShell hands back the byte[] itself, not its unrolled elements.
        return ,([byte[]]$ms.ToArray())
    }
    finally { $ms.Dispose() }
}

<#
    A classic DIB frame: BITMAPINFOHEADER, then bottom-up 32bpp BGRA rows, then the 1bpp AND mask
    (all zero - the alpha channel already carries transparency). GDI+ and older icon consumers only
    read this form; PNG-compressed frames are Vista+ and are conventionally used for 256 px only.
#>
function ConvertTo-DibFrame([System.Drawing.Bitmap]$bmp) {
    $w = $bmp.Width
    $h = $bmp.Height
    $rect = New-Object System.Drawing.Rectangle 0, 0, $w, $h
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,
                          [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $pixels = New-Object byte[] ($data.Stride * $h)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    }
    finally { $bmp.UnlockBits($data) }

    $maskStride = [int]([math]::Floor(($w + 31) / 32) * 4)
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    try {
        $bw.Write([uint32]40)          # biSize
        $bw.Write([int32]$w)           # biWidth
        $bw.Write([int32]($h * 2))     # biHeight: colour rows + mask rows
        $bw.Write([uint16]1)           # biPlanes
        $bw.Write([uint16]32)          # biBitCount
        $bw.Write([uint32]0)           # biCompression: BI_RGB
        $bw.Write([uint32]($w * $h * 4 + $maskStride * $h))
        $bw.Write([int32]0); $bw.Write([int32]0)   # pixels per metre
        $bw.Write([uint32]0); $bw.Write([uint32]0) # palette

        for ($y = $h - 1; $y -ge 0; $y--) {
            $bw.Write($pixels, $y * $data.Stride, $w * 4)
        }

        $mask = New-Object byte[] ($maskStride * $h)
        $bw.Write($mask, 0, $mask.Length)
        $bw.Flush()
        return ,([byte[]]$ms.ToArray())
    }
    finally { $bw.Dispose() }
}

# Copied into a 32bpp bitmap up front: the background is keyed out once, on the source, so every
# frame resamples the resulting soft alpha edge instead of each redoing the fill at its own size.
<#
    Puts a filled disc behind whatever is still opaque, sized to the artwork's bounding box.
    Returns a new bitmap; the caller disposes the old one.
#>
function Add-DiscBackdrop([System.Drawing.Bitmap]$bmp, [System.Drawing.Color]$colour) {
    $w = $bmp.Width
    $h = $bmp.Height

    $minX = $w; $minY = $h; $maxX = -1; $maxY = -1
    for ($y = 0; $y -lt $h; $y++) {
        for ($x = 0; $x -lt $w; $x++) {
            if ($bmp.GetPixel($x, $y).A -gt 16) {
                if ($x -lt $minX) { $minX = $x }
                if ($x -gt $maxX) { $maxX = $x }
                if ($y -lt $minY) { $minY = $y }
                if ($y -gt $maxY) { $maxY = $y }
            }
        }
    }
    if ($maxX -lt 0) { throw "The image is fully transparent - check -Tolerance." }

    # A hair wider than the artwork so nothing sits right on the disc's edge.
    $diameter = [int]([math]::Max($maxX - $minX, $maxY - $minY) * 1.06)
    $cx = ($minX + $maxX) / 2
    $cy = ($minY + $maxY) / 2

    $out = New-Object System.Drawing.Bitmap $w, $h, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($out)
    try {
        $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $g.Clear([System.Drawing.Color]::Transparent)
        $brush = New-Object System.Drawing.SolidBrush $colour
        try { $g.FillEllipse($brush, [single]($cx - $diameter / 2), [single]($cy - $diameter / 2), [single]$diameter, [single]$diameter) }
        finally { $brush.Dispose() }
        $g.DrawImage($bmp, 0, 0, $w, $h)
    }
    finally { $g.Dispose() }
    return $out
}

$loaded = [System.Drawing.Image]::FromFile($Source)
$original = $null
$frames = @()
try {
    $original = New-Object System.Drawing.Bitmap $loaded.Width, $loaded.Height,
                           ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($original)
    try { $g.Clear([System.Drawing.Color]::Transparent); $g.DrawImage($loaded, 0, 0, $loaded.Width, $loaded.Height) }
    finally { $g.Dispose() }
}
finally { $loaded.Dispose() }

try {
    if ($original.Width -lt 256 -or $original.Height -lt 256) {
        Write-Warning "Source is $($original.Width)x$($original.Height); the 256 px frame is upscaled and will look a little soft. Prefer 256x256 or bigger."
    }

    if ($RemoveBackground) {
        Remove-EdgeBackground $original $Tolerance
    }
    elseif ($original.GetPixel(0, 0).A -eq 255) {
        Write-Warning "The source has an opaque background, so the icon will be a solid square on the taskbar and on dark themes. Re-run with -RemoveBackground to key it out."
    }

    if ($Backdrop) {
        $withDisc = Add-DiscBackdrop $original ([System.Drawing.ColorTranslator]::FromHtml($Backdrop))
        $original.Dispose()
        $original = $withDisc
    }

    foreach ($size in ($Sizes | Sort-Object)) {
        $bmp = Get-Resized $original $size
        try {
            [byte[]]$bytes = if ($size -ge 256) { ConvertTo-PngFrame $bmp } else { ConvertTo-DibFrame $bmp }
            $frames += [pscustomobject]@{ Size = $size; Bytes = $bytes }
        }
        finally { $bmp.Dispose() }
    }
}
finally { $original.Dispose() }

# ICONDIR (6 bytes) + one 16-byte ICONDIRENTRY per frame, then the payloads.
$out = [System.IO.File]::Create($Output)
try {
    $w = New-Object System.IO.BinaryWriter($out)
    $w.Write([uint16]0)                  # reserved
    $w.Write([uint16]1)                  # type: icon
    $w.Write([uint16]$frames.Count)

    $offset = 6 + (16 * $frames.Count)
    foreach ($f in $frames) {
        # 256 is stored as 0 in the single byte the format allows.
        $dim = [byte]$(if ($f.Size -ge 256) { 0 } else { $f.Size })
        $w.Write($dim)                   # width
        $w.Write($dim)                   # height
        $w.Write([byte]0)                # palette colours (0 = truecolour)
        $w.Write([byte]0)                # reserved
        $w.Write([uint16]1)              # colour planes
        $w.Write([uint16]32)             # bits per pixel
        $w.Write([uint32]$f.Bytes.Length)
        $w.Write([uint32]$offset)
        $offset += $f.Bytes.Length
    }

    # Three-argument overload: Write(byte[]) is ambiguous enough in PowerShell that a one-byte
    # Write(byte) wins and the payloads silently vanish.
    foreach ($f in $frames) { $payload = [byte[]]$f.Bytes; $w.Write($payload, 0, $payload.Length) }
    $w.Flush()
}
finally { $out.Dispose() }

Write-Host "Wrote $Output with frames: $(($Sizes | Sort-Object) -join ', ') px" -ForegroundColor Green
Write-Host "Rebuild the app to pick it up (dotnet build)."
