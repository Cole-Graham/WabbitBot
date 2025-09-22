# Local Backup Tool

This tool provides a way to backup the entire WabbitBot solution to a local directory.

## Setup

1. Copy `config.template.ini` to `config.ini`
2. Edit `config.ini` with your desired settings

## Configuration

```ini
# WabbitBot Local Backup Configuration
# Configure the settings below

# Directory where the backup will be created
dir_to_write_backup = D:\Projects\Backup\Projects

# Whether to overwrite existing backups
overwrite = false

# Whether to backup files before overwriting them
# Only saves the most recent overwritten backup as a safety measure
# to prevent disk space issues from accumulating many redundant backups
backup_overwritten_files = true
```

### Parameters

- **dir_to_write_backup**: The root directory where backups will be stored
- **overwrite**: Whether to overwrite existing backups (boolean)
- **backup_overwritten_files**: Whether to backup overwritten files to `overwrites_backup` directory (boolean). Only saves one set of backup as a safety measure to prevent disk space accumulation.

## Usage

Run the backup script from anywhere within the solution directory (it will automatically find the .sln file):

```powershell
# Using default config path (config.ini)
.\dev\tools\local_backup\backup-solution.ps1

# Using custom config path
.\dev\tools\local_backup\backup-solution.ps1 -ConfigPath "my-config.ini"
```

### Logging

The script writes output to log files in the `logs/` directory relative to the script location:

- **`backup.log`** - Detailed log with all terminal output, timestamps, and color codes
- **`backup_summary.log`** - Clean summary log with key operations and results only

The logs directory is created automatically if it doesn't exist.

### Temp Output Log

The script always preserves a detailed output log at `temp_output.log` in the same directory as the batch file. This log contains all PowerShell output including any errors or warnings that occur during execution.

## Output Structure

```
(targetdirectory)\
├── WabbitBot\
│   └── contents\          # All solution files
│       ├── src\
│       ├── docs\
│       ├── dev\
│       ├── WabbitBot.sln
│       └── ...
└── overwrites_backup\     # Only created if overwrite=true and backup_overwritten_files=true
    └── WabbitBot\
        └── contents\      # Previous backup files before overwrite
```

## What Gets Backed Up

All files in the solution directory are backed up, excluding:

- `.git` directory
- Build outputs (`bin/`, `obj/`, `Debug/`, `Release/`)
- IDE files (`.vs/`, `.vscode/`, `*.user`, `*.suo`)
- Logs and temporary files
- NuGet packages
- Test results
- Other common development artifacts

## Overwrite Behavior

When `overwrite: true`:

- If `backup_overwritten_files: true`: Existing files are moved to `overwrites_backup` before copying new files
- If `backup_overwritten_files: false`: Existing files are overwritten without backup

When `overwrite: false`: The script will exit with a warning if a backup already exists.
