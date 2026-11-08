param([string]$OutFile = "C:\Users\gaming\FortniteChestCounter\full.png")
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$vs = [System.Windows.Forms.SystemInformation]::VirtualScreen
$bmp = New-Object System.Drawing.Bitmap $vs.Width, $vs.Height
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($vs.X, $vs.Y, 0, 0, $bmp.Size)
$bmp.Save($OutFile)
$g.Dispose()
$bmp.Dispose()
Write-Output "captured: $OutFile"
