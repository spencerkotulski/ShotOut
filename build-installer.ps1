param([string]$NsisCompiler)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
& (Join-Path $root 'MakeInstallerArt.ps1')
if (-not (Test-Path -LiteralPath (Join-Path $root 'ShotOut.exe'))) { & (Join-Path $root 'build.ps1') }
if (-not $NsisCompiler) { $found = Get-Command makensis.exe -ErrorAction SilentlyContinue; if ($found) { $NsisCompiler = $found.Source } }
if (-not $NsisCompiler) {
    foreach ($candidate in @((Join-Path ${env:ProgramFiles(x86)} 'NSIS\makensis.exe'), (Join-Path $env:ProgramFiles 'NSIS\makensis.exe'))) {
        if (Test-Path -LiteralPath $candidate) { $NsisCompiler = $candidate; break }
    }
}
if (-not $NsisCompiler -or -not (Test-Path -LiteralPath $NsisCompiler)) { throw 'Install free NSIS from https://nsis.sourceforge.io/Download or pass -NsisCompiler C:\path\to\makensis.exe' }
Push-Location $root
try {
    & $NsisCompiler /V3 (Join-Path $root 'ShotOut.nsi')
    if ($LASTEXITCODE -ne 0) { throw 'NSIS installer compilation failed' }
} finally { Pop-Location }
Write-Host "Built NSIS installer $root\ShotOut-Setup-v1.4.3.exe"




