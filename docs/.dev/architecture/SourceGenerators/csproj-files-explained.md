# .csproj Files in WabbitBot: Configuration and Rationale

## Overview of Project Structure

The WabbitBot solution uses a multi-project structure to support a vertical slice architecture with clean separation of concerns. The main projects are:

- **Core Projects (net9.0)**: Business logic, database, and shared utilities.
- **DiscBot (net9.0)**: Discord integration using DSharpPlus.
- **Common (net9.0)**: Shared contracts, attributes, and infrastructure.
- **SourceGenerators and Analyzers (netstandard2.1)**: Compile-time code generation and analysis tools.
- **Test Projects (net9.0)**: Unit/integration tests for each component.

This setup allows for:
- **Vertical Slices**: Features like tournaments or scrimmages are self-contained.
- **Source Generators**: Auto-generate boilerplate (e.g., event records, DbContext) at compile time.
- **Analyzers**: Custom rules for code quality (e.g., attribute validation).
- **No Dependency Injection**: Per repo rules, using static services and event buses.

All projects target .NET 9.0 for modern features (C# 13, nullable reference types), except generators/analyzers, which use netstandard2.1 for Roslyn compatibility with modern SDK support.

## NetStandard2.0 vs NetStandard2.1 (Nvm, can't use 2.1... mustuse 2.0, need to update this document)

- **NetStandard2.0**: The baseline for broad compatibility (.NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+). While historically used for Roslyn-hosted projects, it has limitations in .NET 9 SDK like legacy shims (NU1701 warnings from transitive deps such as CodeAnalysis.Workspaces 1.0.1) and ref resolution issues (e.g., core types in obj files). Supports up to C# 7.3 natively, but <LangVersion>12.0</LangVersion> enables C# 12 features.

- **NetStandard2.1**: Extends 2.0 with modern APIs (Span<T>, improved async, better generics/tuples support). Backward-compatible with 2.0 (implements it fully), but offers superior integration in .NET 8/9 SDKs by providing native refs for compiler services (e.g., TupleElementNamesAttribute, ValueTuple, IEquatable<> via netstandard.dll facade).

  In this solution, **all Roslyn-related projects** (WabbitBot.Generator.Shared, WabbitBot.SourceGenerators, WabbitBot.Analyzers) now target netstandard2.1 to resolve build errors consistently:
  - WabbitBot.Generator.Shared: <LangVersion>10.0</LangVersion> (balances modern features with stability).
  - WabbitBot.SourceGenerators and WabbitBot.Analyzers: <LangVersion>12.0</LangVersion> (full C# 12 support for advanced generation/analysis).
  - All use <ImplicitUsings>enable</ImplicitUsings> and <Nullable>enable</Nullable>; IsRoslynComponent=true for hosting.
  - Fixes CS0012 (IEquatable not referenced), CS0234 (missing core types like System.String, Guid, DateTime, Enum, IEnumerable), and CS8137/CS8179 (tuples/generics) without explicit <PackageReference>s (e.g., no NETStandard.Library, System.ValueTuple, or <Reference> for mscorlib/System).
  - .NET 9 SDK quirks (e.g., obj/Debug/netstandard2.0 ref failures) are fully avoided—2.1 auto-includes the complete facade and handles compiler-generated files natively (no skips like <GenerateTargetFrameworkAttribute>false</GenerateTargetFrameworkAttribute> needed).
  - PrivateAssets="all" on CodeAnalysis packages ensures isolation for analyzer/generator use.

  **Why This Full Upgrade Works**: .NET 9 SDK fully supports netstandard2.1 for Roslyn components (Microsoft.CodeAnalysis 4.13.0+ compatible), providing better API coverage and ref resolution without breaking hosting or transitive deps. Dual <ProjectReference>s in Analyzers (one with OutputItemType="Analyzer" ReferenceOutputAssembly="false" for hosting; one regular for compile-time types) maintain clean separation while leveraging 2.1 benefits. Backward-compatible with 2.0 consumers (e.g., test projects via shims). Transitive shims (6 NU1701 warnings) remain cosmetic and suppressed.

**Verification**:
- Run: `dotnet clean; dotnet restore; dotnet build src/WabbitBot.Generator.Shared src/WabbitBot.SourceGenerators src/WabbitBot.Analyzers`
  - Expected: Restore ~0.5s (with shim warnings), builds succeed (0 errors across all). Core types/IEquatable resolve via netstandard2.1 facade.
- IDE: Reload window (Ctrl+Shift+P > "Developer: Reload Window")—no red squiggles on refs (e.g., AttributeAnalyzer.cs line 49 for IEquatable).
- Check packages: NETStandard.Library 2.1.0 auto-included (provides IEquatable<>). Suppress warnings in .vscode/settings.json: `"omnisharp.suppressDotnetRestoreNuGetWarning": true`.

Transitive issues arise because testing libs (e.g., Analyzer.Testing 1.1.2) depend on old Roslyn components, pulling Framework shims. These are harmless (compat code), but warnings are suppressed via settings.json or <NoWarn>.

## Transitive Reference Issues

Transitive references are packages pulled indirectly by direct dependencies. In this solution:
- **Legacy Shims**: Packages like Microsoft.CodeAnalysis.CSharp.Workspaces 1.0.1 are pulled by testing libs (Analyzer.Testing 1.1.2). They use .NET Framework v4.x shims for compat, triggering NU1701 warnings in net9.0 projects ("restored using .NETFramework instead of net9.0").
  - **Why?**: Old Roslyn tools were built for .NET Framework; shims bridge to modern .NET.
  - **Impact**: None—shims work fine, no perf/security risks. Builds succeed.
  - **Fix**: Suppress with <NoWarn>NU1701</NoWarn> in csproj or settings.json in .vscode.
- **Handling**: Direct CodeAnalysis 4.13.0 overrides shims where possible, but testing libs lock to 1.0.1. Warnings are ignored; focus on functionality.

## Project Files Explained

### WabbitBot.Core.csproj (net9.0)
**Purpose**: Main business logic (DbContext, services, events). Vertical slices for features like tournaments/scrimmages.

**Key Configuration**:
- <TargetFramework>net9.0</TargetFramework>: Modern .NET for async/nullable.
- <LangVersion>13.0</LangVersion>: C# 13 features (primary constructors).
- <ImplicitUsings>enable</ImplicitUsings>: Auto-imports System, etc.
- <Nullable>enable</Nullable>: Null safety.

**PackageReferences**:
- Microsoft.EntityFrameworkCore 9.0.0: ORM for PostgreSQL.
- Npgsql.EntityValidation 9.0.0: Postgres driver.
- Microsoft.Extensions.Configuration.* 9.0.0: Config for appsettings.json, env vars.
- xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1: Testing (embedded).

**ProjectReferences**:
- WabbitBot.Common: Shared contracts.
- WabbitBot.SourceGenerators (OutputItemType="Analyzer"): Compile-time gen for events/DbContext.
- WabbitBot.Analyzers (OutputItemType="Analyzer"): Custom rules.

**Why This Way?**: Core is the heart—net9.0 for performance (EF Core async). Generators/analyzers referenced as analyzers (no runtime dep, compile-time only). No DI (static services).

### WabbitBot.DiscBot.csproj (net9.0)
**Purpose**: Discord bot integration (commands, interactions, modals for scrimmages).

**Key Configuration**:
- <TargetFramework>net9.0</TargetFramework>: Matches Core.
- <LangVersion>13.0</LangVersion>: C# 13.
- <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>: Debug gen output.

**PackageReferences**:
- DSharpPlus 5.0.0-nightly-02551: Discord API (commands, interactions).
- DSharpPlus.Commands 5.0.0-nightly-02551: Slash commands.
- DSharpPlus.Interactivity 5.0.0-nightly-02551: Modals/dropdowns.

**ProjectReferences**:
- WabbitBot.Common: Events/interfaces.
- WabbitBot.Core: Business logic.
- Generators/Analyzers as analyzers.

**Why This Way?**: DiscBot is the UI layer—net9.0 for DSharpPlus async. Nightly for latest features (modals). References Core for events (e.g., RaiseEventAsync after command).

### WabbitBot.Common.csproj (net9.0)
**Purpose**: Shared contracts (IEvent, attributes like [EventBoundary], enums).

**Key Configuration**:
- <TargetFramework>net9.0</TargetFramework>: Shared across solution.
- <LangVersion>13.0</LangVersion>.
- <ImplicitUsings>enable</ImplicitUsings>.

**PackageReferences**:
- FluentValidation 12.0.0: Attribute validation.
- Npgsql 8.0.3: Database.
- Microsoft.Extensions.Configuration.* 9.0.0: Config.
- System.Text.Json 9.0.0: Serialization for events.

**ProjectReferences**:
- Analyzers as analyzers.

**Why This Way?**: Common is foundation—net9.0 for nullable/enums. Attributes for gen (e.g., [EntityMetadata]).

### WabbitBot.Common.Tests.csproj (net9.0)
**Purpose**: Tests for Common (attribute validation, event interfaces).

**Key Configuration**:
- <TargetFramework>net9.0</TargetFramework>.
- <IsPackable>false</IsPackable>: Test project.

**PackageReferences**:
- xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1: Testing.
- coverlet.collector 6.0.4: Coverage.

**ProjectReferences**:
- WabbitBot.Common.

**Why This Way?**: Standard test setup—net9.0 for async tests.

### WabbitBot.SourceGenerators.csproj (netstandard2.1)
**Purpose**: Source generators for events, DbContext, commands.

**Key Configuration**:
- <TargetFramework>netstandard2.1</TargetFramework>: Modern Roslyn hosting.
- <LangVersion>12.0</LangVersion>: C# 12 for gen.
- <IsRoslynComponent>true</IsRoslynComponent>: Analyzer component.
- <IncludeBuildOutput>false</IncludeBuildOutput>: No runtime DLL.

**PackageReferences**:
- Microsoft.CodeAnalysis.CSharp 4.13.0: Roslyn API.
- Microsoft.CodeAnalysis.Analyzers 3.11.0: Built-in rules.

**ProjectReferences**:
- WabbitBot.Analyzers: Custom rules.
- WabbitBot.Generator.Shared (OutputItemType="Analyzer"): Shared utils.

**Why This Way?**: netstandard2.1 for Roslyn compatibility and .NET 9 SDK integration. Generators run at compile-time, outputting code (e.g., event publishers).

### WabbitBot.Generator.Shared.csproj (netstandard2.1)
**Purpose**: Shared types for generators (enums, utils like AttributeExtractor).

**Key Configuration**:
- <TargetFramework>netstandard2.1</TargetFramework>: Shared with analyzers.
- <LangVersion>10.0</LangVersion>: Balanced modern features.
- <ImplicitUsings>enable</ImplicitUsings>.

**PackageReferences**:
- Microsoft.CodeAnalysis.CSharp 4.13.0: Gen API.
- Microsoft.CodeAnalysis.Common 4.13.0: Core Roslyn.

**Why This Way?**: netstandard2.1 for reuse and ref resolution. No runtime deps (PrivateAssets=all).

### WabbitBot.Analyzers.csproj (netstandard2.1)
**Purpose**: Custom analyzers (e.g., EntityMetadataAnalyzer for attribute validation).

**Key Configuration**:
- <TargetFramework>netstandard2.1</TargetFramework>.
- <LangVersion>12.0</LangVersion>.
- <IsRoslynComponent>true</IsRoslynComponent>.

**PackageReferences**:
- Microsoft.CodeAnalysis.CSharp 4.13.0.

**ProjectReferences**:
- WabbitBot.Generator.Shared (dual: OutputItemType="Analyzer" ReferenceOutputAssembly="false" for hosting; regular for compile-time types).

**Why This Way?**: netstandard2.1 for Roslyn and SDK support. Validates attributes at compile-time.

### Test Projects (e.g., WabbitBot.Analyzers.Tests.csproj, net9.0)
**Purpose**: Unit tests for generators/analyzers.

**Key Configuration**:
- <TargetFramework>net9.0</TargetFramework>.
- <IsPackable>false</IsPackable>.

**PackageReferences**:
- xunit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1: Testing.
- Microsoft.CodeAnalysis.Analyzer.Testing 1.1.2: Analyzer verification.
- Microsoft.CodeAnalysis.CSharp 4.13.0: Test Roslyn.

**ProjectReferences**:
- Parent project (e.g., WabbitBot.Analyzers).

**Why This Way?**: net9.0 for modern testing. Analyzer.Testing pulls legacy shims (NU1701), suppressed.

## PackageReference Configuration Details

### <IncludeAssets> and <PrivateAssets> Tags

These tags control how NuGet packages are consumed in your project, preventing unwanted propagation of dependencies and ensuring only necessary parts are used. They are crucial for test projects to avoid polluting the main solution's runtime.

- **<IncludeAssets>**: Specifies which assets from the package are included in the project. Categories include:
  - **runtime**: Runtime DLLs (e.g., xunit.dll for test execution).
  - **build**: Build-time tools (e.g., MSBuild tasks for test discovery).
  - **native**: Native libraries (rare in .NET).
  - **contentfiles**: Content files (e.g., docs, configs).
  - **analyzers**: Analyzer DLLs (e.g., for code analysis).
  - **buildtransitive**: Transitive assets from dependencies (included if the package uses them).

  In test projects like WabbitBot.Analyzers.Tests.csproj:
  - For xunit.runner.visualstudio:

    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>

    - Includes all for complete test support (runtime for running tests, build for discovery, analyzers for test-specific rules).
    - Ensures test runner works without pulling unnecessary runtime deps into the main bot.

  - For coverlet.collector:

    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>

    - Includes runtime for coverage collection during test execution, build for integration, analyzers for test code analysis.
    - Allows coverage data generation without affecting main project runtime.

  **Why This Way?**: Test packages are build-time only—include all categories to enable full functionality (discovery, execution, coverage) without leaking to production (e.g., Core/DiscBot).

- **<PrivateAssets>**: Specifies which assets are *excluded* from transitive propagation to dependent projects. Categories match IncludeAssets.

  In test projects:
  - `<PrivateAssets>all</PrivateAssets>` (for both xunit.runner.visualstudio and coverlet.collector):
    - Excludes *all* assets from being passed to dependents (e.g., Core won't see xunit DLLs at runtime).
    - Ensures tests are isolated—test deps don't bloat the bot's output (bin/deploy).
    - "all" means no transitive flow, keeping the solution clean (no test DLLs in production).

  **Why This Way?**: Tests should not affect main builds. PrivateAssets=all prevents test packages from becoming runtime deps, aligning with vertical slice isolation. Without it, xunit could leak to DiscBot, causing unnecessary bloat.

**Impact of Configuration**:
- Tests run locally (e.g., dotnet test) with full assets included.
- Main projects (Core/DiscBot) get clean refs—no test pollution.
- If omitted, transitive deps could cause version conflicts (e.g., xunit in bot runtime).

For more, see NuGet docs: https://learn.microsoft.com/en-us/nuget/concepts/package-reference-settings

## Transitive Reference Issues and Shims

- **What Are They?**: Packages indirectly pulled (e.g., Analyzer.Testing 1.1.2 pulls CodeAnalysis.Workspaces 1.0.1).
- **Shim Warnings (NU1701)**: Old packages use .NET Framework v4.x shims for compat with net9.0. Warnings: "restored using .NETFramework instead of net9.0".
  - **Cause**: Testing libs from 2023, pre-.NET 9.
  - **Impact**: None—shims are transparent.
  - **Fix**: Suppress with <NoWarn>NU1701</NoWarn> in csproj or settings.json:
    ```json
    {
      "omnisharp.disableMSBuildDiagnosticWarning": true
    }
    ```
- **NETSDK1023**: Explicit NETStandard.Library in netstandard2.0—SDK thinks redundant, but needed for refs. Suppress with <NoWarn>NETSDK1023</NoWarn>.

## Why the Configuration?

- **net9.0 for Runtime**: Modern features (async, nullable).
- **netstandard2.1 for Tools**: Roslyn requires compatibility, with 2.1 for better SDK integration.
- **PrivateAssets=all**: Tools don't leak runtime deps.
- **OutputItemType="Analyzer"**: References generators/analyzers at compile-time only.
- **LangVersion**: 13.0 for runtime, 12.0 for SourceGenerators/Analyzers, 10.0 for Generator.Shared (stable for gen).

### SDK Pinning with global.json

The `global.json` file at the solution root pins the .NET SDK version to ensure consistent builds across developer machines, CI/CD pipelines, and environments. This prevents issues from SDK mismatches, such as the NETSDK1045 error ("SDK does not support targeting .NET 9.0") that occurred during troubleshooting.

**Content**:
```json
{
  "sdk": {
    "version": "9.0.200",
    "rollForward": "latestMinor"
  }
}
```

- **version: "9.0.200"**: Explicitly uses SDK 9.0.200, which fully supports .NET 9.0 targets (net9.0) and netstandard2.1 for Roslyn components (Microsoft.CodeAnalysis 4.13.0+). This version resolves transient ref resolution quirks in earlier previews and ensures stable compilation for generators/analyzers.
- **rollForward: "latestMinor"**: Allows automatic upgrade to the latest patch/minor version within 9.0 (e.g., 9.0.300 if installed), but prevents jumping to major versions like 10.0. This balances consistency with security/bug fixes without breaking changes.

**Why This Helps**:
- **Locking for Consistency**: Teams often have different SDK installs; pinning avoids "works on my machine" issues, especially with netstandard2.1's SDK sensitivities (e.g., auto-facade inclusion for IEquatable).
- **Build Reliability**: Fixes errors from SDK drift (e.g., older SDKs failing net9.0 targets or Roslyn shims). Ensures `dotnet build` behaves identically everywhere.
- **Roslyn/Tools Support**: 9.0.200+ handles netstandard2.1 hosting without the obj/Debug ref failures seen in 8.0.x.
- **Best Practice**: Place at repo root; run `dotnet --version` to verify (should match 9.0.200+). If updating, test builds first to catch incompatibilities.

This setup ensures compile-time gen/validation without runtime overhead, with shims suppressed for clean builds.

For updates, monitor NuGet for Analyzer.Testing 1.2.0+ (fixes shims). Questions? Check AGENTS.md for troubleshooting.
