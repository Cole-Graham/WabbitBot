# Deploying WabbitBot to Hostinger VPS (Ubuntu)

## Prerequisites

- Hostinger VPS plan with Ubuntu (22.04 or later recommended)
- SSH access to your VPS
- PostgreSQL database (installed on VPS or external service)

## Overview

Hostinger VPS gives you full root access, allowing complete control over your deployment. This guide covers:
1. Server setup and dependencies
2. PostgreSQL installation and configuration
3. Application deployment
4. systemd service configuration for auto-restart
5. Configuration management

---

## 1. Initial Server Setup

### Connect to Your VPS

```bash
ssh root@your-vps-ip
```

### Update System Packages

```bash
sudo apt update
sudo apt upgrade -y
```

### Install .NET Runtime

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET 9 Runtime
sudo apt update
sudo apt install -y dotnet-runtime-9.0
```

Verify installation:
```bash
dotnet --info
```

---

## 2. PostgreSQL Setup

### Install PostgreSQL

```bash
sudo apt install postgresql postgresql-contrib -y
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

### Create Database and User

```bash
# Switch to postgres user
sudo -u postgres psql

# In PostgreSQL prompt:
CREATE DATABASE wabbitbot;
CREATE USER wabbitbot_user WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE wabbitbot TO wabbitbot_user;
\q
```

> **Note:** PostgreSQL stores its data in `/var/lib/postgresql/` which is completely separate from your application. See [PostgreSQL Data Management](./postgresql-data-management.md) for details on backups, monitoring, and tuning.

### Configure PostgreSQL for Local Access

Edit PostgreSQL configuration if needed:
```bash
sudo nano /etc/postgresql/*/main/pg_hba.conf
```

Ensure local connections are allowed:
```
# IPv4 local connections:
host    all             all             127.0.0.1/32            scram-sha-256
```

### Tune PostgreSQL for Your VPS (Optional but Recommended)

Your Hostinger KVM 2 VPS has excellent specs (2 CPU, 8 GB RAM, NVMe). Apply these optimizations:

```bash
# Edit PostgreSQL configuration
sudo nano /etc/postgresql/*/main/postgresql.conf
```

Add or modify these settings:
```
# Memory settings (optimized for 8 GB RAM)
shared_buffers = 2GB
effective_cache_size = 6GB
work_mem = 16MB
maintenance_work_mem = 512MB

# WAL settings (optimized for NVMe)
wal_buffers = 16MB
min_wal_size = 1GB
max_wal_size = 4GB

# Query planning (NVMe SSD)
random_page_cost = 1.1
effective_io_concurrency = 200
max_worker_processes = 2
max_parallel_workers_per_gather = 2
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

> **See full tuning guide:** [PostgreSQL Data Management](./postgresql-data-management.md) for complete settings and explanation.

---

## 3. Application Deployment

### Create Application Directory

```bash
sudo mkdir -p /opt/wabbitbot
sudo chown $USER:$USER /opt/wabbitbot
```

### Build and Transfer Application

**On your local machine:**

```powershell
# Build release version
cd C:\Users\coleg\Projects\WabbitBot
dotnet publish src/WabbitBot.Host/WabbitBot.Host.csproj -c Release -o out

