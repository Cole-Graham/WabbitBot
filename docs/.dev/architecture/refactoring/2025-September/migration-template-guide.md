# EF Core Migration Template Guide for Version Metadata

## Purpose

This guide provides best practices for creating EF Core migrations that include version metadata tracking to support the Application & Database Versioning Strategy implemented in Step 6.6.

## Quick Reference

Every migration that introduces schema changes should insert a record into the `schema_metadata` table to track:
- Schema version identifier
- Description of changes
- Breaking change flag
- Compatibility notes
- Migration name

## Migration Template with Version Metadata

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WabbitBot.Core.Migrations
{
    /// <summary>
    /// [Brief description of what this migration does]
    /// Schema Version: [version-id]
    /// Breaking Change: [Yes/No]
    /// </summary>
    public partial class AddNewFeature : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Perform schema changes
            migrationBuilder.CreateTable(
                name: "new_table",
                columns: table => new
                {
                    id = table.Column<Guid>(nullable: false),
                    name = table.Column<string>(maxLength: 255, nullable: false),
                    created_at = table.Column<DateTime>(nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_new_table", x => x.id);
                });

            // 2. Insert version metadata
            migrationBuilder.Sql(@"
                INSERT INTO schema_metadata
                (id, schema_version, applied_at, applied_by, description, is_breaking_change, compatibility_notes, migration_name, created_at, updated_at)
                VALUES
                (
                    gen_random_uuid(),
                    '002-1.1',
                    NOW(),
                    'AutoMigration',
                    'Add new feature table with backward compatibility',
                    false,
                    'Compatible with app versions 1.1.0+. Does not affect existing functionality.',
                    '20250930120000_AddNewFeature',
                    NOW(),
                    NOW()
                )
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Remove version metadata
            migrationBuilder.Sql(@"
                DELETE FROM schema_metadata
                WHERE migration_name = '20250930120000_AddNewFeature'
            ");

            // 2. Rollback schema changes
            migrationBuilder.DropTable(
                name: "new_table");
        }
    }
}
```

## Schema Version Naming Convention

Format: `{major}-{minor}.{patch}`

- **Major** (001, 002, 003, ...): Incremented for significant schema changes or breaking changes
- **Minor** (1, 2, 3, ...): Incremented for new features or non-breaking changes
- **Patch** (0, 1, 2, ...): Incremented for bug fixes or minor adjustments

Examples:
- `001-1.0` - Initial schema
- `001-1.1` - Added indexes for performance
- `002-1.0` - Added new leaderboard tables (breaking change)
- `002-1.1` - Added new columns to existing tables

## Breaking vs. Non-Breaking Changes

### Non-Breaking Changes (`is_breaking_change: false`)
- Adding new tables
- Adding new columns with default values
- Adding new indexes
- Renaming columns (with appropriate migration logic)

### Breaking Changes (`is_breaking_change: true`)
- Removing columns
- Changing column types incompatibly
- Removing tables
- Adding non-nullable columns without defaults
- Changing primary keys

## Compatibility Notes Guidelines

Be specific about version requirements:

✅ **Good Examples:**
- `"Compatible with app versions 1.1.0+. Does not affect existing functionality."`
- `"Requires app version 1.2.0 or higher. New leaderboard features depend on this schema."`
- `"Backward compatible with app 1.0.x. Forward compatible with app 1.1.x."`

❌ **Bad Examples:**
- `"Works fine"` (too vague)
- `"Should be okay"` (uncertain)
- `"Compatible"` (no version specified)

## Example Migrations

### Example 1: Non-Breaking Feature Addition

```csharp
public partial class AddTeamStatistics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Dictionary<string, object>>(
            name: "statistics",
            table: "teams",
            type: "jsonb",
            nullable: false,
            defaultValue: "{}");

        migrationBuilder.Sql(@"
            INSERT INTO schema_metadata
            (id, schema_version, applied_at, applied_by, description, is_breaking_change, compatibility_notes, migration_name, created_at, updated_at)
            VALUES
            (
                gen_random_uuid(),
                '001-1.2',
                NOW(),
                'AutoMigration',
                'Add JSONB statistics column to teams table',
                false,
                'Compatible with app 1.0.0+. Old versions will ignore the statistics field.',
                '20250930130000_AddTeamStatistics',
                NOW(),
                NOW()
            )
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM schema_metadata WHERE migration_name = '20250930130000_AddTeamStatistics'");
        
        migrationBuilder.DropColumn(
            name: "statistics",
            table: "teams");
    }
}
```

### Example 2: Breaking Change Migration

```csharp
public partial class RestructureLeaderboards : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // This is a breaking change - restructuring leaderboard tables
        migrationBuilder.DropTable(name: "old_leaderboards");

        migrationBuilder.CreateTable(
            name: "leaderboards",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                season_id = table.Column<Guid>(nullable: false),
                rankings = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: false),
                created_at = table.Column<DateTime>(nullable: false)
            });

        migrationBuilder.Sql(@"
            INSERT INTO schema_metadata
            (id, schema_version, applied_at, applied_by, description, is_breaking_change, compatibility_notes, migration_name, created_at, updated_at)
            VALUES
            (
                gen_random_uuid(),
                '002-1.0',
                NOW(),
                'AutoMigration',
                'Restructure leaderboard tables with new JSONB schema',
                true,
                'BREAKING CHANGE: Requires app version 1.2.0 or higher. Incompatible with app versions 1.0.x and 1.1.x.',
                '20250930140000_RestructureLeaderboards',
                NOW(),
                NOW()
            )
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM schema_metadata WHERE migration_name = '20250930140000_RestructureLeaderboards'");
        
        migrationBuilder.DropTable(name: "leaderboards");
        
        // Recreate old structure (if possible)
        migrationBuilder.CreateTable(name: "old_leaderboards", ...);
    }
}
```

## Integration with VersionMonitor

The `VersionMonitor` service uses the `schema_metadata` table to:
1. Check current schema version against application version
2. Alert on version drift
3. Provide detailed compatibility information

## Best Practices

1. **Always add metadata** - Every migration should include a `schema_metadata` insert
2. **Be honest about breaking changes** - Set `is_breaking_change: true` when appropriate
3. **Provide clear compatibility notes** - Specify exact version requirements
4. **Use semantic versioning** - Follow the schema version naming convention
5. **Test rollbacks** - Ensure the `Down()` migration properly removes metadata
6. **Document in migration** - Add XML comments with schema version and breaking change info

## Automation Opportunities

Consider creating:
- **Migration scaffolding script** - Generates migrations with metadata template
- **Pre-commit hook** - Validates that migrations include metadata
- **CI/CD check** - Verifies metadata exists in new migrations

## References

- [refactor-step6.6.md](./refactor-step6.6.md) - Application & Database Versioning Strategy
- [SchemaMetadata.cs](../../../src/WabbitBot.Core/Common/Models/Common/SchemaMetadata.cs) - Entity definition
- [VersionMonitor.cs](../../../src/WabbitBot.Core/Common/Utilities/VersionMonitor.cs) - Monitoring service
