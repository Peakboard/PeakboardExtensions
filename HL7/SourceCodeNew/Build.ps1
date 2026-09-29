# Build HL7 and package it into Binary\HL7.zip.
#
# The extension has no third-party dependencies and a flat output folder, so the
# shared tools\pack-extension.ps1 packer does the zipping.
#
#   powershell -ExecutionPolicy Bypass -File SourceCodeNew\Build.ps1

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot      # the extension folder
$repo = Split-Path -Parent $root
$project = "$PSScriptRoot\HL7"
$destination = "$root\Binary\HL7.zip"

Write-Host "Building HL7..." -ForegroundColor Cyan
dotnet build "$project\PeakboardExtensionHL7.csproj" -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# Building through the .sln adds an x64 segment that building the .csproj does
# not, so find the output rather than assuming either shape.
$buildDir = Get-ChildItem "$project\bin" -Recurse -Filter "PeakboardExtensionHL7.dll" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty DirectoryName
if (-not $buildDir) { throw "Build output not found under $project\bin" }
Write-Host "  output: $buildDir"

# Peakboard's own assembly must not be redistributed.
if (Test-Path "$buildDir\Peakboard.ExtensionKit.dll") {
    throw "Peakboard.ExtensionKit.dll is in the build output. The project reference needs Private=false and ExcludeAssets=runtime."
}

& "$repo\tools\pack-extension.ps1" -BinDir $buildDir -Destination $destination
if ($LASTEXITCODE -ne 0) { throw "Packaging failed." }
