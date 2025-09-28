# EntityMetadata Attribute Parsing: Technical Deep Dive

## The Attribute Parsing Problem

### What Was the Issue?

The `EntityMetadataGenerator` source generator was failing to parse values from `[EntityMetadata]` attributes on entity classes. Despite the attributes being correctly applied to entity classes (e.g., `[EntityMetadata("players", "player_archive", 500, 30)]`), the generator was returning `null` values for all parsed attributes, causing generated configurations to fall back to default values instead of using the specified attribute values.

### Root Cause Analysis

The problem stemmed from a mismatch between how the `EntityMetadata` attribute was defined and how the generator was attempting to parse it.

#### Attribute Definition
```csharp
[EntityMetadata("players", "player_archive", 500, 30)]
public class Player { ... }
```

The `EntityMetadata` attribute uses **positional parameters** (arguments passed by position/order), not **named parameters** (arguments passed with explicit names).

#### Original Parsing Logic
The generator's `GetAttributeArgument()` method was designed to handle **named arguments** only:

```csharp
// This was looking for named arguments like: tableName: "players"
if (foundArgName == argumentName) { ... }
```

Since the attribute used positional arguments, `arg.NameEquals?.Name.Identifier.Text` was always `null`, causing the parsing to fail.

#### The Evidence
When we added debug output, the generator showed:
```
ATTRS: EntityMetadata; ARGS: unnamed = "players", unnamed = "player_archive", unnamed = 500, unnamed = 30
```

This confirmed:
1. The `EntityMetadata` attribute was being found correctly
2. All arguments were `unnamed` (positional), not named
3. The values were present but couldn't be extracted

## The Solution

### Implementation Details

We created two new methods to handle positional arguments:

#### `GetPositionalAttributeArgument()`
```csharp
private string? GetPositionalAttributeArgument(ClassDeclarationSyntax classDeclaration, string attributeName, int position)
{
    foreach (var attrList in classDeclaration.AttributeLists)
    {
        foreach (var attr in attrList.Attributes)
        {
            if (attrName.Contains("EntityMetadata"))
            {
                if (attr.ArgumentList != null && position < attr.ArgumentList.Arguments.Count)
                {
                    var arg = attr.ArgumentList.Arguments[position];
                    // Ensure it's not a named argument (NameEquals should be null for positional)
                    if (arg.NameEquals == null)
                    {
                        if (arg.Expression is LiteralExpressionSyntax literal)
                        {
                            var value = literal.Token.ValueText;
                            // Strip quotes from string literals
                            return value.StartsWith("\"") && value.EndsWith("\"")
                                ? value.Substring(1, value.Length - 2)
                                : value;
                        }
                    }
                }
            }
        }
    }
    return null;
}
```

#### `GetPositionalAttributeArgumentInt()`
```csharp
private int? GetPositionalAttributeArgumentInt(ClassDeclarationSyntax classDeclaration, string attributeName, int position)
{
    foreach (var attrList in classDeclaration.AttributeLists)
    {
        foreach (var attr in attrList.Attributes)
        {
            if (attrName.Contains("EntityMetadata"))
            {
                if (attr.ArgumentList != null && position < attr.ArgumentList.Arguments.Count)
                {
                    var arg = attr.ArgumentList.Arguments[position];
                    if (arg.NameEquals == null)
                    {
                        if (arg.Expression is LiteralExpressionSyntax literal &&
                            int.TryParse(literal.Token.ValueText, out var value))
                        {
                            return value;
                        }
                    }
                }
            }
        }
    }
    return null;
}
```

### Updated Parsing Logic
```csharp
// Before (broken):
var tableName = GetAttributeArgument(classDeclaration, "EntityMetadata", "tableName");

// After (working):
var tableName = GetPositionalAttributeArgument(classDeclaration, "EntityMetadata", 0);
var archiveTableName = GetPositionalAttributeArgument(classDeclaration, "EntityMetadata", 1);
var maxCacheSize = GetPositionalAttributeArgumentInt(classDeclaration, "EntityMetadata", 2);
var cacheExpiryMinutes = GetPositionalAttributeArgumentInt(classDeclaration, "EntityMetadata", 3);
```

### Verification
After the fix, the debug output showed correct parsing:
```
TableName from attr: players, Archive from attr: player_archive
```

