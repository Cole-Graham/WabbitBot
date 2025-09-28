# WabbitBot Architecture Refactor Implementation Steps

## Step 1: Entity Redefinition for Native PostgreSQL JSON Support ✅ COMPLETE
1a. Entity Redefinition for Native PostgreSQL JSON Support
1b. Remove Manual JSON Properties
1c. Use Native Complex Objects
1d. Leverage PostgreSQL JSONB Features
1e. Npgsql Automatic Mapping

## Step 2: EF Core Foundation ✅ COMPLETE
2a. Add Npgsql.EntityFrameworkCore.PostgreSQL Package
2b. Create WabbitBotDbContext with JSONB Configurations
2c. Configure Entity Mappings for JSONB Columns
2d. Set up Connection String Management

## Step 3: Database Layer Refinement ✅ COMPLETE
3a. Implement `DatabaseService<TEntity>` with partial classes for Repository, Cache, and Archive
3b. Consolidate logic from `RepositoryService`, `CacheService`, and `ArchiveService` into `DatabaseService` partials
3c. Remove old `RepositoryService`, `CacheService`, and `ArchiveService` classes
3d. Remove MapEntity/BuildParameters Methods from Component Classes
3e. Implement Entity Configuration Pattern

## Step 4: Schema Migration ✅ COMPLETE
4a. Implement Database Versioning Strategy
4b. Handle Schema Migration Scripts
4c. Add JSONB Indexes for Performance Optimization
4d. Update Table Structures for Native JSON Support
4e. Migrate Existing Data (if any)

## Step 5: Additional Entity Integration ✅ COMPLETE
5a. Integrate Stats Entity (Remove IJsonVersioned, Inherit from Entity)
5b. Integrate SeasonConfig Entity (Runtime Season Configuration)
5c. Integrate SeasonGroup Entity (Season Grouping)
5d. Integrate LeaderboardItem Entity (Extract from Leaderboard)
5e. Handle IJsonVersioned Interface Removal

## Step 5.5: Re-evaluate ListWrapper Classes in PostgreSQL/EF Core Architecture ✅ COMPLETE
5.5a. Critical Analysis: Evaluate if ListWrapper classes make sense in PostgreSQL/EF Core architecture
5.5b. Decision: ELIMINATE ListWrapper classes - they add unnecessary complexity
5.5c. Move Business Logic: Transfer valuable logic to CoreService
5.5d. Add Strategic Caching: Implement caching directly in CoreService where needed
5.5e. Update Dependencies: Remove all references to ListWrapper classes
5.5f. Delete Classes: Remove ListWrapper classes and interfaces entirely

## Step 6: JSONB Schema Migration ✅ COMPLETE
6a. Implement EF Core Migrations Strategy (no production data concerns)
6b. Update Database Schema to Use JSONB Columns
6c. Add JSONB Indexes for Performance Optimization
6d. Update Table Structures for Native JSON Support
6e. Migration Strategy Documented (./database-migration-strategy.md)

## Step 6.1: Entity Relationship Type Correction ✅ COMPLETED
6.1a. Analyze Current String Reference Usage
6.1b. Update Entity Class Properties
6.1c. Update Database Migration Scripts
6.1d. Update EF Core Configurations
6.1e. Implement Data Migration Strategy
6.1f. Update Business Logic Code
6.1g. Update LINQ Queries
6.1h. Move Entity Definitions to Common Directory

## Step 6.2: Code Organization and Cleanup ✅ COMPLETED
6.2a. Reorganize ConfigureIndexes Method Regions
6.2b. Reorganize EntityConfigurations.cs by Entity Groups
6.2c. Reorganize EntityConfigTests.cs to Match New Structure

## Step 6.3: Simplify Map Entity and Refactor Thumbnail Management ✅ COMPLETED
6.3a. Simplify Map Entity
6.3b. Create ThumbnailUtility in WabbitBot.Common
6.3c. Update Configuration for Thumbnail Management
6.3d. Update Usage in Application Services
6.3e. Update Tests

