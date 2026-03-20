$buildDir = "$PSScriptRoot\bin\x64\Debug\net8.0"
$zipPath = "$PSScriptRoot\..\..\Binary\PeakboardPythonNew.zip"

$files = @(
    "Extension.xml",
    "PeakboardPython.dll",
    "PeakboardPython.pdb",
    "PeakboardPython.deps.json",
    "PeakboardPython.runtimeconfig.json",
    "IronPython.dll",
    "IronPython.Modules.dll",
    "IronPython.SQLite.dll",
    "IronPython.Wpf.dll",
    "Microsoft.Dynamic.dll",
    "Microsoft.Scripting.dll",
    "Microsoft.Scripting.Metadata.dll",
    "Mono.Unix.dll",
    "System.CodeDom.dll"
)

if (Test-Path $zipPath) { Remove-Item $zipPath }

$tempDir = Join-Path $env:TEMP "PeakboardPythonZip"
if (Test-Path $tempDir) { Remove-Item $tempDir -Recurse }
New-Item -ItemType Directory -Path $tempDir | Out-Null

foreach ($f in $files) {
    $src = Join-Path $buildDir $f
    if (Test-Path $src) {
        Copy-Item $src $tempDir
    } else {
        Write-Warning "Missing: $f"
    }
}

Compress-Archive -Path (Join-Path $tempDir "*") -DestinationPath $zipPath
Remove-Item $tempDir -Recurse

Write-Host "Zip created:" (Get-Item $zipPath).Length "bytes"
