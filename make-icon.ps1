Add-Type -AssemblyName System.Drawing
$dest = Join-Path $PSScriptRoot 'HotbarStudio/images'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
$bitmap = [System.Drawing.Bitmap]::new(512,512)
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::FromArgb(20,29,46))
$teal = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(59,193,183))
$blue = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(70,108,160))
$gold = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(255,192,79))
for ($row=0; $row -lt 2; $row++) {
 for ($col=0; $col -lt 4; $col++) {
  $g.FillRectangle($blue, 48+$col*105, 65+$row*250, 88, 88)
  $g.FillRectangle($teal, 56+$col*105, 73+$row*250, 72, 9)
 }
}
$points = [System.Drawing.Point[]]@([System.Drawing.Point]::new(125,218),[System.Drawing.Point]::new(298,218),[System.Drawing.Point]::new(298,184),[System.Drawing.Point]::new(384,256),[System.Drawing.Point]::new(298,296),[System.Drawing.Point]::new(298,262),[System.Drawing.Point]::new(125,262))
$g.FillPolygon($gold,$points)
$bitmap.Save((Join-Path $dest 'icon.png'),[System.Drawing.Imaging.ImageFormat]::Png)
$gold.Dispose(); $blue.Dispose(); $teal.Dispose(); $g.Dispose(); $bitmap.Dispose()