## Step 6.4: Unified DatabaseService Architecture ✅ COMPLETED
6.4a. Create DatabaseService Structure with Partial Classes ✅ COMPLETED
6.4b. Implement Repository Operations (DatabaseService.Repository.cs) ✅ COMPLETED
6.4c. Implement Cache Operations (DatabaseService.Cache.cs) ✅ COMPLETED
6.4d. Implement Archive Operations (DatabaseService.Archive.cs) ✅ COMPLETED
6.4e. Update CoreService Integration ✅ COMPLETED
6.4f. Migrate Entities to DatabaseService ⏳ PENDING (boilerplate left as-is)
6.4g. Clean Up Old Services ✅ COMPLETED

## Step 6.5: Legacy Closure and Gap Remediation ✨ NEW
6.5a. Implement unified `ErrorService` architecture
6.5b. Finish Step 6.4f by migrating active features to `DatabaseService<T>`
6.5c. Replace lingering runtime DI or event bus hooks that point at legacy assemblies
6.5d. Refresh tests to match the refactored paths and remove suites tied to deprecated code

## Step 6.6: Application & Database Versioning Strategy ✨ NEW
6.6a. Create Application Version Tracking (ApplicationInfo Class)
6.6b. Implement Schema Version Tracking (SchemaVersionTracker Class)
6.6c. Add Version Compatibility Checking on Startup
6.6d. Create Feature Flags System (FeatureManager for Gradual Rollouts)
6.6e. Implement Version Metadata Table (Schema_Metadata for Audit Trail)
6.6f. Add Version Drift Monitoring (Alerting for Incompatible Combinations)
6.6g. Create Compatibility Test Suite (VersionCompatibilityTests)
6.6h. Update Migration Templates (Version Metadata Integration)

## Step 7: CoreService Organization
7a. Create Main `CoreService.cs` for business logic orchestration
7b. Utilize `DatabaseService<TEntity>` for all data access
7c. Utilize `ErrorService` for all error handling
7d. Organize `CoreService` with partial classes for different domains (e.g., Player, Team)
7e. Set up Direct Instantiation and Event Messaging (No Runtime DI)

## Step 8: Entity Migration
8a. Migration Strategy: Move all entity-specific business logic into `CoreService` partials
8b. Foundation Phase
8c. CoreService Structure Phase
8d. Entity Migration Phase
8e. Testing & Cleanup Phase

## Step 9: Configuration and PostgreSQL Setup
9a. PostgreSQL JSON Strategy - Use Npgsql Library
9b. Configuration Management
9c. Database Schema Changes
9d. Unified Table Structure
9e. Indexing Strategy

## FINALIZATION/REFINEMENT: Testing & Cleanup
- **Final Validation and Architecture Cleanup** - Comprehensive testing and cleanup to ensure production readiness
- **a. VersionCompatibilityTests** (see step 6.5) - Execute version compatibility test suite
- **b. Comprehensive Integration Testing** - Run full end-to-end integration tests covering all CoreService operations
- **c. Performance Benchmarking** - Execute performance benchmarks to ensure requirements are met
- **d. Production Readiness Validation** - Final checklist for production deployment readiness
- **e. Documentation Finalization** - Complete all documentation and deployment procedures

### Testing Strategy
- **Unit Tests**: CoreService operations, EF Core integration, caching behavior
- **Integration Tests**: End-to-end workflows, PostgreSQL JSONB operations
- **Performance Tests**: Query performance, caching efficiency, memory usage

### Cleanup Checklist
- **Remove Deprecated Code**: Delete old services, ListWrapper classes, unused interfaces
- **Configuration Updates**: Update appsettings.json, Program.cs, dependency injection
- **Documentation Updates**: Update README.md, API docs, migration guides

### Final Validation
- **Architecture Verification**: Single CoreService, EF Core integration, direct instantiation, event messaging
- **Performance Verification**: JSONB operations, query optimization, caching efficiency
- **Code Quality Verification**: Lean implementation, consistent patterns, proper error handling

### Success Metrics
- ⚪ All tests pass (unit, integration, performance)
- ⚪ Zero breaking changes (existing functionality preserved)
- ⚪ Performance improved (measurable gains over old architecture)
- ⚪ Code maintainable (new features easy to add)
- ⚪ Documentation complete (all changes documented)
- ⚪ Clean architecture (no deprecated code remaining)
