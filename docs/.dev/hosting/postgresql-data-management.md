# PostgreSQL Data Management on Hostinger VPS

## Overview

PostgreSQL manages its own data storage separately from your application. You don't configure where PostgreSQL stores data in your application - PostgreSQL handles that automatically.

## PostgreSQL Data Locations on Ubuntu

### Default Data Directory
```
/var/lib/postgresql/15/main/    # PostgreSQL 15 (default on Ubuntu 22.04)
```

**This is managed by PostgreSQL itself** - you don't need to change it.

### What's Stored There

```
/var/lib/postgresql/15/main/
├── base/              # Database files (your actual data)
├── global/            # Cluster-wide tables
├── pg_wal/            # Write-Ahead Log files
├── pg_stat/           # Statistics
├── pg_tblspc/         # Tablespaces
└── postgresql.conf    # Configuration file
```

## Your Application Configuration

Your application **only needs the connection string** - it doesn't care where PostgreSQL stores files:

### Development (appsettings.json)
```json
{
  "Bot": {
    "Database": {
      "ConnectionString": "Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=wabbitbot",
      "MaxPoolSize": 10
    }
  }
}
```

### Production (appsettings.Production.json or .env)
```json
{
  "Bot": {
    "Database": {
      "ConnectionString": "Host=localhost;Database=wabbitbot;Username=wabbitbot_user;Password=your_secure_password",
      "MaxPoolSize": 10
    }
  }
}
```

**Or via environment variable:**
```bash
Bot__Database__ConnectionString=Host=localhost;Database=wabbitbot;Username=wabbitbot_user;Password=secure_pass
```

## PostgreSQL vs Application Data

| Data Type | Storage Location | Managed By | Persists on App Update |
|-----------|------------------|------------|----------------------|
| **Database Data** | `/var/lib/postgresql/` | PostgreSQL | ✅ Yes (separate service) |
| **Replay Files** | `/var/lib/wabbitbot/replays/` | Your App | ✅ Yes (configured path) |
| **Images** | `/var/lib/wabbitbot/images/` | Your App | ✅ Yes (configured path) |
| **Application Binaries** | `/opt/wabbitbot/` | Your App | ❌ No (replaced on update) |

## PostgreSQL Data Persistence

### During Application Updates
```bash
# Stop your app
sudo systemctl stop wabbitbot

# Update application binaries
# (upload new files to /opt/wabbitbot/)

# Start your app
sudo systemctl start wabbitbot

# PostgreSQL data is UNTOUCHED - it's a separate service
```

### During PostgreSQL Updates
```bash
# Update PostgreSQL (via apt)
sudo apt update
sudo apt upgrade postgresql

# Your database data persists through PostgreSQL version upgrades
# PostgreSQL handles data migration automatically
```

## Backup Strategies

### 1. Database Backups (SQL Dumps)

**Daily Backup Script** (from deployment guide):
```bash
#!/bin/bash
BACKUP_DIR="/backups/wabbitbot"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# Backup database
sudo -u postgres pg_dump wabbitbot > "$BACKUP_DIR/wabbitbot_db_$DATE.sql"

# Backup application data (replays, images)
tar -czf "$BACKUP_DIR/wabbitbot_data_$DATE.tar.gz" \
    -C /var/lib/wabbitbot replays images/discord

# Compress database backup
gzip "$BACKUP_DIR/wabbitbot_db_$DATE.sql"

# Keep only last 7 days
find $BACKUP_DIR -name "*.sql.gz" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete

echo "Backup completed: $DATE"
```

### 2. PostgreSQL Continuous Archiving (Advanced)

For production systems with high data value, configure WAL archiving:

```bash
# Edit PostgreSQL configuration
sudo nano /etc/postgresql/15/main/postgresql.conf
```

Add:
```
wal_level = replica
archive_mode = on
archive_command = 'cp %p /var/lib/postgresql/wal_archive/%f'
```

This enables point-in-time recovery.

## Checking Database Size

### Via SQL
```bash
# Connect to PostgreSQL
sudo -u postgres psql wabbitbot

# Check database size
SELECT pg_size_pretty(pg_database_size('wabbitbot'));

# Check table sizes
SELECT 
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Via Command Line
```bash
# Show all databases and sizes
sudo -u postgres psql -c "SELECT datname, pg_size_pretty(pg_database_size(datname)) FROM pg_database;"

