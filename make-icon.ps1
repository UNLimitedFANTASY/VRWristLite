$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-IconPng([int]$size) {
    $bitmap = New-Object System.Drawing.Bitmap($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $graphics.ScaleTransform($size / 256.0, $size / 256.0)
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $path.AddArc(12, 12, 64, 64, 180, 90)
    $path.AddArc(180, 12, 64, 64, 270, 90)
    $path.AddArc(180, 180, 64, 64, 0, 90)
    $path.AddArc(12, 180, 64, 64, 90, 90)
    $path.CloseFigure()
    $background = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(20, 34, 45))
    $graphics.FillPath($background, $path)
    $border = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(65, 90, 105), 3)
    $graphics.DrawPath($border, $path)
    $halo = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(35, 230, 218), 23)
    $halo.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $halo.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawArc($halo, 52, 48, 152, 152, 35, 272)
    $band = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(101, 225, 214), 11)
    $band.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $band.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawArc($band, 80, 80, 96, 96, 42, 258)
    $plate = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(34, 63, 74))
    $graphics.FillEllipse($plate, 105, 105, 46, 46)
    $spark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(246, 250, 244))
    $graphics.FillEllipse($spark, 117, 117, 22, 22)
    $graphics.FillEllipse($spark, 184, 184, 16, 16)
    $stream = New-Object System.IO.MemoryStream
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $result = $stream.ToArray()
    $stream.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
    $path.Dispose()
    $background.Dispose()
    $border.Dispose()
    $halo.Dispose()
    $band.Dispose()
    $plate.Dispose()
    $spark.Dispose()
    return ,$result
}

$sizes = @(256, 64, 48, 32, 16)
$images = @($sizes | ForEach-Object { New-IconPng $_ })
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'WristLite-icon.png'), $images[0])
$stream = New-Object System.IO.MemoryStream
$writer = New-Object System.IO.BinaryWriter($stream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$images.Count)
$offset = 6 + $images.Count * 16
for ($i = 0; $i -lt $images.Count; $i++) {
    $writer.Write([byte]($sizes[$i] % 256))
    $writer.Write([byte]($sizes[$i] % 256))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$i].Length)
    $writer.Write([uint32]$offset)
    $offset += $images[$i].Length
}
foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
$writer.Flush()
[System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot 'WristLite.ico'), $stream.ToArray())
$writer.Dispose()
$stream.Dispose()