# Transfer to VPS (using SCP or SFTP)
scp -r out/* root@your-vps-ip:/opt/wabbitbot/
```

**Alternative: Using SFTP client (WinSCP, FileZilla, etc.)**
- Connect to your VPS via SFTP
- Upload contents of `out/` folder to `/opt/wabbitbot/`

### Set Permissions

```bash
sudo chmod +x /opt/wabbitbot/WabbitBot.Host
sudo chown -R www-data:www-data /opt/wabbitbot
```

---

## 4. File Storage Configuration

Following Linux Filesystem Hierarchy Standard (FHS), WabbitBot stores persistent data separately from application binaries. This ensures data persists across application updates.

### Recommended Directory Structure

```
/opt/wabbitbot/           # Application binaries (replaced on update)
/var/lib/wabbitbot/       # Persistent application data
├── replays/              # Replay files (.rpl3, .zip)
├── images/
│   ├── maps/discord/     # Map thumbnails
│   ├── discord/          # Custom Discord images (user-uploaded)
│   └── default/discord/  # Default Discord images (shipped with app)
└── divisions/icons/      # Division icons
```

### Create Data Directories

```bash
# Create persistent data directory
sudo mkdir -p /var/lib/wabbitbot/{replays,images/{maps/discord,discord,default/discord},divisions/icons}

# Set ownership
sudo chown -R www-data:www-data /var/lib/wabbitbot

# Set permissions (data directory writable, replay directory writable)
sudo chmod 755 /var/lib/wabbitbot
sudo chmod 775 /var/lib/wabbitbot/replays
sudo chmod 775 /var/lib/wabbitbot/images/discord
sudo chmod 755 /var/lib/wabbitbot/images/default
```

### Copy Default Images

Default images ship with the application. Copy them to the persistent location:

```bash
# Copy default images from application to persistent storage
sudo cp -r /opt/wabbitbot/data/images/default/discord/* /var/lib/wabbitbot/images/default/discord/
sudo chown -R www-data:www-data /var/lib/wabbitbot/images/default
```

### Storage Configuration in appsettings

The storage paths are configured in `appsettings.json` or environment-specific config files.

**For Development** (use relative paths in `appsettings.json`):
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

**For Production on VPS** (create `appsettings.Production.json`):
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

Create the production config:
```bash
sudo nano /opt/wabbitbot/appsettings.Production.json
```

Paste the production configuration above, then set permissions:
```bash
sudo chmod 644 /opt/wabbitbot/appsettings.Production.json
sudo chown www-data:www-data /opt/wabbitbot/appsettings.Production.json
```

---

## 5. Configuration Management

You have two options for managing secrets on Ubuntu VPS:

### Option A: .env File (Simpler Migration)

Create a `.env` file in the application directory:

```bash
sudo nano /opt/wabbitbot/.env
```

Add your configuration:
```
ASPNETCORE_ENVIRONMENT=Production
Bot__Token=your_discord_bot_token_here
Bot__Database__ConnectionString=Host=localhost;Database=wabbitbot;Username=wabbitbot_user;Password=your_secure_password;
```

Set secure permissions:
```bash
sudo chmod 600 /opt/wabbitbot/.env
sudo chown www-data:www-data /opt/wabbitbot/.env
```

### Option B: systemd Environment File (More Linux-Native)

Create a systemd environment file:

```bash
sudo nano /etc/wabbitbot/environment
```

Add your configuration:
```
ASPNETCORE_ENVIRONMENT=Production
Bot__Token=your_discord_bot_token_here
Bot__Database__ConnectionString=Host=localhost;Database=wabbitbot;Username=wabbitbot_user;Password=your_secure_password;
```

Set secure permissions:
```bash
sudo chmod 600 /etc/wabbitbot/environment
sudo chown root:root /etc/wabbitbot/environment
```

---

## 6. systemd Service Configuration

Create a systemd service file for automatic startup and restart:

```bash
sudo nano /etc/systemd/system/wabbitbot.service
```

**If using .env file (Option A):**
```ini
[Unit]
Description=WabbitBot Discord Bot
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/opt/wabbitbot
ExecStart=/usr/bin/dotnet /opt/wabbitbot/WabbitBot.Host.dll
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wabbitbot

# Timeout settings
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```

**If using systemd environment file (Option B):**
```ini
[Unit]
Description=WabbitBot Discord Bot
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=simple
User=www-data
Group=www-data
WorkingDirectory=/opt/wabbitbot
EnvironmentFile=/etc/wabbitbot/environment
ExecStart=/usr/bin/dotnet /opt/wabbitbot/WabbitBot.Host.dll
Restart=always
RestartSec=10
StandardOutput=journal
StandardError=journal
SyslogIdentifier=wabbitbot

# Timeout settings
TimeoutStopSec=30

[Install]
WantedBy=multi-user.target
```

### Enable and Start Service

```bash
# Reload systemd to recognize new service
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable wabbitbot

# Start the service
sudo systemctl start wabbitbot

# Check status
sudo systemctl status wabbitbot
```

---

## 7. Monitoring and Logs

### View Real-Time Logs

```bash
# View all logs
sudo journalctl -u wabbitbot -f

# View last 100 lines
sudo journalctl -u wabbitbot -n 100

# View logs since boot
sudo journalctl -u wabbitbot -b
```

### Check Service Status

```bash
sudo systemctl status wabbitbot
```

### Restart Service

```bash
sudo systemctl restart wabbitbot
```

### Stop Service

```bash
sudo systemctl stop wabbitbot
```

---

## 8. Updating the Application

When you need to deploy updates, **your data files are preserved** because they're stored separately in `/var/lib/wabbitbot/`.

### Update Process

```bash
# Stop the service
sudo systemctl stop wabbitbot

# Backup current version (optional but recommended)
sudo cp -r /opt/wabbitbot /opt/wabbitbot.backup.$(date +%Y%m%d)

# Transfer new build files (on local machine)
# This will overwrite application binaries but NOT your data files
# scp -r out/* root@your-vps-ip:/opt/wabbitbot/

# Set permissions on application files
sudo chown -R www-data:www-data /opt/wabbitbot
sudo chmod +x /opt/wabbitbot/WabbitBot.Host

# Ensure data directories still have correct permissions
sudo chown -R www-data:www-data /var/lib/wabbitbot
sudo chmod 775 /var/lib/wabbitbot/replays

# Start the service
sudo systemctl start wabbitbot

# Check logs for successful startup
sudo journalctl -u wabbitbot -f
```

### What Gets Updated vs What Persists

| Location | Type | On Update |
|----------|------|-----------|
| `/opt/wabbitbot/` | Application binaries | **Replaced** |
| `/opt/wabbitbot/appsettings.json` | Base config | **Replaced** (may need to merge changes) |
| `/opt/wabbitbot/appsettings.Production.json` | Production config | **Preserved** (unless you replace it) |
| `/var/lib/wabbitbot/replays/` | Replay files | **Preserved** ✓ |
| `/var/lib/wabbitbot/images/discord/` | Custom images | **Preserved** ✓ |
| `/etc/wabbitbot/environment` | Secrets | **Preserved** ✓ |
| Database | All game data | **Preserved** ✓ |

**Important**: After updating, check if `appsettings.json` has new configuration sections that need to be added to your `appsettings.Production.json`.

---

## 9. Security Best Practices

### Firewall Configuration

```bash
# Install UFW if not already installed
sudo apt install ufw -y

# Allow SSH
sudo ufw allow 22/tcp

# Enable firewall
sudo ufw enable
```

### Regular Updates

Set up automatic security updates:

```bash
sudo apt install unattended-upgrades -y
sudo dpkg-reconfigure --priority=low unattended-upgrades
```

### Secure SSH

Edit SSH config:
```bash
sudo nano /etc/ssh/sshd_config
```

Recommended settings:
```
PermitRootLogin no
PasswordAuthentication no
PubkeyAuthentication yes
```

Restart SSH:
```bash
sudo systemctl restart sshd
```

---

## 10. Troubleshooting

### Service Won't Start

```bash
# Check detailed status
sudo systemctl status wabbitbot -l

# Check logs for errors
sudo journalctl -u wabbitbot -n 50

# Verify .NET is working
dotnet --info

# Test running manually
cd /opt/wabbitbot
sudo -u www-data dotnet WabbitBot.Host.dll
```

### Database Connection Issues

```bash
# Test PostgreSQL connection
psql -h localhost -U wabbitbot_user -d wabbitbot

# Check PostgreSQL logs
sudo journalctl -u postgresql -n 50

# Verify PostgreSQL is running
sudo systemctl status postgresql
```

### Permission Issues

```bash
# Reset permissions
sudo chown -R www-data:www-data /opt/wabbitbot
sudo chmod +x /opt/wabbitbot/WabbitBot.Host
sudo chmod 600 /opt/wabbitbot/.env  # if using .env
```

---

## 11. Backup Strategy

### Database Backups

Create a backup script:

```bash
sudo nano /usr/local/bin/backup-wabbitbot.sh
```

```bash
#!/bin/bash
BACKUP_DIR="/backups/wabbitbot"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# Backup database
sudo -u postgres pg_dump wabbitbot > "$BACKUP_DIR/wabbitbot_db_$DATE.sql"

# Backup replay files and custom images
tar -czf "$BACKUP_DIR/wabbitbot_data_$DATE.tar.gz" \
    -C /var/lib/wabbitbot replays images/discord

# Compress database backup
gzip "$BACKUP_DIR/wabbitbot_db_$DATE.sql"

# Keep only last 7 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $DATE"
```

Make executable:
```bash
sudo chmod +x /usr/local/bin/backup-wabbitbot.sh
```

Add to cron (daily at 2 AM):
```bash
sudo crontab -e
```

Add line:
```
0 2 * * * /usr/local/bin/backup-wabbitbot.sh >> /var/log/wabbitbot-backup.log 2>&1
```

---

## Comparison: Hostinger VPS vs Cybrancee

| Feature | Cybrancee | Hostinger VPS |
|---------|-----------|---------------|
| Setup Complexity | Low (managed) | Medium (manual) |
| Control | Limited | Full root access |
| PostgreSQL | Provided | Manual install |
| Auto-restart | Built-in | systemd service |
| Cost | ~$10-15/month | ~$3.99-8/month |
| Scalability | Fixed plans | Flexible resources |
| Configuration | .env via panel | .env or systemd |
| Monitoring | Panel UI | systemd + journalctl |
| Backups | Panel feature | Manual setup |

---

## Quick Reference Commands

```bash
# View logs
sudo journalctl -u wabbitbot -f

# Restart service
sudo systemctl restart wabbitbot

# Check status
sudo systemctl status wabbitbot

# Edit configuration
sudo nano /opt/wabbitbot/.env  # or /etc/wabbitbot/environment
sudo nano /opt/wabbitbot/appsettings.Production.json

# Update application (preserves data)
sudo systemctl stop wabbitbot
# ... upload new files to /opt/wabbitbot/ ...
sudo chown -R www-data:www-data /opt/wabbitbot
sudo systemctl start wabbitbot

# Database backup
sudo -u postgres pg_dump wabbitbot > backup.sql

# Backup replay files
sudo tar -czf replays_backup.tar.gz -C /var/lib/wabbitbot replays

# Check data directory size
du -sh /var/lib/wabbitbot/*

# List recent replay files
ls -lht /var/lib/wabbitbot/replays/ | head -10
```