# Show disk usage of PostgreSQL data directory
sudo du -sh /var/lib/postgresql/15/main/
```

## Monitoring Database Performance

### Connection Count
```sql
SELECT count(*) FROM pg_stat_activity;
```

### Active Queries
```sql
SELECT 
    pid,
    age(clock_timestamp(), query_start) as duration,
    usename,
    query 
FROM pg_stat_activity 
WHERE state != 'idle' AND query NOT ILIKE '%pg_stat_activity%'
ORDER BY query_start;
```

### Database Connections by Application
```sql
SELECT 
    application_name,
    count(*) 
FROM pg_stat_activity 
GROUP BY application_name;
```

## PostgreSQL Configuration Tuning

### Location
```
/etc/postgresql/15/main/postgresql.conf
```

### Recommended Settings for Hostinger KVM 2 VPS (2 CPU, 8 GB RAM, NVMe)

```
# Memory settings (optimized for 8 GB RAM)
shared_buffers = 2GB                # 25% of RAM (8GB * 0.25)
effective_cache_size = 6GB          # 75% of RAM (for query planning)
work_mem = 16MB                     # Per sort/hash operation (adjust based on concurrent queries)
maintenance_work_mem = 512MB        # For VACUUM, CREATE INDEX, etc.

# Connection settings
max_connections = 100               # Your app's MaxPoolSize (10) * safety margin
                                    # Can be lower if you only run 1 bot instance

# Write-Ahead Log (optimized for NVMe)
wal_buffers = 16MB                  # -1 for auto-tuning, or 16MB for 2GB shared_buffers
min_wal_size = 1GB
max_wal_size = 4GB
checkpoint_completion_target = 0.9  # Spread out checkpoint writes

# Query planning (optimized for NVMe SSD)
random_page_cost = 1.1              # NVMe has very low random access cost
effective_io_concurrency = 200      # Number of concurrent disk I/O operations (NVMe)
max_worker_processes = 2            # Match CPU cores
max_parallel_workers_per_gather = 2 # Parallel query workers
max_parallel_workers = 2            # Total parallel workers

# Autovacuum tuning
autovacuum_max_workers = 2          # Match CPU cores
autovacuum_naptime = 30s            # More frequent for active Discord bot

# Logging (optional, for debugging)
# Uncomment these if you need to debug slow queries
# log_min_duration_statement = 1000  # Log queries slower than 1 second
# log_line_prefix = '%t [%p]: '      # Timestamp and process ID
```

### Apply Changes

```bash
# Edit configuration
sudo nano /etc/postgresql/15/main/postgresql.conf

# Test configuration syntax
sudo -u postgres /usr/lib/postgresql/15/bin/postgres -D /var/lib/postgresql/15/main --check

# Restart PostgreSQL to apply
sudo systemctl restart postgresql

