# findrun.ps1
param([string]$BatchFileName)

if (-not $BatchFileName) {
    Write-Host "Usage: ps 'batchfile.bat'"
    exit
}

# First try to find the batch file recursively from current directory
$batchFile = Get-ChildItem -Path . -Filter $BatchFileName -Recurse -File | Select-Object -First 1

if (-not $batchFile) {
    # If not found recursively, try specific common paths
    $searchPaths = @(".", "..", "..\..", "..\..\..", "dev\tools", "tools")
    foreach ($path in $searchPaths) {
        $testPath = Join-Path $path $BatchFileName
        if (Test-Path $testPath) {
            $batchFile = Get-Item $testPath
            break
        }
    }
}

if ($batchFile) {
    Write-Host "Found batch file: $($batchFile.FullName)"

    # Save current directory
    $originalDir = Get-Location

    try {
        # Change to the directory where the batch file is located
        Set-Location $batchFile.DirectoryName
        Write-Host "Changed to directory: $($batchFile.DirectoryName)"

        # Run the batch file
        & $batchFile.FullName
    }
    finally {
        # Always change back to the original directory
        Set-Location $originalDir
        Write-Host "Changed back to directory: $originalDir"
    }
} else {
    Write-Host "Batch file '$BatchFileName' not found in current directory tree."
}