And the generated configurations now use the attribute values instead of defaults.

## Guidance for Maintainers and Developers

### Understanding the EntityMetadata System

The `EntityMetadata` attribute follows a **"single source of truth"** principle where entity class definitions drive all database configuration. This eliminates the need for separate manual configuration files.

#### Current Attribute Structure
```csharp
[EntityMetadata(tableName, archiveTableName, maxCacheSize, cacheExpiryMinutes)]
```

**Parameter Order (CRITICAL)**:
1. `tableName` (string): Database table name
2. `archiveTableName` (string): Archive table name
3. `maxCacheSize` (int): Maximum cache size in items
4. `cacheExpiryMinutes` (int): Cache expiry time in minutes

### Modifying the EntityMetadata Attribute

#### Option 1: Keep Positional Parameters (Recommended)
If you want to maintain backward compatibility and the current simple syntax:

1. **Don't change the attribute definition** - keep it as positional parameters
2. **Update the parameter order** in the attribute definition if needed
3. **Update the generator** to match the new parameter positions
4. **Update all entity class attributes** to use the new parameter values/order

#### Option 2: Switch to Named Parameters
If you prefer more explicit, self-documenting attributes:

1. **Change the attribute definition** to use named parameters:
   ```csharp
   [AttributeUsage(AttributeTargets.Class)]
   public class EntityMetadataAttribute : Attribute
   {
       public string TableName { get; set; }
       public string ArchiveTableName { get; set; }
       public int MaxCacheSize { get; set; } = 1000;
       public int CacheExpiryMinutes { get; set; } = 60;
   }
   ```

2. **Update entity classes** to use named syntax:
   ```csharp
   [EntityMetadata(TableName = "players", ArchiveTableName = "player_archive", MaxCacheSize = 500, CacheExpiryMinutes = 30)]
   ```

3. **Simplify the generator** to use the existing `GetAttributeArgument()` method for named parameters

### Modifying the Generator

#### When to Add New Attribute Parameters

1. **Add the parameter to the attribute class** (both `WabbitBot.Common` and `WabbitBot.SourceGenerators`)

2. **Add parsing logic in the generator**:
   ```csharp
   // For positional parameters (current approach):
   var newParameter = GetPositionalAttributeArgumentInt(classDeclaration, "EntityMetadata", 4); // Next position

   // For named parameters (future approach):
   var newParameter = GetAttributeArgumentInt(classDeclaration, "EntityMetadata", "newParameter");
   ```

3. **Update the EntityMetadata class** to store the new parameter

4. **Update the generated code** to use the new parameter

5. **Update tests** to expect the new values

#### Generator Architecture Notes

- The generator uses Roslyn's syntax analysis to inspect C# code at compile time
- `ClassDeclarationSyntax` provides access to the class structure and attributes
- `LiteralExpressionSyntax` handles string and numeric literals
- The generator creates source files that become part of the compilation

### Testing Attribute Changes

1. **Build the solution** to ensure no compilation errors
2. **Run the EntityConfigTests** - they should fail with the new expected values (this is correct!)
3. **Update test expectations** to match the new attribute values
4. **Verify generated code** contains the correct values

### Best Practices

#### For Attribute Definitions
- Keep attributes simple and focused
- Use positional parameters for brevity when parameter order is obvious
- Use named parameters when clarity is more important than brevity
- Document parameter order clearly in comments

#### For Generator Maintenance
- Always test parsing with debug output before removing it
- Handle both positional and named arguments if supporting both syntaxes
- Validate that required parameters are present
- Provide sensible defaults for optional parameters

#### For Entity Class Maintenance
- When adding `[EntityMetadata]` to a new entity, check existing examples for parameter values
- Test that the generated configuration matches expectations
- Update tests when changing attribute values

### Troubleshooting

#### Common Issues

1. **"Attribute parsing returns null"**
   - Check if using positional vs named parameters correctly
   - Verify parameter positions match the expected order
   - Ensure attribute is applied to the class (not properties/methods)

2. **"Generated code uses wrong values"**
   - Check that attribute values are correct
   - Verify parsing logic matches attribute parameter style
   - Ensure defaults are appropriate

3. **"Build fails with duplicate definitions"**
   - Check for conflicts between generated and manual configurations
   - Ensure deprecated files are properly removed
   - Clean and rebuild the solution

