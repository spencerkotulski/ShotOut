$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath (Join-Path $root 'ShotOut.exe'))) { & (Join-Path $root 'build.ps1') }
$scratch = Join-Path $root 'setup-build'
New-Item -ItemType Directory -Force -Path $scratch | Out-Null
$payload = Join-Path $scratch 'app.deflate'
$inputStream = [System.IO.File]::OpenRead((Join-Path $root 'ShotOut.exe'))
$outputStream = [System.IO.File]::Create($payload)
$deflater = [System.IO.Compression.DeflateStream]::new($outputStream, [System.IO.Compression.CompressionMode]::Compress)
try { $inputStream.CopyTo($deflater) } finally { $deflater.Dispose(); $inputStream.Dispose(); $outputStream.Dispose() }
$digest = (Get-FileHash -LiteralPath (Join-Path $root 'ShotOut.exe') -Algorithm SHA256).Hash.ToLowerInvariant()
$digestFile = Join-Path $scratch 'app.sha256'
[System.IO.File]::WriteAllText($digestFile, $digest)
$installer = Join-Path $root 'ShotOut-Setup-v1.3.4.exe'
& $compiler /nologo /target:winexe /optimize+ /platform:anycpu /win32icon:"$root\ShotOut.ico" /win32manifest:"$root\installer.manifest" /out:"$installer" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /main:ShotOutSetup.Program "/resource:$payload,ShotOut.App.deflate" "/resource:$digestFile,ShotOut.App.sha256" "/resource:$root\README.md,ShotOut.Setup.Readme" "$root\Installer.cs"
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed' }
Write-Host "Built $installer"
