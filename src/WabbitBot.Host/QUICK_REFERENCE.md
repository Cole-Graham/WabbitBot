# WabbitBot - Quick Reference

Quick reference for common development tasks. For detailed instructions, see [DEVELOPMENT.md](./DEVELOPMENT.md).

## First-Time Setup

```bash
# 1. Clone repository
git clone https://github.com/yourusername/WabbitBot.git
cd WabbitBot

# 2. Create PostgreSQL database (if not using Docker)
psql -U postgres
CREATE DATABASE wabbitbot;
CREATE USER wabbitbot WITH PASSWORD 'wabbitbot';
GRANT ALL PRIVILEGES ON DATABASE wabbitbot TO wabbitbot;
\c wabbitbot
GRANT ALL ON SCHEMA public TO wabbitbot;
\q

# 3. Configure bot token
cd src/WabbitBot.Host
dotnet user-secrets set "Bot:Token" "YOUR_DISCORD_BOT_TOKEN"
cd ../..

# 4. Build and run
dotnet build
dotnet run --project src/WabbitBot.Host
```

## Docker PostgreSQL Setup

**Unix/Linux/Mac (bash):**
```bash
# Start PostgreSQL container
docker run --name wabbitbot-postgres \
  -e POSTGRES_DB=wabbitbot \
  -e POSTGRES_USER=wabbitbot \
  -e POSTGRES_PASSWORD=wabbitbot \
  -p 5432:5432 \
  -d postgres:16-alpine
```

**Windows (PowerShell):**
```powershell
# Start PostgreSQL container (single line)
docker run --name wabbitbot-postgres -e POSTGRES_DB=wabbitbot -e POSTGRES_USER=wabbitbot -e POSTGRES_PASSWORD=wabbitbot -p 5432:5432 -d postgres:16-alpine
```

**All platforms:**
```bash
# Stop container
docker stop wabbitbot-postgres

# Start existing container
docker start wabbitbot-postgres

# Remove container
docker rm -f wabbitbot-postgres
```

## Running the Bot

```bash
# Run from project root
dotnet run --project src/WabbitBot.Host

# Run with specific configuration
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/WabbitBot.Host

# Run in release mode
dotnet run --project src/WabbitBot.Host --configuration Release
```

## Configuration

```bash
# Set Discord bot token (recommended)
cd src/WabbitBot.Host
dotnet user-secrets set "Bot:Token" "YOUR_TOKEN"

# Set database connection string
dotnet user-secrets set "Bot:Database:ConnectionString" "Host=localhost;Database=wabbitbot;Username=user;Password=pass"

# Set debug guild ID for instant command updates (development only)
dotnet user-secrets set "Bot:DebugGuildId" "1348467819528065074"

# Set log level
dotnet user-secrets set "Bot:LogLevel" "Debug"

# List all secrets
dotnet user-secrets list

# Clear all secrets
dotnet user-secrets clear
```

## Database Operations

```bash
# Run migrations (automatic on startup, or manually)
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host

# Create new migration
dotnet ef migrations add YourMigrationName --project src/WabbitBot.Core --startup-project src/WabbitBot.Host

# Drop database (WARNING: Deletes all data)
dotnet ef database drop --project src/WabbitBot.Core --startup-project src/WabbitBot.Host --force

# List migrations
dotnet ef migrations list --project src/WabbitBot.Core --startup-project src/WabbitBot.Host

# Generate SQL script for migration
dotnet ef migrations script --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

## Building

```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/WabbitBot.Core/WabbitBot.Core.csproj

# Clean build artifacts
dotnet clean

# Build in Release mode
dotnet build --configuration Release

# Restore NuGet packages
dotnet restore
```

## Testing

```bash
# Run all tests
dotnet test

# Run tests for specific project
dotnet test src/WabbitBot.Core.Tests/WabbitBot.Core.Tests.csproj

# Run tests with detailed output
dotnet test --verbosity detailed

# Run specific test
dotnet test --filter "FullyQualifiedName~YourTestName"
```

## Project Management

```bash
# Add new project reference
dotnet add src/ProjectA/ProjectA.csproj reference src/ProjectB/ProjectB.csproj

# Add NuGet package
dotnet add src/WabbitBot.Core/WabbitBot.Core.csproj package PackageName

# Update all NuGet packages
dotnet restore --force-evaluate

# List outdated packages
dotnet list package --outdated
```

## Development Data

The bot automatically seeds test data in Development mode:

**AlphaTeam:**
- Player 1: Discord ID `1348719242882584689` (Captain)
- Player 2: Discord ID `1348724033306366055`

**BravoTeam:**
- Player 1: Discord ID `1348724778906681447` (Captain)
- Player 2: Discord ID `1348725467422916749`

To reset seed data, drop and recreate the database.

## Debugging

```bash
# Set environment to Development
# Windows PowerShell:
$env:ASPNETCORE_ENVIRONMENT="Development"; dotnet run --project src/WabbitBot.Host

