# Build ObjectDetection and package it into Binary\ObjectDetection.zip.
#
# Not a call into tools\pack-extension.ps1, for two reasons specific to this
# extension: the shared packer enumerates its BinDir non-recursively, so it would
# silently drop the PretrainedModels subfolder and ship an extension with no
# model, and the licence gate below has to run over the staged models.
#
#   powershell -ExecutionPolicy Bypass -File Build.ps1

param(
    [string]$Configuration = "Release",
    [int]$Retries = 20,
    [int]$DelaySeconds = 3
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$project = "$root\SourceCodeNew\ObjectDetection"
$destination = "$root\Binary\ObjectDetection.zip"

Write-Host "Building ObjectDetection..." -ForegroundColor Cyan
dotnet build "$project\PeakboardExtensionObjectDetection.csproj" -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# Building through the .sln adds an x64 segment that building the .csproj does
# not, so find the output rather than assuming either shape.
$buildDir = Get-ChildItem "$project\bin" -Recurse -Filter "PeakboardExtensionObjectDetection.dll" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty DirectoryName
if (-not $buildDir) { throw "Build output not found under $project\bin" }
Write-Host "  output: $buildDir"

$stagingDir = "$env:TEMP\PeakboardExtensionObjectDetection"
if (Test-Path $stagingDir) { Remove-Item $stagingDir -Recurse -Force }
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# Build output, minus what must not ship.
$excluded = @('.pdb', '.deps.json', '.runtimeconfig.json')
Get-ChildItem -LiteralPath $buildDir -File | Where-Object {
    $name = $_.Name.ToLowerInvariant()
    $keep = $true
    foreach ($suffix in $excluded) { if ($name.EndsWith($suffix)) { $keep = $false } }
    $keep
} | ForEach-Object { Copy-Item $_.FullName "$stagingDir\" -Force }

# Models: an allowlist, not a wildcard, so an AGPL export cannot be picked up by
# dropping it in the folder.
$shippedModels = @("yolov9t.onnx", "coco_classes.txt")
$pretrainedDir = "$stagingDir\PretrainedModels"
New-Item -ItemType Directory -Path $pretrainedDir -Force | Out-Null
foreach ($f in $shippedModels) {
    $src = Join-Path "$project\PretrainedModels" $f
    if (-not (Test-Path $src)) { throw "Required model file missing: $src" }
    Copy-Item $src "$pretrainedDir\" -Force
    Write-Host "  + PretrainedModels\$f"
}

# Licence gate. Ultralytics YOLO weights are AGPL-3.0 and must not ship. The
# marker is in the ONNX metadata, so this is a content check rather than a
# filename check: renaming a file does not get past it. Do not weaken it.
Write-Host "Checking staged models for AGPL artifacts..."
Get-ChildItem "$pretrainedDir\*.onnx" | ForEach-Object {
    $text = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($_.FullName))
    if ($text -match "ultralytics") {
        throw "AGPL artifact in package: $($_.Name) carries the Ultralytics marker. Refusing to package."
    }
    Write-Host "  ok $($_.Name)"
}

# The C++ runtime is not optional: onnxruntime.dll imports it and a stock
# Peakboard Box has none in System32, so without these the extension fails there
# with 0x8007007E while working on every developer PC. See NOTICE.txt section 4.
foreach ($required in @("Extension.xml", "NOTICE.txt", "LibreYOLO-LICENSE.txt",
                        "PeakboardExtensionObjectDetection.dll",
                        "msvcp140.dll", "msvcp140_1.dll",
                        "vcruntime140.dll", "vcruntime140_1.dll")) {
    if (-not (Test-Path "$stagingDir\$required")) { throw "Missing from package: $required" }
}

# Peakboard's own assembly must not be redistributed.
if (Test-Path "$stagingDir\Peakboard.ExtensionKit.dll") {
    throw "Peakboard.ExtensionKit.dll is in the package. The project reference needs Private=false and ExcludeAssets=runtime."
}

if (-not (Test-Path "$root\Binary")) { New-Item -ItemType Directory -Path "$root\Binary" -Force | Out-Null }

for ($attempt = 1; $attempt -le $Retries; $attempt++) {
    try {
        if (Test-Path $destination) { Remove-Item $destination -Force }
        Compress-Archive -Path "$stagingDir\*" -DestinationPath $destination -CompressionLevel Optimal -Force
        break
    }
    catch {
        if ($attempt -eq $Retries) { throw "Packaging failed after $Retries attempts: $($_.Exception.Message)" }
        Write-Warning "Attempt $attempt/$Retries failed. Retrying in $DelaySeconds s..."
        Start-Sleep -Seconds $DelaySeconds
    }
}

Remove-Item $stagingDir -Recurse -Force
$size = (Get-Item $destination).Length / 1MB
Write-Host "Package created: $destination ($([math]::Round($size, 1)) MB)" -ForegroundColor Green