# Verify it started successfully
sudo systemctl status postgresql
```

### Performance Tuning Notes

**Why these settings for your VPS:**
- **2 GB shared_buffers**: With 8 GB RAM, PostgreSQL can cache frequently accessed data
- **16 MB work_mem**: Enough for sorting operations without excessive memory usage (100 connections × 16 MB = 1.6 GB max)
- **512 MB maintenance_work_mem**: Makes VACUUM and index creation much faster
- **NVMe optimization**: Low `random_page_cost` because NVMe has excellent random I/O
- **Parallel queries**: With 2 CPU cores, parallel query execution can help with large scans

**Memory allocation breakdown:**
```
shared_buffers:           2 GB   (PostgreSQL cache)
OS file cache:            4 GB   (Linux caches files here)
Application + overhead:   2 GB   (Your bot + system)
--------------------------------
Total:                    8 GB
```

## Security Best Practices

### 1. Secure PostgreSQL Access
```bash
# Edit pg_hba.conf
sudo nano /etc/postgresql/15/main/pg_hba.conf
```

Ensure only local connections for your app:
```
# TYPE  DATABASE        USER            ADDRESS                 METHOD
local   all             postgres                                peer
local   wabbitbot       wabbitbot_user                          scram-sha-256
host    wabbitbot       wabbitbot_user  127.0.0.1/32            scram-sha-256
```

### 2. Use Strong Passwords
```sql
-- Change password for database user
ALTER USER wabbitbot_user WITH PASSWORD 'very_secure_random_password';
```

### 3. Regular Updates
```bash
sudo apt update
sudo apt upgrade postgresql
```

## Troubleshooting

### Problem: Database connection refused

**Check if PostgreSQL is running:**
```bash
sudo systemctl status postgresql
```

**Check logs:**
```bash
sudo journalctl -u postgresql -n 50
```

**Verify connection settings:**
```bash
# Try connecting manually
psql -h localhost -U wabbitbot_user -d wabbitbot
```

### Problem: Database is slow

**Check active connections:**
```sql
SELECT count(*) FROM pg_stat_activity;
```

**Check for long-running queries:**
```sql
SELECT pid, age(clock_timestamp(), query_start), query 
FROM pg_stat_activity 
WHERE state != 'idle' 
ORDER BY query_start;
```

**Analyze table statistics:**
```sql
ANALYZE VERBOSE;
```

### Problem: Out of disk space

**Check database size:**
```bash
sudo du -sh /var/lib/postgresql/15/main/
```

**Clean up old WAL files:**
```sql
-- Connect as postgres superuser
sudo -u postgres psql

-- Run checkpoint to flush data
CHECKPOINT;
```

**Vacuum old data:**
```sql
VACUUM VERBOSE;
```

## Data Recovery

### Restore from SQL Dump
```bash
# Stop your application
sudo systemctl stop wabbitbot

# Drop and recreate database (DESTRUCTIVE!)
sudo -u postgres psql -c "DROP DATABASE wabbitbot;"
sudo -u postgres psql -c "CREATE DATABASE wabbitbot OWNER wabbitbot_user;"

# Restore from backup
gunzip < /backups/wabbitbot/wabbitbot_db_20250119_020000.sql.gz | sudo -u postgres psql wabbitbot

# Start your application
sudo systemctl start wabbitbot
```

### Restore Specific Table
```bash
# Extract just one table
pg_restore -t match_results /backups/wabbitbot/backup.sql
```

## Quick Reference Commands

```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# View PostgreSQL logs
sudo journalctl -u postgresql -f

# Connect to database
sudo -u postgres psql wabbitbot

# Manual backup
sudo -u postgres pg_dump wabbitbot > backup.sql

# Check database size
sudo -u postgres psql -c "SELECT pg_size_pretty(pg_database_size('wabbitbot'));"

# List all databases
sudo -u postgres psql -c "\l"

# List all tables in wabbitbot database
sudo -u postgres psql wabbitbot -c "\dt"

# Show PostgreSQL version
sudo -u postgres psql -c "SELECT version();"
```

## Hardware Specifications

This guide is optimized for **Hostinger KVM 2 VPS**:
- **CPU:** 2 cores
- **RAM:** 8 GB
- **Storage:** 100 GB NVMe SSD
- **OS:** Ubuntu 22.04

Settings may need adjustment for different hardware configurations.

## Performance Expectations

With the recommended PostgreSQL tuning on your VPS, you can expect:

| Metric                   | Expected Performance                          |
|--------------------------|-----------------------------------------------|
| Concurrent connections   | 50-100 easily supported                       |
| Query response time      | <10ms for simple queries, <100ms for complex  |
| Database size capacity   | Up to ~80 GB (leaving 20 GB for OS/app/logs)  |
| Backup time              | ~30 seconds per GB (NVMe is fast)             |
| Match history queries    | <50ms for recent data, <200ms for large scans |
| Leaderboard calculations | <100ms for active season data                 |

## Related Documentation

- [Hostinger Deployment Guide](./hostinger-deployment.md) - Full VPS setup
- [Storage Configuration](./storage-configuration-migration.md) - Application file storage
- [Official PostgreSQL Documentation](https://www.postgresql.org/docs/)
- [PostgreSQL Performance Tuning](https://wiki.postgresql.org/wiki/Performance_Optimization)