# Windows CMD:
set ASPNETCORE_ENVIRONMENT=Development && dotnet run --project src/WabbitBot.Host

# Linux/Mac:
ASPNETCORE_ENVIRONMENT=Development dotnet run --project src/WabbitBot.Host

# Enable sensitive data logging (for debugging SQL queries)
dotnet user-secrets set "UseSensitiveDataLogging" "true"
dotnet user-secrets set "UseDetailedErrors" "true"
```

## Discord Bot Setup

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create New Application
3. Go to "Bot" section
4. Reset Token and copy it
5. Enable Privileged Gateway Intents:
   - ✅ Server Members Intent
   - ✅ Message Content Intent
6. Go to "OAuth2 > URL Generator"
7. Select scopes: `bot`, `applications.commands`
8. Select permissions: `Administrator` (for testing)
9. Copy URL and invite bot to your server

## Useful Commands

```bash
# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks

# List project references
dotnet list reference

# Check PostgreSQL connection
psql -h localhost -U wabbitbot -d wabbitbot

# View running processes
# Windows: tasklist | findstr WabbitBot
# Linux: ps aux | grep WabbitBot

# Kill process by port (if port 5432 is busy)
# Windows: netstat -ano | findstr :5432
#          taskkill /PID <PID> /F
# Linux: lsof -ti:5432 | xargs kill
```

## Common Issues

### Bot won't start - "Missing Bot:Token"
```bash
dotnet user-secrets set "Bot:Token" "YOUR_TOKEN" --project src/WabbitBot.Host
```

### Database connection failed
```bash
# Check PostgreSQL is running
# Windows: Check Services for "postgresql"
# Linux: systemctl status postgresql
# Docker: docker ps | grep postgres

# Test connection
psql -h localhost -U wabbitbot -d wabbitbot
```

### Port already in use
```bash
# Find process using port
netstat -ano | findstr :5432  # Windows
lsof -ti:5432                 # Linux/Mac

# Change PostgreSQL port in connection string
dotnet user-secrets set "Bot:Database:ConnectionString" "Host=localhost;Port=5433;Database=wabbitbot;Username=wabbitbot;Password=wabbitbot"
```

### Build errors after pulling changes
```bash
dotnet clean
dotnet restore
dotnet build
```

### Migration errors
```bash
# Remove last migration (if not applied)
dotnet ef migrations remove --project src/WabbitBot.Core --startup-project src/WabbitBot.Host

# Reset database
dotnet ef database drop --project src/WabbitBot.Core --startup-project src/WabbitBot.Host --force
dotnet ef database update --project src/WabbitBot.Core --startup-project src/WabbitBot.Host
```

### Slash commands not updating
```bash
# Option 1: Use debug guild for instant updates (recommended for development)
dotnet user-secrets set "Bot:DebugGuildId" "YOUR_GUILD_ID" --project src/WabbitBot.Host

# Option 2: Wait for Discord's global command cache to update (~1 hour)

# Option 3: Kick and re-invite the bot to your server to force cache clear
# 1. Go to Server Settings → Members
# 2. Right-click bot → Kick
# 3. Re-invite using OAuth2 URL from Discord Developer Portal

# To get your guild ID:
# 1. Enable Developer Mode in Discord (Settings → Advanced → Developer Mode)
# 2. Right-click your server icon → Copy Server ID
```

## File Locations

| Item             | Location                                                      |
|------------------|---------------------------------------------------------------|
| Configuration    | `src/WabbitBot.Host/appsettings.json`                         |
| User Secrets     | `%APPDATA%\Microsoft\UserSecrets\` (Windows)                  |
| Logs             | Console output                                                |
| Database Context | `src/WabbitBot.Core/Common/Database/`                         |
| Migrations       | Auto-generated by EF Core                                     |
| Seed Data        | `src/WabbitBot.Core/Common/Database/DevelopmentDataSeeder.cs` |
| Discord Layer    | `src/WabbitBot.DiscBot/DSharpPlus/`                           |
| Business Logic   | `src/WabbitBot.Core/{Feature}/`                               |

## Documentation Links

- [Full Development Guide](./DEVELOPMENT.md)
- [Architecture](../docs/.dev/architecture/event-system-architecture.md)
- [Database Seeding](../docs/.dev/database-seed-data.md)
- [Contributing Guidelines](../AGENTS.md)

