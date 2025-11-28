# Build script for WheelMe Peakboard Extension
# This script builds the project in debug mode and puts the output into the zip file

param(
    [string]$ZipPath = "C:\Dropbox\Source\PeakboardExtensions\WheelMe\Binary\WheelmeNew.zip",
    [string]$TempExtractPath = "temp_extract",
    [string]$OutputPath = "WheelMe\bin\Debug\net8.0"
)

Write-Host "=== WheelMe Extension Build and Package Script ===" -ForegroundColor Green
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

# Step 3: Check if output directory exists
Write-Host "Step 3: Checking build output..." -ForegroundColor Yellow
if (-not (Test-Path $OutputPath)) {
    Write-Host "Error: Build output directory not found at: $OutputPath" -ForegroundColor Red
    exit 1
}

# Step 4: Create backup of original zip if it exists
Write-Host "Step 4: Creating backup of original zip..." -ForegroundColor Yellow
if (Test-Path $ZipPath) {
    $backupPath = $ZipPath -replace '\.zip$', '_backup.zip'
    Copy-Item $ZipPath $backupPath -Force
    Write-Host "  [OK] Backup created: $backupPath" -ForegroundColor Green
}

# Step 5: Create temporary directory for zip contents
Write-Host "Step 5: Preparing zip contents..." -ForegroundColor Yellow
if (Test-Path $TempExtractPath) {
    Remove-Item $TempExtractPath -Recurse -Force
}
New-Item -ItemType Directory -Path $TempExtractPath -Force

# Step 6: Copy build output files to temporary directory
Write-Host "Step 6: Copying build output to zip contents..." -ForegroundColor Yellow
$filesToPackage = @(
    "Extension.xml",
    "Newtonsoft.Json.dll", 
    "WheelMe.deps.json",
    "WheelMe.dll",
    "WheelMe.pdb",
    "WheelMe.runtimeconfig.json"
)

foreach ($file in $filesToPackage) {
    $sourceFile = Join-Path $OutputPath $file
    $destFile = Join-Path $TempExtractPath $file
    
    if (Test-Path $sourceFile) {
        Copy-Item $sourceFile $destFile -Force
        Write-Host "  [OK] Packaged: $file" -ForegroundColor Green
    } else {
        Write-Host "  [WARN] Warning: $file not found in build output" -ForegroundColor Yellow
    }
}

# Step 7: Create new zip file with build output
Write-Host "Step 7: Creating new zip file..." -ForegroundColor Yellow
if (Test-Path $ZipPath) {
    Remove-Item $ZipPath -Force
}
Compress-Archive -Path "$TempExtractPath\*" -DestinationPath $ZipPath -Force

# Step 8: Clean up temporary directory
Write-Host "Step 8: Cleaning up..." -ForegroundColor Yellow
if (Test-Path $TempExtractPath) {
    Remove-Item $TempExtractPath -Recurse -Force
}

Write-Host ""
Write-Host "=== Build and Package Complete ===" -ForegroundColor Green
Write-Host "Build output packaged into: $ZipPath" -ForegroundColor Cyan
Write-Host "Source directory: $OutputPath" -ForegroundColor Cyan
