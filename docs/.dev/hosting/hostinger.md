# Hostinger VPS Setup for WabbitBot

> **Full deployment guide available:** See [hostinger-deployment.md](./hostinger-deployment.md) for complete setup instructions.

## PostgreSQL Support Overview

Yes, Hostinger supports PostgreSQL, but **only on their VPS hosting plans**—not on shared web hosting, cloud hosting, or other lower-tier options. This is due to PostgreSQL's higher resource demands (e.g., memory and specific configurations) that aren't feasible in their managed shared environments. On VPS, you get full root access, allowing you to install and manage PostgreSQL manually via tools like APT on Ubuntu-based servers.

#### Key Details by Plan Type
| Plan Type              | PostgreSQL Support?      | Details                                                                                         |
|------------------------|--------------------------|-------------------------------------------------------------------------------------------------|
| **Shared Web Hosting** | No                       | Uses MariaDB (MySQL-compatible) only. No custom DBs like PostgreSQL allowed.                    |
| **Cloud Hosting**      | No                       | Similar limitations to shared; optimized for MySQL/MariaDB.                                     |
| **VPS Hosting**        | Yes                      | Full root access for installation (e.g., on Ubuntu 22.04). Supports latest versions (e.g., 15+).|
| **Dedicated/Other**    | Varies (via VPS upgrade) | Can migrate from shared to VPS for PostgreSQL.                                                  |

#### How to Set It Up on VPS
1. **Purchase a VPS Plan**: Starts at ~$3.99/month (1 vCPU, 1 GB RAM—sufficient for a lightweight bot + Postgres). Choose Ubuntu as the OS.
2. **Access Your Server**: Log in via SSH using credentials from hPanel (Hostinger's control panel).
3. **Install PostgreSQL**: Run these commands (adapted from Hostinger's tutorial):
   ```
   sudo apt update
   sudo apt install postgresql postgresql-contrib -y
   sudo systemctl start postgresql
   sudo systemctl enable postgresql
   ```
   - Create a database/user: `sudo -u postgres createdb yourbotdb` and `sudo -u postgres createuser -P yourbotuser`.
   - Secure it: Edit `/etc/postgresql/*/main/pg_hba.conf` for local access.
4. **Integrate with Your Bot**: Update your C# connection string (e.g., in `appsettings.json`) to `Host=localhost;Database=yourbotdb;Username=yourbotuser;Password=yourpass`.
5. **Deploy Bot**: Use systemd for persistence, similar to DigitalOcean setups. Hostinger's KVM-based VPS supports .NET Core easily.

#### Pros/Cons for Your Use Case
- **Pros**: Affordable VPS scaling, 99.9% uptime, free weekly backups, and easy migration from shared plans. Good for bots needing local Postgres.
- **Cons**: Manual setup (no one-click installer like some competitors). If you prefer managed Postgres, alternatives like DigitalOcean's RDS (~$15/month extra) or Aiven might be simpler.

For the latest pricing or plan details, check Hostinger's VPS page. If your bot grows, VPS lets you scale RAM/CPU without DB changes.

## Quick Start

**SSH Access:**
```bash
ssh root@148.230.87.82
```

**Next Steps:**
1. Follow the complete deployment guide in [hostinger-deployment.md](./hostinger-deployment.md)
2. Install .NET 9 runtime
3. Install and configure PostgreSQL
4. Deploy application files
5. Set up systemd service for auto-restart
6. Configure environment variables or .env file

## Key Differences from Cybrancee

| Aspect        | Cybrancee           | Hostinger VPS               |
|---------------|---------------------|-----------------------------|
| Management    | Fully managed       | Self-managed                |
| PostgreSQL    | Included            | Manual install              |
| Auto-restart  | Built-in            | systemd setup required      |
| Configuration | Panel + .env        | .env or systemd environment |
| Cost          | Higher (~$10-15/mo) | Lower (~$3.99-8/mo)         |
| Control       | Limited             | Full root access            |