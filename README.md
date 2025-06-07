# WabbitBot

A Discord bot for running tournaments and a ranked scrimmage system.

# Libraries

- Uses the latest Nightly build of DSharpPlus 5.0

## Setup

1. Clone the repository
2. Copy `src/output/config.template.json` to `src/output/config.json` and configure:
   - Set your Discord bot token
   - Adjust prefix if desired (default: "!")
   - Configure admin and map management roles
   - Customize bot activity status

3. Map Configuration:
   - Default maps are defined in `src/WabbitBot.Core/Config/maps.default.json`
   - For custom maps, copy `maps.default.json` to `maps.json` in the same directory
   - Use admin commands to manage maps:
     - `!maps export` - Export current map configuration
     - `!maps import` - Import maps from a JSON file
     - `!maps add` - Add or update a map
     - `!maps remove` - Remove a map

## Development

The project uses a vertical slice architecture with the following components:
- `WabbitBot.Common` - Shared code between one or more of the other 3 projects
- `WabbitBot.Core` - Core business logic and models
- `WabbitBot.DiscBot` - Discord bot implementation using DSharpPlus
- `WabbitBot.SourceGenerators` - Command registration source generators

## Building and Running

1. Ensure you have .NET 9.0+ installed
2. Build the solution: `dotnet build`
3. Run the bot: `dotnet run --project src/WabbitBot.DiscBot`

## Configuration Files

- `config.json` - Bot configuration (token, settings)
- `maps.json` - Server-specific map configuration
- `maps.default.json` - Default map configuration template

Both `config.json` and `maps.json` are gitignored to prevent committing sensitive data or server-specific configurations.
