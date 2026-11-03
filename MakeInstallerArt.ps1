$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$versionMatch = [regex]::Match((Get-Content (Join-Path $PSScriptRoot 'ShotOut.nsi') -Raw),'!define APP_VERSION "([^"]+)"')
$brandVersion = $versionMatch.Groups[1].Value
$assetRoot = Join-Path $PSScriptRoot 'InstallerAssets'
New-Item -ItemType Directory -Path $assetRoot -Force | Out-Null
$brandImage = [Drawing.Image]::FromFile((Join-Path $PSScriptRoot 'Logo.png'))
function Write-BrandBitmap([int]$width,[int]$height,[string]$name,[bool]$sidebar) {
    $bitmap = [Drawing.Bitmap]::new(($width*2),($height*2),[Drawing.Imaging.PixelFormat]::Format24bppRgb)
    $graphics = [Drawing.Graphics]::FromImage($bitmap)
    $fonts = [Collections.Generic.List[Drawing.Font]]::new()
    try {
        $graphics.ScaleTransform(2,2)
        $graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.TextRenderingHint = [Drawing.Text.TextRenderingHint]::AntiAliasGridFit
        if ($sidebar) {
            $gradient = [Drawing.Drawing2D.LinearGradientBrush]::new([Drawing.Rectangle]::new(0,0,$width,$height),[Drawing.Color]::FromArgb(26,28,33),[Drawing.Color]::FromArgb(25,58,94),90)
            try {$graphics.FillRectangle($gradient,0,0,$width,$height)} finally {$gradient.Dispose()}
            $graphics.DrawImage($brandImage,35,38,94,94)
            $title = [Drawing.Font]::new('Segoe UI',22,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel);$fonts.Add($title)
            $body = [Drawing.Font]::new('Segoe UI',12,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel);$fonts.Add($body)
            $small = [Drawing.Font]::new('Segoe UI',10,[Drawing.FontStyle]::Regular,[Drawing.GraphicsUnit]::Pixel);$fonts.Add($small)
            $center = [Drawing.StringFormat]::new();$center.Alignment=[Drawing.StringAlignment]::Center
            try {
                $graphics.DrawString('ShotOut',$title,[Drawing.Brushes]::White,[Drawing.RectangleF]::new(8,149,148,36),$center)
                $graphics.DrawString("Capture. Annotate.`nShare clearly.",$body,[Drawing.Brushes]::LightSteelBlue,[Drawing.RectangleF]::new(14,193,136,56),$center)
                $graphics.DrawString(("VERSION "+$brandVersion),$small,[Drawing.Brushes]::LightSteelBlue,[Drawing.RectangleF]::new(12,($height-35),140,20),$center)
            } finally {$center.Dispose()}
        } else {
            $graphics.Clear([Drawing.Color]::White);$graphics.DrawImage($brandImage,3,12,42,42)
            $title = [Drawing.Font]::new('Segoe UI',18,[Drawing.FontStyle]::Bold,[Drawing.GraphicsUnit]::Pixel);$fonts.Add($title)
            $graphics.DrawString('ShotOut',$title,[Drawing.Brushes]::Black,52,21)
        }
        $bitmap.Save((Join-Path $assetRoot $name),[Drawing.Imaging.ImageFormat]::Bmp)
    } finally {foreach($font in $fonts){$font.Dispose()};$graphics.Dispose();$bitmap.Dispose()}
}
try {Write-BrandBitmap 164 364 'Sidebar.bmp' $true;Write-BrandBitmap 150 66 'Header.bmp' $false} finally {$brandImage.Dispose()}

