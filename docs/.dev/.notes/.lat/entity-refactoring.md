# Procedural Entity Refactoring Checklist

This checklist ensures all parts of the application are updated when entity class definitions are modified during procedural programming refactoring.

## Database Layer
- [ ] **Entity Framework DbContext** (`WabbitBot.Core/Common/Database/WabbitBotDbContext/`)
  - Update entity configurations for changed/removed fields
  - Add configurations for new properties
  - Update table/column mappings (e.g., `WabbitBotDbContext.Game.cs`)

- [ ] **Database Context Provider** (`WabbitBot.Core/Common/Database/WabbitBotDbContextProvider.cs`)
  - Update context options for changed entity configurations
  - Modify provider-specific settings if needed

- [ ] **Database Context Factory** (`WabbitBot.Core/Common/Database/WabbitBotDbContextFactory.cs`)
  - Update factory methods for new entity types
  - Modify context creation logic

- [ ] **Database Settings** (`WabbitBot.Core/Common/Database/DatabaseSettings.cs`)
  - Update connection string logic for new entities if needed
  - Modify validation rules for database configuration

- [ ] **Database Migrations** (`WabbitBot.Common/Data/Schema/Migrations/`)
  - Create new migration for schema changes (e.g., `CreateGamesTable.cs`)
  - Update existing migration files if needed
  - Run migration scripts and verify data integrity

- [ ] **Entity DbConfig Files** (`WabbitBot.Core/Common/Models/*.DbConfig.cs`)
  - Update column arrays for changed/removed properties (e.g., `Game.DbConfig.cs`)
  - Modify table/archive table names if needed
  - Update cache settings and expiry times
  - Add new DbConfig classes for new entity types

## Repository Layer
- [ ] **Database Service Repository** (`WabbitBot.Common/Data/Service/DatabaseService.Repository.cs`)
  - Update CRUD operations for changed properties
  - Modify query builders and filtering logic
  - Update caching logic if property-based

- [ ] **Base Repository** (`WabbitBot.Common/Data/Repository.cs`)
  - Update generic repository operations
  - Modify query helpers and utilities

## Service Layer
- [ ] **Core Services** (`WabbitBot.Core/Common/Services/`)
  - Update business logic using changed properties
  - Modify calculation methods (e.g., `CoreService.Player.cs`)
  - Update data access patterns

- [ ] **Command Classes** (`WabbitBot.Core/*/Commands/`)
  - Update command processing logic (e.g., `ScrimmageCommands.cs`)
  - Modify validation and result objects
  - Update method signatures

## Event System
- [ ] **Event Classes** (`WabbitBot.Core/Common/Events/`)
  - Update event property definitions (e.g., `GameEvents.cs`)
  - Modify event data structures
  - Update event serialization

- [ ] **Event Handlers** (`WabbitBot.Core/Common/Handlers/`)
  - Update event processing logic
  - Modify event routing and handling

## Validation Layer
- [ ] **Deprecated Validation** (`src/deprecated/Validation/`)
  - Update validation rules for changed properties (if still used)
  - Remove validators for deleted properties
  - Add validators for new properties

## External Interfaces
- [ ] **Discord Commands** (`WabbitBot.DiscBot/DSharpPlus/Commands/`)
  - Update command parameter handling (e.g., `ScrimmageCommandsDiscord.cs`)
  - Modify embed generation and user interactions
  - Update command method signatures

- [ ] **Discord Embeds** (`WabbitBot.DiscBot/DSharpPlus/Embeds/`)
  - Update embed field mappings (e.g., `ScrimmageEmbed.cs`)
  - Modify embed styling and content

- [ ] **Discord Interactions** (`WabbitBot.DiscBot/DSharpPlus/Interactions/`)
  - Update button/modal interactions
  - Modify interaction handlers

## Testing Layer
- [ ] **Entity Config Tests** (`WabbitBot.Core/Common/Config/EntityConfigTests.cs`)
  - Update test configurations for changed entities
  - Modify test assertions

- [ ] **Database Integration Tests** (`WabbitBot.Core/Common/Database/Tests/DbContextIntegrationTest.cs`)
  - Update test entity creation and data
  - Modify test queries and assertions
  - Update expected test results

## Source Generation
- [ ] **Source Generators** (`WabbitBot.SourceGenerators/Generators/`)
  - Update embed generators for changed entities
  - Modify command generators
  - Update event bus generators
  - Regenerate affected generated files

- [ ] **Generated Code** (`WabbitBot.DiscBot/DSharpPlus/Generated/`)
  - Regenerate embed factories and styling
  - Update command registrations
  - Refresh event bus code

## Serialization and Data Transfer
- [ ] **JSON Utilities** (`WabbitBot.Common/Data/Utilities/JsonUtil.cs`)
  - Update JSON serialization logic
  - Modify property mappings

- [ ] **Query Utilities** (`WabbitBot.Common/Data/Utilities/QueryUtil.cs`)
  - Update query building for changed properties
  - Modify SQL generation

## Configuration and Setup
- [ ] **Configuration Service** (`WabbitBot.Common/Configuration.cs`)
  - Update configuration validation for new entity properties
  - Modify configuration options classes (e.g., `BotOptions`)
  - Update configuration binding logic

- [ ] **Configuration Files** (`src/appsettings*.json`)
  - Update any entity-related configuration
  - Modify default values and settings

- [ ] **Database Connection** (`WabbitBot.Common/Data/DatabaseConnection.cs`)
  - Update connection configuration if needed
  - Modify provider settings

## Documentation
- [ ] **Code Comments**
  - Update XML documentation on changed properties
  - Modify inline code comments

- [ ] **Architecture Documentation** (`docs/`)
  - Update entity relationship diagrams
  - Modify data flow documentation
  - Update design documents

## Deployment and Migration
- [ ] **Data Migration Scripts**
  - Create data transformation scripts for production
  - Update existing data in databases

- [ ] **Schema Manager** (`WabbitBot.Common/Data/Schema/SchemaManager.cs`)
  - Update schema management logic
  - Modify migration execution

## Quality Assurance
- [ ] **Build Verification**
  - Ensure all projects compile successfully
  - Run build and check for errors

- [ ] **Integration Testing**
  - Test end-to-end workflows manually
  - Verify data persistence and retrieval

## Monitoring and Logging
- [ ] **Error Service** (`WabbitBot.Common/ErrorService/`)
  - Update error handling for changed entities
  - Modify error logging and reporting

- [ ] **Core Event Bus** (`WabbitBot.Core/Common/BotCore/CoreEventBus.cs`)
  - Update event publishing for changed entities
  - Modify event routing

## Security Considerations
- [ ] **Permission Attributes** (`WabbitBot.DiscBot/DSharpPlus/Attributes/`)
  - Update permission checks for changed entities
  - Modify access control logic

- [ ] **Input Validation**
  - Update input sanitization in command handlers
  - Modify validation in Discord interactions

## Rollback Plan
- [ ] **Database Rollback**
  - Create rollback migration scripts
  - Test rollback procedures

- [ ] **Code Rollback**
  - Maintain backward-compatible code branches
  - Document rollback procedures
