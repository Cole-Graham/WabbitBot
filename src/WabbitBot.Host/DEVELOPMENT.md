# WabbitBot - Local Development Guide

This guide will help you set up and run WabbitBot locally for development and testing.

## Prerequisites

Before you begin, ensure you have the following installed:

1. **[.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** or later
2. **[PostgreSQL 16](https://www.postgresql.org/download/)** or later
3. **[Git](https://git-scm.com/downloads)** (for cloning the repository)
4. **A Discord Bot Token** (see [Discord Bot Setup](#discord-bot-setup) below)
5. **IDE** (recommended: Visual Studio 2022, VS Code with C# Dev Kit, or Rider)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/yourusername/WabbitBot.git
cd WabbitBot
```

### 2. Set Up PostgreSQL Database

#### Option A: Using PostgreSQL Installed Locally

1. Start PostgreSQL service
2. Create a database and user:

```sql
-- Connect to PostgreSQL as superuser (postgres)
CREATE DATABASE wabbitbot;
CREATE USER wabbitbot WITH PASSWORD 'wabbitbot';
GRANT ALL PRIVILEGES ON DATABASE wabbitbot TO wabbitbot;

-- Connect to the wabbitbot database
\c wabbitbot

-- Grant schema privileges (PostgreSQL 15+)
GRANT ALL ON SCHEMA public TO wabbitbot;
```

#### Option B: Using Docker

**Unix/Linux/Mac (bash):**
```bash
docker run --name wabbitbot-postgres \
  -e POSTGRES_DB=wabbitbot \
  -e POSTGRES_USER=wabbitbot \
  -e POSTGRES_PASSWORD=wabbitbot \
  -p 5432:5432 \
  -d postgres:16-alpine
```

**Windows (PowerShell):**
```powershell
docker run --name wabbitbot-postgres -e POSTGRES_DB=wabbitbot -e POSTGRES_USER=wabbitbot -e POSTGRES_PASSWORD=wabbitbot -p 5432:5432 -d postgres:16-alpine
```

### 3. Configure the Bot

#### Get a Discord Bot Token

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Click "New Application" and give it a name
3. Go to the "Bot" section
4. Click "Reset Token" to get your bot token (save this - you'll need it!)
5. Enable these **Privileged Gateway Intents**:
   - Server Members Intent
   - Message Content Intent
6. Go to "OAuth2 > URL Generator"
7. Select scopes: `bot`, `applications.commands`
8. Select bot permissions: `Administrator` (for testing) or specific permissions
9. Copy the generated URL and invite the bot to your test server

#### Configure Application Settings

The bot uses multiple configuration sources in this priority order:
1. **User Secrets** (recommended for local development)
2. **Environment Variables**
3. **appsettings.Development.json** (optional, gitignored)
4. **appsettings.json** (base configuration)

**Recommended: Use User Secrets for the Discord Token**

```bash
cd src/WabbitBot.Host
dotnet user-secrets init
dotnet user-secrets set "Bot:Token" "YOUR_DISCORD_BOT_TOKEN_HERE"
```

**Alternative: Create appsettings.Development.json**

Copy the template and edit:

```bash
cd src/WabbitBot.Host
cp appsettings.Development.json.template appsettings.Development.json
```

Edit `appsettings.Development.json`:

```json
{
  "Bot": {
    "Token": "YOUR_DISCORD_BOT_TOKEN_HERE",
    "LogLevel": "Debug",
    "ServerId": 1234567890123456789,
    "Activity": {
      "Type": "Playing",
      "Name": "WARNO Actual (DEV)"
    }
  }
}
```

**Alternative: Use .env File**

Create a `.env` file in the project root:

```bash
# .env
WABBITBOT_TOKEN=YOUR_DISCORD_BOT_TOKEN_HERE
ASPNETCORE_ENVIRONMENT=Development
```

#### Verify Database Configuration

The default database configuration in `appsettings.json` is:

```json
"Database": {
  "Provider": "PostgreSQL",
  "ConnectionString": "Host=localhost;Database=wabbitbot;Username=wabbitbot;Password=wabbitbot",
  "MaxPoolSize": 10
}
```

If your PostgreSQL setup is different, override it in `appsettings.Development.json` or user secrets:

```bash
dotnet user-secrets set "Bot:Database:ConnectionString" "Host=localhost;Database=wabbitbot;Username=myuser;Password=mypass"
```

#### Configure Development Guild for Instant Command Updates

By default, Discord slash commands are registered **globally**, which can take up to **1 hour** to update after changes. During development, you can register commands to a specific server (guild) for **instant updates**.

**How to Enable:**

1. **Find your development server's Guild ID**:
   - In Discord, enable Developer Mode (Settings → Advanced → Developer Mode)
   - Right-click your server icon → Copy Server ID

2. **Set the Debug Guild ID** in `appsettings.Development.json`:

```json
{
  "Bot": {
    "DebugGuildId": "1348467819528065074",
    "LogLevel": "Debug",
    "Activity": {
      "Type": "Playing",
      "Name": "WARNO Actual (DEV)"
    }
  }
}
```

Or via user secrets:

```bash
dotnet user-secrets set "Bot:DebugGuildId" "1348467819528065074"
```

Or via environment variable:

```bash
# In .env file
WABBITBOT_DEBUG_GUILD_ID=1348467819528065074
```

**What This Does:**
- ✅ Commands update **instantly** in your development server
- ✅ No waiting for Discord's global command cache
- ✅ Automatically switches to global registration in production (when `DebugGuildId` is not set)

**Note:** Commands registered to a guild will **only** appear in that specific server. Remove the `DebugGuildId` setting for production deployments to make commands available globally.

### 4. Build the Project

```bash
# From the project root
dotnet build
```

### 5. Run Database Migrations

The bot automatically runs migrations on startup, but you can run them manually:

```bash
# From the project root
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

### 6. Run the Bot

```bash
# From the project root
dotnet run --project src/WabbitBot.Host
```

Or from Visual Studio:
- Set `WabbitBot.Host` as the startup project
- Press F5 or click "Start Debugging"

### 7. Verify It's Working

You should see output like:

```
🌍 Current environment: Development
🌱 Seeding development data...
✅ Created team 'AlphaTeam' with 2 players
✅ Created team 'BravoTeam' with 2 players
✅ Development data seeded successfully!
[DSharpPlus] Connected to Discord
[Bot] Ready! Logged in as YourBot#1234
```

In your Discord server, the bot should:
- Show as online
- Respond to slash commands (type `/` to see available commands)

## Development Data

In Development mode, the bot automatically seeds test data on first run:

- **AlphaTeam**: 2 players (Discord IDs: 1348719242882584689, 1348724033306366055)
- **BravoTeam**: 2 players (Discord IDs: 1348724778906681447, 1348725467422916749)

This data is **idempotent** - it won't create duplicates if you restart the bot.

See [database-seed-data.md](./docs/.dev/database-seed-data.md) for details on customizing seed data.

## Project Structure

```
WabbitBot/
├── src/
│   ├── WabbitBot.Host/              # Entry point, configuration, startup
│   ├── WabbitBot.Core/              # Business logic (vertical slices)
│   ├── WabbitBot.DiscBot/           # Discord layer (DSharpPlus integration)
│   ├── WabbitBot.Common/            # Shared contracts and infrastructure
│   └── WabbitBot.SourceGenerators/  # Compile-time code generation
├── docs/                            # Documentation
└── data/                            # Runtime data (images, replays)
```

## Common Tasks

### Reset the Database

```bash
# Drop and recreate database
dotnet ef database drop --project src/WabbitBot.Core --startup-project src/WabbitBot.Host --force
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

Seed data will be recreated automatically on next run.

### Create a New Migration

```bash
dotnet ef migrations add MigrationName --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

### View Logs

Logs are written to:
- Console (stdout)
- Application Insights (if configured)

Set log level in `appsettings.Development.json`:

```json
{
  "Bot": {
    "LogLevel": "Debug"  // Options: Trace, Debug, Information, Warning, Error, Critical
  }
}
```

### Hot Reload

The bot doesn't support hot reload for code changes. You'll need to stop and restart it.

Configuration changes in `appsettings.json` are reloaded automatically (marked with `reloadOnChange: true`).

## Troubleshooting

### Bot won't start - "Missing Bot:Token"

**Solution**: Make sure you've set the Discord bot token in user secrets, environment variable, or `appsettings.Development.json`.

```bash
dotnet user-secrets set "Bot:Token" "YOUR_TOKEN_HERE" --project src/WabbitBot.Host
```

### Database connection error

**Solution**: Verify PostgreSQL is running and credentials are correct:

```bash
# Test connection
psql -h localhost -U wabbitbot -d wabbitbot

# Check PostgreSQL is running
# Windows: Check Services
# Linux: systemctl status postgresql
# Docker: docker ps
```

### Bot shows offline in Discord

**Possible causes**:
1. Invalid bot token - regenerate token in Discord Developer Portal
2. Bot not invited to server with correct permissions
3. Network/firewall blocking Discord gateway connection

### Migrations fail

**Solution**: Delete and recreate the database:

```bash
dotnet ef database drop --project src/WabbitBot.Core --startup-project src/WabbitBot.Host --force
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

### "DSharpPlus.Commands not found"

**Solution**: Restore NuGet packages:

```bash
dotnet restore
dotnet build
```

## Environment Variables Reference

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Development` |
| `WABBITBOT_TOKEN` | Discord bot token | (required) |
| `WABBITBOT_CONNECTION_STRING` | PostgreSQL connection string | See appsettings.json |

## Testing Commands

Once the bot is running, try these commands in Discord:

- `/ping` - Test if bot responds
- `/help` - View available commands
- `/team create` - Create a new team
- `/scrimmage` - Start a scrimmage queue

## Additional Documentation

- [Architecture Overview](./docs/.dev/architecture/event-system-architecture.md)
- [Database Migrations](./docs/.dev/architecture/database/database-migration-strategy.md)
- [Seed Data Guide](./docs/.dev/database-seed-data.md)
- [Deployment Guide](./docs/.dev/hosting/hostinger-deployment.md)

## Getting Help

- Check [Issues](https://github.com/yourusername/WabbitBot/issues) for known problems
- Review existing documentation in `docs/.dev/`
- Ask in the development Discord channel

## Contributing

See [AGENTS.md](./AGENTS.md) for architecture guidelines and coding standards.

Key principles:
- No dependency injection (uses static service locator pattern)
- Vertical slice architecture
- Event-driven communication via event buses
- PostgreSQL for persistence
- DSharpPlus 5.0 for Discord integration

