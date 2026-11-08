param(
  [string]$Src = "C:\Users\gaming\FortniteChestCounter\full.png",
  [string]$OutFile,
  [int]$X, [int]$Y, [int]$W, [int]$H
)
Add-Type -AssemblyName System.Drawing
$srcImg = [System.Drawing.Image]::FromFile($Src)
$bmp = New-Object System.Drawing.Bitmap $W, $H
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.DrawImage($srcImg, (New-Object System.Drawing.Rectangle 0, 0, $W, $H), (New-Object System.Drawing.Rectangle $X, $Y, $W, $H), [System.Drawing.GraphicsUnit]::Pixel)
$g.Dispose()
$bmp.Save($OutFile)
$g.Dispose()
$bmp.Dispose()
$srcImg.Dispose()
Write-Output "cropped: $OutFile ($W x $H)"
