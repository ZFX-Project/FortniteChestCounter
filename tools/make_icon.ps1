# Generates assets/app.ico from assets/chest_closed.png
# (256x256 center-cropped square wrapped in an ICO container with a PNG entry)

Add-Type -AssemblyName System.Drawing
$root = $PSScriptRoot
$src = [System.Drawing.Image]::FromFile("$root\chest_closed.png")
$bmp = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$side = [Math]::Min($src.Width, $src.Height)
$sx = [int](($src.Width - $side) / 2)
$sy = [int](($src.Height - $side) / 2)
$dest = New-Object System.Drawing.Rectangle 0, 0, 256, 256
$srcr = New-Object System.Drawing.Rectangle $sx, $sy, $side, $side
$g.DrawImage($src, $dest, $srcr, [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$ms = New-Object System.IO.MemoryStream
$bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
$bytes = $ms.ToArray()
$ms.Dispose()
$bmp.Dispose()
$src.Dispose()
$fs = [System.IO.File]::Create("$root\app.ico")
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)   # reserved
$bw.Write([uint16]1)   # type: icon
$bw.Write([uint16]1)   # image count
$bw.Write([byte]0)     # width 256 -> 0
$bw.Write([byte]0)     # height 256 -> 0
$bw.Write([byte]0)     # palette
$bw.Write([byte]0)     # reserved
$bw.Write([uint16]1)   # planes
$bw.Write([uint16]32)  # bit depth
$bw.Write([uint32]$bytes.Length)
$bw.Write([uint32]22)  # header(6) + dir(16)
$bw.Write($bytes)
$bw.Flush()
$bw.Close()
Write-Output ("app.ico created: " + (Get-Item "$root\app.ico").Length + " bytes")
