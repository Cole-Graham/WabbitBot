# WabbitBot

A Discord bot for running tournaments and a ranked scrimmage system for WARNO (Wargame Army NATO).

## Features

- **Scrimmage System**: Ranked 1v1, 2v2, and 10v10 matches with ELO-based rating
- **Tournament Management**: Single/double elimination brackets with automated scheduling
- **Team Management**: Create and manage teams with rosters for different game modes
- **Match Tracking**: Automatic game state management with replay upload support
- **Leaderboards**: Dynamic rankings with seasonal resets
- **Map Pool Management**: Customizable map bans and selections

## Technology Stack

- **Framework**: .NET 9.0 (C# 13)
- **Discord Library**: DSharpPlus 5.0
- **Database**: PostgreSQL 16+ with EF Core 9.0
- **Architecture**: Vertical slice architecture with event-driven communication
- **Code Generation**: Source generators for boilerplate reduction

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 16](https://www.postgresql.org/download/)
- Discord Bot Token ([Get one here](https://discord.com/developers/applications))

### Running Locally

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/WabbitBot.git
   cd WabbitBot
   ```

2. **Set up PostgreSQL**
   ```sql
   CREATE DATABASE wabbitbot;
   CREATE USER wabbitbot WITH PASSWORD 'wabbitbot';
   GRANT ALL PRIVILEGES ON DATABASE wabbitbot TO wabbitbot;
   ```

3. **Configure bot token** (using user secrets)
   ```bash
   cd src/WabbitBot.Host
   dotnet user-secrets set "Bot:Token" "YOUR_DISCORD_BOT_TOKEN"
   ```

4. **Build and run**
   ```bash
   dotnet build
   dotnet run --project src/WabbitBot.Host
   ```

📖 **See [DEVELOPMENT.md](./docs/DEVELOPMENT.md) for detailed setup instructions**

## Documentation

- **[Development Guide](./docs/DEVELOPMENT.md)** - Complete setup and local development guide
- **[Architecture Overview](./docs/.dev/architecture/event-system-architecture.md)** - System architecture and design patterns
- **[Contributing Guidelines](./AGENTS.md)** - Code standards and architecture principles
- **[Database Guide](./docs/.dev/architecture/database/database-migration-strategy.md)** - Database setup and migrations
- **[Deployment Guide](./docs/.dev/hosting/hostinger-deployment.md)** - Production deployment instructions

## Project Structure

```
WabbitBot/
├── src/
│   ├── WabbitBot.Host/              # Entry point and configuration
│   ├── WabbitBot.Core/              # Business logic (tournaments, scrimmages, matches)
│   ├── WabbitBot.DiscBot/           # Discord integration layer
│   ├── WabbitBot.Common/            # Shared contracts and infrastructure
│   └── WabbitBot.SourceGenerators/  # Compile-time code generation
├── docs/                            # Documentation
└── data/                            # Runtime data (images, replays)
```

## Contributing

This project uses specific architectural patterns and guidelines. Please review [AGENTS.md](./AGENTS.md) before contributing.

Key principles:
- Vertical slice architecture for feature organization
- Event-driven communication via typed event buses
- No runtime dependency injection (static service locator pattern)
- PostgreSQL for all data persistence
- Source generators for reducing boilerplate

## License

[MIT License](./LICENSE)
