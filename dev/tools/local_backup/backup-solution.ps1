param(
    [string]$ConfigPath = "config.ini"
)

# Function to write colored output and log to file
function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$Color = "White",
        [string]$LogFile = "backup.log",
        [switch]$SummaryOnly
    )
    Write-Host $Message -ForegroundColor $Color

    # Ensure logs directory exists
    $logsDir = Join-Path $PSScriptRoot "logs"
    if (-not (Test-Path $logsDir)) {
        New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
    }

    # Write to detailed log file (backup.log) - exact terminal output
    $detailedLogPath = Join-Path $logsDir "backup.log"
    $Message | Out-File -FilePath $detailedLogPath -Append -Encoding UTF8

    # Write to summary log file if SummaryOnly is specified or for important messages
    if ($SummaryOnly -or $LogFile -eq "backup_summary.log") {
        $summaryLogPath = Join-Path $logsDir "backup_summary.log"
        # For summary log, only include clean messages without color codes
        "$timestamp : $Message" | Out-File -FilePath $summaryLogPath -Append -Encoding UTF8
    }
}

# Function to create directory if it doesn't exist
function New-Directory {
    param([string]$Path)
    if (-not (Test-Path $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
        Write-ColorOutput "Created directory: $Path" "Green"
    }
}

# Function to parse INI configuration file
function Get-IniFile {
    param([string]$Path)

    $config = @{}
    $content = Get-Content $Path -ErrorAction Stop

    foreach ($line in $content) {
        $line = $line.Trim()

        # Skip empty lines and comments (lines starting with # or ;)
        if ([string]::IsNullOrEmpty($line) -or $line.StartsWith("#") -or $line.StartsWith(";")) {
            continue
        }

        # Parse key=value pairs
        if ($line -match "^([^=]+)=(.*)$") {
            $key = $matches[1].Trim()
            $value = $matches[2].Trim()

            # Convert string values to appropriate types
            if ($value -eq "true") {
                $config[$key] = $true
            }
            elseif ($value -eq "false") {
                $config[$key] = $false
            }
            elseif ($value -match "^\d+$") {
                $config[$key] = [int]$value
            }
            else {
                $config[$key] = $value
            }
        }
    }

    return $config
}

# Function to find solution file by walking up directory tree
function Find-SolutionFile {
    param([string]$StartPath = $PWD.Path)

    $currentPath = $StartPath
    while ($currentPath -and (Test-Path $currentPath)) {
        $slnFiles = Get-ChildItem -Path $currentPath -Filter "*.sln" -File
        if ($slnFiles.Count -gt 0) {
            return $slnFiles[0]
        }

        # Move up one directory
        $parentPath = Split-Path $currentPath -Parent
        if ($parentPath -eq $currentPath) {
            # Reached root directory
            break
        }
        $currentPath = $parentPath
    }

    return $null
}

# Clear the logs at the start of each run
$logsDir = Join-Path $PSScriptRoot "logs"
if (-not (Test-Path $logsDir)) {
    New-Item -ItemType Directory -Path $logsDir -Force | Out-Null
}

# Clear backup.log (detailed log)
$detailedLogPath = Join-Path $logsDir "backup.log"
"" | Out-File -FilePath $detailedLogPath -Encoding UTF8  # Clear/create empty detailed log

# Clear backup_summary.log (summary log)
$summaryLogPath = Join-Path $logsDir "backup_summary.log"
"" | Out-File -FilePath $summaryLogPath -Encoding UTF8  # Clear/create empty summary log

# Read configuration
if (-not (Test-Path $ConfigPath)) {
    Write-ColorOutput "Configuration file not found: $ConfigPath" "Red"
    Write-ColorOutput "Please copy config.template.ini to config.ini and configure the settings." "Yellow"
    exit 1
}

try {
    $config = Get-IniFile $ConfigPath
}
catch {
    Write-ColorOutput "Error reading configuration file: $($_.Exception.Message)" "Red"
    exit 1
}

# Validate configuration
if (-not $config.dir_to_write_backup) {
    Write-ColorOutput "Error: dir_to_write_backup is required in config.ini" "Red"
    exit 1
}

$targetDirectory = $config.dir_to_write_backup
$overwrite = $config.overwrite -eq $true
$backupOverwritten = $config.backup_overwritten_files -eq $true

# Get solution information
$solutionFile = Find-SolutionFile
if (-not $solutionFile) {
    Write-ColorOutput "No .sln file found in current directory or any parent directories" "Red"
    exit 1
}

$solutionName = [System.IO.Path]::GetFileNameWithoutExtension($solutionFile.Name)
$solutionDirectory = Split-Path $solutionFile.FullName -Parent

Write-ColorOutput "Solution: $solutionName" "Cyan" -SummaryOnly
Write-ColorOutput "Source: $solutionDirectory" "Cyan" -SummaryOnly
Write-ColorOutput "Target: $targetDirectory" "Cyan" -SummaryOnly

# Define backup paths
$backupRoot = Join-Path $targetDirectory $solutionName
$backupContents = Join-Path $backupRoot "contents"
$overwritesBackup = Join-Path (Split-Path $targetDirectory -Parent) "overwrites_backup"
$overwritesBackupSolution = Join-Path $overwritesBackup $solutionName

# Check if backup already exists
$backupExists = Test-Path $backupContents
if ($backupExists -and -not $overwrite) {
    Write-ColorOutput "Backup already exists and overwrite is disabled. Use overwrite: true in config.json to overwrite." "Yellow" -SummaryOnly
    exit 0
}

# Create directories
New-Directory $backupRoot
New-Directory $backupContents

# Only create overwrites_backup directories if there are files to backup
if ($overwrite -and $backupOverwritten -and $backupExists) {
    $existingFiles = Get-ChildItem $backupContents -Recurse -File -ErrorAction SilentlyContinue
    if ($existingFiles.Count -gt 0) {
        New-Directory $overwritesBackup
        New-Directory $overwritesBackupSolution
        Write-ColorOutput "Found $($existingFiles.Count) existing files to backup before overwrite" "Yellow"
    }
    else {
        Write-ColorOutput "No existing files to backup (directory is empty)" "Gray"
    }
}

Write-ColorOutput "Starting backup..." "Green"

# Get all files to backup (excluding common excludes)
$excludes = @(
    ".git",
    ".vscode",
    ".cursor",
    "node_modules",
    "bin",
    "obj",
    "*.log",
    "*.tmp",
    "*.cache",
    ".DS_Store",
    "Thumbs.db",
    "*.user",
    "*.suo",
    "*.userosscache",
    "*.sln.ide",
    "packages",
    ".nuget",
    "TestResults",
    "*.testsettings",
    "*.vspscc",
    "_ReSharper*",
    "*.sublime-*",
    ".vs",
    ".idea"
)

# Build exclude patterns for robocopy
$excludePatterns = $excludes | ForEach-Object { "/XD `"$_`"" }
$excludeFiles = $excludes | Where-Object { $_ -notmatch "^\." -and $_ -notmatch "\*$" } | ForEach-Object { "/XF `"$_`"" }

# If backup exists and we need to backup overwritten files (only if overwrites_backup directory was created)
if ($overwrite -and $backupOverwritten -and (Test-Path $overwritesBackupSolution)) {
    Write-ColorOutput "Backing up existing files to overwrites_backup..." "Yellow"

    # Use robocopy to mirror the existing backup to overwrites_backup
    $robocopyArgs = @(
        "`"$backupContents`"",
        "`"$overwritesBackupSolution`"",
        "/MIR",
        "/NJH",
        "/NJS",
        "/V"  # Verbose output
    ) + $excludePatterns + $excludeFiles

    $robocopyCommand = "robocopy " + ($robocopyArgs -join " ")
    Write-ColorOutput "Executing: $robocopyCommand" "Gray"

    # Capture and log robocopy output
    $robocopyOutput = Invoke-Expression $robocopyCommand 2>&1
    $robocopyOutput | ForEach-Object {
        Write-ColorOutput $_.ToString() "Gray"
    }
    if ($LASTEXITCODE -ge 8) {
        Write-ColorOutput "Warning: Robocopy encountered issues during overwrite backup (Exit code: $LASTEXITCODE)" "Yellow"
    }
    else {
        Write-ColorOutput "Overwrite backup completed" "Green" -SummaryOnly
    }
}

# Perform the backup
Write-ColorOutput "Copying files to backup location..." "Green"

$robocopyArgs = @(
    "`"$solutionDirectory`"",
    "`"$backupContents`"",
    "/MIR",
    "/NJH",
    "/NJS",
    "/V"  # Verbose output
) + $excludePatterns + $excludeFiles

$robocopyCommand = "robocopy " + ($robocopyArgs -join " ")
Write-ColorOutput "Executing: $robocopyCommand" "Gray"

# Capture and log robocopy output
$robocopyOutput = Invoke-Expression $robocopyCommand 2>&1
$robocopyOutput | ForEach-Object {
    Write-ColorOutput $_.ToString() "Gray"
}

# Check robocopy exit code
switch ($LASTEXITCODE) {
    0 { Write-ColorOutput "Backup completed successfully - no files copied (source and destination identical)" "Green" -SummaryOnly }
    1 { Write-ColorOutput "Backup completed successfully" "Green" -SummaryOnly }
    2 { Write-ColorOutput "Backup completed successfully - extra files removed from destination" "Green" -SummaryOnly }
    3 { Write-ColorOutput "Backup completed successfully - some files copied, some removed" "Green" -SummaryOnly }
    { $_ -ge 8 } {
        Write-ColorOutput "Error: Robocopy failed with exit code $LASTEXITCODE" "Red" -SummaryOnly
        exit 1
    }
}

# Summary
$sourceFiles = (Get-ChildItem $solutionDirectory -Recurse -File | Measure-Object).Count
$backupFiles = (Get-ChildItem $backupContents -Recurse -File | Measure-Object).Count

Write-ColorOutput "Backup Summary:" "Cyan" -SummaryOnly
Write-ColorOutput "  Source files: $sourceFiles" "White" -SummaryOnly
Write-ColorOutput "  Backup files: $backupFiles" "White" -SummaryOnly
Write-ColorOutput "  Backup location: $backupContents" "White" -SummaryOnly

if (Test-Path $overwritesBackupSolution) {
    Write-ColorOutput "  Overwritten files backup: $overwritesBackupSolution" "White" -SummaryOnly
}

Write-ColorOutput "Backup completed successfully!" "Green" -SummaryOnly
