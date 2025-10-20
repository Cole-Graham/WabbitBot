# Storage Configuration Migration Guide

## Overview

WabbitBot now supports **configurable file storage paths**, allowing persistent data (replays, images, etc.) to be stored separately from application binaries. This ensures data survives application updates on production servers.

## What Changed

### 1. New Configuration Section

Added `Storage` configuration section to `appsettings.json`:

```json
{
  "Bot": {
    "Storage": {
      "BaseDataDirectory": "data",
      "ReplaysDirectory": "data/replays",
      "ImagesDirectory": "data/images",
      "MapsDirectory": "data/images/maps/discord",
      "DivisionIconsDirectory": "data/divisions/icons",
      "DiscordComponentImagesDirectory": "data/images/discord",
      "DefaultDiscordImagesDirectory": "data/images/default/discord"
    }
  }
}
```

### 2. New StorageOptions Class

**Location:** `WabbitBot.Common.Configuration.StorageOptions`

Provides:
- Configuration binding for storage paths
- `ResolvePath()` method that converts relative paths to absolute
- Supports both relative paths (development) and absolute paths (production)

### 3. Updated FileSystemService

**Changes:**
- Constructor now accepts `StorageOptions` parameter
- Removed restriction requiring files to be within app directory
- Security checks now validate against path traversal, not location
- Paths are resolved at initialization using `StorageOptions.ResolvePath()`

### 4. Updated CoreService

**Changes:**
- `InitializeFileSystemService()` now requires `StorageOptions` as first parameter
- Passes storage configuration to FileSystemService constructor

### 5. Updated Program.cs

**Changes:**
- Loads `StorageOptions` from configuration during startup
- Passes storage options to `CoreService.InitializeFileSystemService()`

## Migration Path

### For Development (No Changes Needed)

Your existing development setup continues to work unchanged:
- Uses relative paths in `appsettings.json`
- Data stored in `data/` directory within project
- User secrets still work for bot token

### For Production Deployment

#### Option 1: Keep Data in App Directory (Easier, Less Ideal)

If you want to keep the current behavior:
- No changes needed to configuration
- Default relative paths still work
- **However**, data will be lost when updating the application

#### Option 2: Use Linux FHS Structure (Recommended)

Follow Linux Filesystem Hierarchy Standard:

**Directory Structure:**
```
/opt/wabbitbot/              # Application binaries
/var/lib/wabbitbot/          # Persistent data
  ├── replays/
  ├── images/
  │   ├── maps/discord/
  │   ├── discord/
  │   └── default/discord/
  └── divisions/icons/
```

**Create `appsettings.Production.json`:**
```json
{
  "Bot": {
    "Storage": {
      "BaseDataDirectory": "/var/lib/wabbitbot",
      "ReplaysDirectory": "/var/lib/wabbitbot/replays",
      "ImagesDirectory": "/var/lib/wabbitbot/images",
      "MapsDirectory": "/var/lib/wabbitbot/images/maps/discord",
      "DivisionIconsDirectory": "/var/lib/wabbitbot/divisions/icons",
      "DiscordComponentImagesDirectory": "/var/lib/wabbitbot/images/discord",
      "DefaultDiscordImagesDirectory": "/var/lib/wabbitbot/images/default/discord"
    }
  }
}
```

**Setup Commands:**
```bash
# Create directories
sudo mkdir -p /var/lib/wabbitbot/{replays,images/{maps/discord,discord,default/discord},divisions/icons}

# Set permissions
sudo chown -R www-data:www-data /var/lib/wabbitbot
sudo chmod 755 /var/lib/wabbitbot
sudo chmod 775 /var/lib/wabbitbot/replays
sudo chmod 775 /var/lib/wabbitbot/images/discord

# Copy default images
sudo cp -r /opt/wabbitbot/data/images/default/discord/* /var/lib/wabbitbot/images/default/discord/
```

## Benefits

### Before This Change

| Action | Result |
|--------|--------|
| Update application | ❌ All replay files lost |
| Update application | ❌ All custom images lost |
| Update application | ✅ Database preserved (separate) |

### After This Change (with proper config)

| Action | Result |
|--------|--------|
| Update application binaries | ✅ All replay files preserved |
| Update application binaries | ✅ All custom images preserved |
| Update application binaries | ✅ Database preserved |
| Update configuration | ⚠️  Requires manual merge |

## Backward Compatibility

The changes are **fully backward compatible**:

1. **Default Behavior**: If no `Storage` section is provided, defaults to relative paths (same as before)
2. **Existing Deployments**: Continue working without any changes
3. **Optional Migration**: You can migrate to FHS structure at your convenience

## Testing

To verify the configuration is working:

```bash
# Check where files are being stored
ls -la /var/lib/wabbitbot/replays/

# Check application logs for storage initialization
sudo journalctl -u wabbitbot -f | grep -i "storage\|directory"

# Upload a replay file and verify location
# (Upload via Discord, then check the replays directory)
```

## Troubleshooting

### Problem: Application can't create directories

**Error:** `Failed to create directory: /var/lib/wabbitbot/replays`

**Solution:**
```bash
sudo mkdir -p /var/lib/wabbitbot/replays
sudo chown -R www-data:www-data /var/lib/wabbitbot
```

### Problem: Permission denied when saving files

**Error:** `Access to the path '/var/lib/wabbitbot/replays/xyz.rpl3' is denied`

**Solution:**
```bash
sudo chown -R www-data:www-data /var/lib/wabbitbot
sudo chmod 775 /var/lib/wabbitbot/replays
```

### Problem: Default images not showing

**Error:** Images display as broken links

**Solution:**
```bash
# Copy default images to persistent storage
sudo cp -r /opt/wabbitbot/data/images/default/discord/* /var/lib/wabbitbot/images/default/discord/
sudo chown -R www-data:www-data /var/lib/wabbitbot/images
```

## Files Modified

### Core Changes
- `src/WabbitBot.Common/Configuration/Configuration.cs` - Added `StorageOptions` class
- `src/WabbitBot.Core/Common/Services/FileSystemService.cs` - Updated constructor and security checks
- `src/WabbitBot.Core/Common/Services/CoreService.cs` - Updated initialization method signature
- `src/WabbitBot.Host/Program.cs` - Load and pass storage configuration

### Configuration
- `src/WabbitBot.Host/appsettings.json` - Added default `Storage` section

### Documentation
- `docs/.dev/hosting/hostinger-deployment.md` - Added FHS structure guide and storage configuration
- `docs/.dev/hosting/storage-configuration-migration.md` - This document
- `docs/.dev/hosting/hostinger.md` - Updated with deployment references

## Related Documentation

- [Hostinger Deployment Guide](./hostinger-deployment.md) - Full VPS setup guide
- [Hostinger Overview](./hostinger.md) - Quick reference
- [Cybrancee Deployment](./cybrancee-deployment.md) - Alternative hosting setup

## Questions?

If you encounter issues or have questions about the storage configuration:

1. Check the application logs: `sudo journalctl -u wabbitbot -f`
2. Verify directory permissions: `ls -la /var/lib/wabbitbot/`
3. Confirm configuration is loaded: Check startup logs for storage paths
4. Review the [Hostinger Deployment Guide](./hostinger-deployment.md) troubleshooting section

