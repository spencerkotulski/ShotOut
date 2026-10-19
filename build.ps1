$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& (Join-Path $root 'MakeIcon.ps1')
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"$root\ShotOut.ico" "/resource:$root\Logo.png,ShotOut.Logo" "/resource:$root\ShotOut.ico,ShotOut.Icon" /out:"$root\ShotOut.exe" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "$root\ShotOut.cs" "$root\ModernUI.cs" "$root\Refinements.cs" "$root\Arrows.cs" "$root\Settings.cs" "$root\RichText.cs" "$root\Documents.cs" "$root\LogoChanges.cs" "$root\SmoothDrag.cs" "$root\Shadows.cs" "$root\ShotOutUpdate.cs" "$root\WorkspaceUpdate.cs" "$root\DesktopUpdate.cs" "$root\CanvasSizing.cs" "$root\SmartCapture.cs" "$root\DpiBucket.cs" "$root\ImageLayerResize.cs" "$root\ChromeOpacity.cs" "$root\ModalFrameFix.cs" "$root\ResizeTools.cs" "$root\MarkupQuality.cs" "$root\EditableCrop.cs" "$root\ShiftMarkup.cs" "$root\CaptureAndEffects.cs" "$root\EditableEffects.cs" "$root\Recovery.cs" "$root\ReactiveMarkup.cs" "$root\ReleaseTools.cs" "$root\ScreenshotLibrary.cs" "$root\CloneBrush.cs" "$root\SmoothNavigation.cs"
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Write-Host "Built $root\ShotOut.exe"