#### Debug Steps

1. Add debug output to see what attributes are found
2. Check if arguments are positional (`unnamed`) or named
3. Verify literal values are being extracted correctly
4. Test with a single entity class before applying broadly

### Future Considerations

- Consider supporting both positional and named syntax for flexibility
- Add validation in the generator to ensure required parameters are present
- Consider generating compile-time warnings for missing or invalid attributes
- Document attribute usage patterns for the development team

## Interface-Driven Attribute Validation (Future Enhancement)

### Current Limitations

C# interfaces **cannot directly enforce attribute usage**. Interfaces define method/property contracts but not attribute requirements. The current system relies on:

1. **Runtime detection** of marker interfaces (e.g., `IPlayerEntity`)
2. **Source generator analysis** to find missing interfaces
3. **Compile-time warnings** for architectural violations

### Potential Solutions for Interface-Driven Validation

#### Option 1: Roslyn Analyzer (Recommended)
Create a Roslyn analyzer that validates attribute usage at compile time:

```csharp
// In a separate analyzer project
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EntityMetadataAnalyzer : DiagnosticAnalyzer
{
    public override void Initialize(AnalysisContext context)
    {
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration,
            SyntaxKind.ClassDeclaration);
    }

    private void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if class implements marker interface
        var implementsMarkerInterface = classDeclaration.BaseList?.Types
            .Any(t => t.Type.ToString().EndsWith("Entity")) ?? false;

        if (implementsMarkerInterface)
        {
            // Require EntityMetadata attribute
            var hasEntityMetadata = classDeclaration.AttributeLists
                .Any(attrs => attrs.Attributes
                    .Any(attr => attr.Name.ToString().Contains("EntityMetadata")));

            if (!hasEntityMetadata)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    MissingEntityMetadataRule,
                    classDeclaration.Identifier.GetLocation()));
            }
        }
    }
}
```

#### Option 2: Source Generator Warnings
Extend the current generator to emit warnings for interface violations:

```csharp
// In EntityMetadataGenerator.cs
private void GenerateArchitecturalValidation(
    GeneratorExecutionContext context,
    ImmutableArray<EntityMetadata> entityMetadata)
{
    foreach (var metadata in entityMetadata)
    {
        // Check for missing EntityMetadata on classes implementing marker interfaces
        if (metadata.MarkerInterfaces.Any() && metadata.TableName == ToSnakeCase(metadata.ClassName))
        {
            // TableName is default (not from attribute), but class has marker interfaces
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor(
                    "WB0003",
                    "Missing EntityMetadata Attribute",
                    $"Class '{metadata.ClassName}' implements marker interfaces but lacks [EntityMetadata] attribute",
                    "Architecture",
                    DiagnosticSeverity.Warning,
                    isEnabledByDefault: true),
                Location.None));
        }
    }
}
```

#### Option 3: Interface Attributes (Limited)
While interfaces can't have attributes that enforce implementation, you can add documentation:

```csharp
/// <summary>
/// Marker interface for player entities.
/// Classes implementing this interface MUST have [EntityMetadata] attribute.
/// Expected attribute format: [EntityMetadata("players", "player_archive", maxCacheSize, cacheExpiryMinutes)]
/// </summary>
public interface IPlayerEntity { }
```

### Implementation Recommendation

For the EntityMetadata system, I recommend **Option 1 (Roslyn Analyzer)** because:

1. **Compile-time validation** catches issues immediately
2. **IDE integration** provides real-time feedback
3. **NuGet distribution** makes it reusable across projects
4. **Configurable severity** (warning vs error)

Example analyzer rules:
- Classes implementing `I*Entity` interfaces must have `[EntityMetadata]`
- `[EntityMetadata]` attributes should have valid parameter values
- Parameter order should match expected conventions

### Migration Path

1. **Create analyzer project** in the solution
2. **Implement validation rules** for current conventions
3. **Add analyzer as dependency** to WabbitBot.Core
4. **Update CI/CD** to include analyzer results
5. **Document new requirements** for contributors

This would provide the interface-driven validation you're looking for while maintaining backward compatibility.

---

This system provides a robust foundation for automated database configuration while maintaining flexibility for future enhancements.
