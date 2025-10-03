# Proposal: Remove IDbContextFactory Abstraction

## Summary
Remove `IDbContextFactory<WabbitBotDbContext>` and have `EfRepositoryAdapter` use `WabbitBotDbContextProvider` directly.

## Rationale

### 1. Single Consumer
`IDbContextFactory` is used by exactly ONE class: `EfRepositoryAdapter`

### 2. Architectural Misalignment
- Project principle: **No runtime DI**
- `IDbContextFactory` is a DI-era abstraction
- Conflicts with static provider pattern

### 3. Unnecessary Indirection
```
Current:
EfRepositoryAdapter → IDbContextFactory (interface) 
                   → WabbitBotDbContextProviderAdapter (wrapper)
                   → WabbitBotDbContextProvider (actual)

Proposed:
EfRepositoryAdapter → WabbitBotDbContextProvider (actual)
```

### 4. Testability Unchanged
Tests can still:
- Initialize `WabbitBotDbContextProvider` with test configuration
- Use in-memory database (better tests than mocks anyway)
- Mock `WabbitBotDbContextProvider` if needed (it's a static class)

## Proposed Changes

### Before:
```csharp
// EfRepositoryAdapter.cs
public class EfRepositoryAdapter<TEntity> : IRepositoryAdapter<TEntity>
{
    private readonly IDbContextFactory<WabbitBotDbContext> _dbContextFactory;

    public EfRepositoryAdapter(IDbContextFactory<WabbitBotDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<TEntity?> GetByIdAsync(object id)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Set<TEntity>().FindAsync(id);
    }
}
```

### After:
```csharp
// EfRepositoryAdapter.cs
public class EfRepositoryAdapter<TEntity> : IRepositoryAdapter<TEntity>
{
    // No constructor needed - uses static provider directly

    public async Task<TEntity?> GetByIdAsync(object id)
    {
        await using var db = WabbitBotDbContextProvider.CreateDbContext();
        return await db.Set<TEntity>().FindAsync(id);
    }
}
```

### Registration Simplification:
```csharp
// CoreService.Database.cs - Before
public static void RegisterRepositoryAdapters()
{
    var factory = new WabbitBotDbContextProviderAdapter(); // Extra wrapper!
    RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Player>(factory));
    // ...
}

// CoreService.Database.cs - After
public static void RegisterRepositoryAdapters()
{
    RepositoryAdapterRegistry.RegisterAdapter(new EfRepositoryAdapter<Player>());
    // ...
}
```

## Files to Modify

### Update:
- `src/WabbitBot.Core/Common/Database/EfRepositoryAdapter.cs`
  - Remove `_dbContextFactory` field
  - Remove constructor
  - Replace all `_dbContextFactory.CreateDbContextAsync()` with `WabbitBotDbContextProvider.CreateDbContext()`

- `src/WabbitBot.Core/Common/Services/Core/CoreService.Database.cs`
  - Simplify `RegisterRepositoryAdapters()` (no factory needed)

### Delete:
- `src/WabbitBot.Core/Common/Database/WabbitBotDbContextProviderAdapter.cs` (no longer needed!)

### No Changes Needed:
- `src/WabbitBot.Core/Common/Database/WabbitBotDbContextProvider.cs` (unchanged)
- All other code using `WabbitBotDbContextProvider` directly (already correct!)

## Benefits

1. **Simpler code** - 3 fewer references to track
2. **Fewer files** - delete WabbitBotDbContextProviderAdapter
3. **Clearer intent** - no "why do we have this interface?" questions
4. **Architectural consistency** - aligns with static provider pattern
5. **Easier onboarding** - one less abstraction layer to explain

## Risks

### Minimal:
- **Testing:** Can still test with real DB or configured provider
- **Flexibility:** If we ever need multiple implementations (why would we?), we can add abstraction then (YAGNI)
- **EF Convention:** We're choosing project consistency over EF convention

## Testing Strategy

```csharp
// Unit tests can configure the provider
[SetUp]
public void Setup()
{
    var testConfig = BuildTestConfiguration();
    WabbitBotDbContextProvider.Initialize(testConfig);
}

// Or use in-memory database (better integration testing)
[SetUp]
public void Setup()
{
    // Configure provider to use in-memory DB
    // This is MORE valuable than mocking anyway
}
```

## Decision

**Recommendation: REMOVE the abstraction**

Reasons:
1. Aligns with "no runtime DI" principle
2. Reduces complexity
3. No loss of testability
4. Follows YAGNI (You Aren't Gonna Need It)

If we later discover we need the abstraction, it's easy to add back. But experience shows we won't need it.

## Migration Steps

1. Update `EfRepositoryAdapter<TEntity>` to use `WabbitBotDbContextProvider` directly
2. Update `RegisterRepositoryAdapters()` to construct without factory parameter
3. Delete `WabbitBotDbContextProviderAdapter.cs`
4. Build and verify
5. Update any tests that were mocking `IDbContextFactory`

Estimated effort: **15 minutes**
