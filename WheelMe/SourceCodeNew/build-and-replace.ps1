# Build script for WheelMe Peakboard Extension
# This script builds the project in debug mode and replaces the output with files from WheelmeNew.zip

param(
    [string]$ZipPath = "C:\Dropbox\Source\PeakboardExtensions\WheelMe\Binary\WheelmeNew.zip",
    [string]$TempExtractPath = "temp_extract",
    [string]$OutputPath = "WheelMe\bin\Debug\net8.0"
)

Write-Host "=== WheelMe Extension Build and Replace Script ===" -ForegroundColor Green
Write-Host ""

# Step 1: Clean the project
Write-Host "Step 1: Cleaning the project..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to clean the project" -ForegroundColor Red
    exit 1
}

# Step 2: Build the project in debug mode
Write-Host "Step 2: Building the project in debug mode..." -ForegroundColor Yellow
dotnet build --configuration Debug
if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to build the project" -ForegroundColor Red
    exit 1
}

# Step 3: Check if zip file exists
Write-Host "Step 3: Checking zip file..." -ForegroundColor Yellow
if (-not (Test-Path $ZipPath)) {
    Write-Host "Error: Zip file not found at: $ZipPath" -ForegroundColor Red
    exit 1
}

# Step 4: Extract zip file to temporary location
Write-Host "Step 4: Extracting zip file..." -ForegroundColor Yellow
if (Test-Path $TempExtractPath) {
    Remove-Item $TempExtractPath -Recurse -Force
}
Expand-Archive -Path $ZipPath -DestinationPath $TempExtractPath -Force

# Step 5: Ensure output directory exists
Write-Host "Step 5: Preparing output directory..." -ForegroundColor Yellow
if (-not (Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force
}

# Step 6: Replace files in output directory
Write-Host "Step 6: Replacing files in output directory..." -ForegroundColor Yellow
$filesToReplace = @(
    "Extension.xml",
    "Newtonsoft.Json.dll", 
    "WheelMe.deps.json",
    "WheelMe.dll",
    "WheelMe.pdb",
    "WheelMe.runtimeconfig.json"
)

foreach ($file in $filesToReplace) {
    $sourceFile = Join-Path $TempExtractPath $file
    $destFile = Join-Path $OutputPath $file
    
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile $destFile -Force
        Write-Host "  [OK] Replaced: $file" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Warning: $file not found in zip" -ForegroundColor Yellow
    }
}

# Step 7: Clean up temporary extraction
Write-Host "Step 7: Cleaning up..." -ForegroundColor Yellow
if (Test-Path $TempExtractPath) {
    Remove-Item $TempExtractPath -Recurse -Force
}

Write-Host ""
Write-Host "=== Build and Replace Complete ===" -ForegroundColor Green
Write-Host "Output directory: $OutputPath" -ForegroundColor Cyan
Write-Host "Files replaced from: $ZipPath" -ForegroundColor Cyan