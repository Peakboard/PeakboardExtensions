<#
.SYNOPSIS
    Packages a built Peakboard extension into its Binary/<Extension>.zip,
    with a retry loop to ride out the transient file locks Dropbox/AV put
    on freshly built DLLs.

.DESCRIPTION
    Zips every file in -BinDir EXCEPT build artefacts that must not ship
    (*.pdb, *.deps.json, *.runtimeconfig.json). This matches the documented
    repackage workflow: the extension DLL, Extension.xml and any third-party
    runtime DLLs are included automatically, so there is no per-extension
    file list to maintain.

.PARAMETER BinDir
    The build output folder, e.g.
    MicrosoftGraph\SourceCodeNew\MicrosoftGraph\bin\Release\net8.0

.PARAMETER Destination
    Full path of the .zip to (over)write, e.g.
    MicrosoftGraph\Binary\MicrosoftGraph.zip

.PARAMETER Retries
    How many attempts before giving up. Default 20.

.PARAMETER DelaySeconds
    Seconds to wait between attempts. Default 3 (so ~60s total budget, which
    comfortably covers the Dropbox/AV lock window observed on freshly built DLLs).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\pack-extension.ps1 `
        -BinDir  "C:\...\MicrosoftGraph\SourceCodeNew\MicrosoftGraph\bin\Release\net8.0" `
        -Destination "C:\...\MicrosoftGraph\Binary\MicrosoftGraph.zip"
#>
param(
    [Parameter(Mandatory = $true)] [string] $BinDir,
    [Parameter(Mandatory = $true)] [string] $Destination,
    [int] $Retries = 20,
    [int] $DelaySeconds = 3
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $BinDir)) {
    Write-Error "BinDir not found: $BinDir"
    exit 1
}

$excludedExtensions = @('.pdb', '.deps.json', '.runtimeconfig.json')

function Test-Excluded([string] $name) {
    foreach ($suffix in $excludedExtensions) {
        if ($name.ToLowerInvariant().EndsWith($suffix)) { return $true }
    }
    return $false
}

$files = Get-ChildItem -LiteralPath $BinDir -File |
    Where-Object { -not (Test-Excluded $_.Name) }

if (-not $files -or $files.Count -eq 0) {
    Write-Error "No files to package in $BinDir (after excluding build artefacts)."
    exit 1
}

$destDir = Split-Path -Parent $Destination
if (-not (Test-Path -LiteralPath $destDir)) {
    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
}

$paths = $files | ForEach-Object { $_.FullName }

for ($attempt = 1; $attempt -le $Retries; $attempt++) {
    try {
        # Remove a stale/partial zip so a failed attempt can't leave junk behind.
        if (Test-Path -LiteralPath $Destination) {
            Remove-Item -LiteralPath $Destination -Force
        }
        Compress-Archive -Path $paths -DestinationPath $Destination -Force
        Write-Output ("Packaged {0} file(s) -> {1} (attempt {2})" -f $files.Count, $Destination, $attempt)
        Write-Output ("Included: {0}" -f (($files | ForEach-Object { $_.Name }) -join ', '))
        exit 0
    }
    catch {
        if ($attempt -eq $Retries) {
            Write-Error ("Packaging failed after {0} attempts. Last error: {1}" -f $Retries, $_.Exception.Message)
            exit 1
        }
        Write-Warning ("Attempt {0}/{1} failed ({2}). Retrying in {3}s..." -f `
            $attempt, $Retries, $_.Exception.Message, $DelaySeconds)
        Start-Sleep -Seconds $DelaySeconds
    }
}
