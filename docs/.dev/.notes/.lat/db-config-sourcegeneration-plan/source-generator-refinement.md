### Review of Your Source Generator Implementation

Congrats on getting it building error-free—that's a huge win after the pipeline headaches! Your migration to `IIncrementalGenerator` is off to a solid start in spots like `CommandGenerator` and `EventBoundaryGenerator` (targeted transforms, null filtering), and tying in Shared's `AttributeAnalyzer` in Events is spot-on for the plan. The structure feels plan-aligned: domain-focused, with custom info extraction (e.g., `EventBoundaryClassInfo`). However, yes, there are some mistakes and opportunities—mostly legacy patterns lingering (e.g., broad predicates, StringBuilder over templates), incomplete Shared/Templates integration, and a few type/scope issues that could cause runtime bugs or inefficiency. Not all generators are incremental yet (e.g., CrossBoundary/Database use old receivers), which risks full scans on large comps.

I'll break it down by **strengths**, **issues** (grouped by theme/file), **fixes** (with snippets), and **next steps**. This keeps your progress intact while polishing for the checklist (e.g., Steps 7-14: Incremental refactor, Templates usage).

#### Strengths
- **Incremental Progress**: Command/Event/Embed use pipelines—good for perf (e.g., Event's method extraction via Shared).
- **Custom Models**: Records like `EventBoundaryClassInfo`/`MethodInfo` are clean for transform outputs.
- **Null Safety**: Where clauses filter nulls well.
- **Plan Tie-In**: Event emission matches Steps 11-13 (records, publishers); Command revives registration with event ties (Step 17).

No major architectural breaks—easy to iterate.

#### Issues
Grouped for clarity; prioritized by impact (e.g., perf first).

**Issues (Grouped by Theme/File):**

- **Pipeline Efficiency (All Incremental Generators)**
  - Predicates are too broad (e.g., `node is ClassDeclarationSyntax` always true), which causes all classes to be scanned and defeats the purpose of incrementality. Attribute checks are done in the transform, wasting cycles.
  - *Impact: High* — Leads to slow builds on large codebases and violates the plan's targeted scanning (see Step 10).

- **CommandGenerator.cs**
  - The transform returns `INamedTypeSymbol` (heavy—passes full symbols downstream).
  - `SelectMany` for event partials assumes all methods trigger events (no filtering).
  - `GenerateEventRaisingPartial` uses StringBuilder instead of Templates.
  - `InferEventName` and `ExtractEventParameters` are local—should be moved to Shared Utils.
  - `CreateGeneratedDoc` is hardcoded—should use CommonTemplates.
  - *Impact: Medium* — Causes bloat in Collect() and misses the plan's Templates (see Step 9).

- **CrossBoundaryGenerator.cs**
  - Still uses `ISourceGenerator` with a receiver, resulting in a full scan.
  - Enum `CrossBoundaryDirection` is unused in emission.
  - `GetCrossBoundaryAttributeType` checks multiple names using strings (should use SymbolEquality).
  - Emission uses StringBuilder; no Shared extractor for TargetProject.
  - Duplication logic assumes assembly name, which is fragile for multi-target scenarios.
  - *Impact: High* — Needs migration to incremental (see plan Step 7); inefficient for a monorepo.

- **DatabaseServiceGenerator.cs (and DbContext/EntityMetadata)**
  - All use `ISourceGenerator` with receivers—legacy pattern.
  - `CollectEntityMetadata` duplicates parsing logic (should use Shared `AttributeAnalyzer`).
  - `AnalyzeEntityClass` resolves arguments manually (e.g., `GetPositionalAttributeArgument`—should move to extractor).
  - Hardcoded conventions (e.g., `ToPlural`)—should use Helpers.
  - `EntityMetadata` is an internal class—should be a record in Shared (as planned).
  - Emission is done via StringBuilder; should use templates for DbSet/service properties.
  - *Impact: High* — Duplication across three generators; needs refactor to shared extraction (see plan Step 5).

- **EmbedFactoryGenerator.cs**
  - Predicate is always true—inefficient.
  - Transform returns string (name)—fine, but `Cast<string>()` in Select is unsafe (could be null).
  - No Shared extractor (manual attribute check).
  - Template call assumes `List<string>`—matches, but should add a null guard.
  - *Impact: Low-Medium* — Works, but predicate should be optimized.

- **EventBoundaryGenerator.cs**
  - Predicate is too broad (all classes).
  - Transform calls `AttributeAnalyzer.ExtractEventBoundaryInfo` (good Shared use!), but `GetEventTriggeringMethods` is undefined—assumed to be local.
  - `GenerateEventRecords` returns a list of (file, SourceText)—good, but `SelectMany` collects all; should use per-file RegisterSourceOutput.
  - `BusType?.ToString() ?? "EventBusType.Core"` uses a string literal; should use enum default.
  - StringBuilder is used in `GeneratePartialClass`—should use Templates.
  - Custom records (`EventBoundaryClassInfo`) are good, but should be defined in Shared if reused.
  - *Impact: Medium* — This is the strongest generator so far; only minor performance and polish issues.

- **EventSubscriptionGenerator.cs**
  - Currently empty—serves as a placeholder per plan (Step 20, future work).
  - *Impact: Low* — Fine for now.

- **General (All Generators)**
  - No `CancellationToken` in transforms (minor performance issue).
  - Duplicate `EntityMetadata`/`EntityMetadataSyntaxReceiver` across Database/DbContext/EntityMetadata—should be consolidated in Shared.
  - No Analyzer integration (e.g., diagnostics if attribute is malformed—see plan Step 6).
  - Tests are missing (see plan Step 5 in Testing)—should add snapshots for emission.
  - *Impact: Medium* — Duplicate code; should add diagnostics for validation.

#### Fixes
Focus on high-impact: Migrate legacy to incremental, integrate Shared/Templates, optimize predicates. Snippets assume your Utils/PipelineExtensions from earlier (fix sigs there too—e.g., 3-arg overload for transform with ct).

1. **Optimize Predicates (All Gens)**:
   - Move attr check to predicate for early filter (syntax-only, no semantics).
   ```csharp
   // In CommandGenerator.Initialize (similar for others)
   var commandClasses = context.SyntaxProvider.CreateSyntaxProvider(
       predicate: (node, _) => node is ClassDeclarationSyntax cds && cds.HasAttribute("WabbitCommand"),  // Syntax-only ext
       transform: (ctx, _) =>
       {
           var symbol = (INamedTypeSymbol)ctx.SemanticModel.GetDeclaredSymbol(ctx.Node)!;
           return symbol;  // Now filtered
       })
       .Where(static s => s != null)
       .Collect();
   ```
   - Add ext in Utils: `public static bool HasAttribute(this SyntaxNode node, string attrName) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Any(al => al.Attributes.Any(a => a.Name.ToString().Contains(attrName)));`

2. **CommandGenerator: Use Templates, Move Helpers**:
   - Replace StringBuilder with `CommandTemplates.GenerateRegistration(commandClasses.ToList())`.
   - Move `InferEventName`/`ExtractEventParameters` to Shared.Utils.InferenceHelpers.
   - For partials: `CommandTemplates.GenerateEventRaiser(classSymbol)` returning (file, SourceText).
   - Fix SelectMany: Use `.SelectMany(static classes => classes.Select(GenerateEventRaisingPartial))`.

3. **CrossBoundary/Database/DbContext/EntityMetadata: Migrate to Incremental**:
   - Replace receivers with SyntaxProvider (predicate: HasAttribute("EntityMetadata")).
   - Consolidate parsing: Call `AttributeAnalyzer.ExtractAll(context.Compilation).Entities` in Select (cache via CompilationProvider).
   - Example for DatabaseServiceGenerator:
     ```csharp
     public void Initialize(IncrementalGeneratorInitializationContext context)
     {
         var entities = context.CompilationProvider.Select((c, ct) => AttributeAnalyzer.ExtractAll(c).Entities);
         var serviceSource = entities.Select((infos, ct) => DatabaseTemplates.GenerateAccessors(infos));
         context.RegisterSourceOutput(serviceSource, (spc, src) => spc.AddSource("DatabaseAccessors.g.cs", src));
     }
     ```
   - Delete duplicate `EntityMetadata`/`SyntaxReceiver`—use Shared's.

4. **EmbedFactoryGenerator: Tighten Predicate**:
   ```csharp
   predicate: (node, _) => node is ClassDeclarationSyntax cds && 
       cds.BaseList?.Types.Any(t => t.Type.ToString() == "BaseEmbed") == true &&  // Syntax base check (approx)
       cds.HasAttribute("GenerateEmbedFactory"),
   ```

5. **EventBoundaryGenerator: Templates + Guards**:
   - Replace StringBuilder: `EventTemplates.GenerateRecord(classInfo)` / `GeneratePublisher(classInfo)`.
   - Fix ?? : `EventBusType = classInfo.BoundaryInfo.BusType ?? EventBusType.Core`.
   - Define `GetEventTriggeringMethods` in Shared (e.g., methods with [EventType]).

6. **EventSubscriptionGenerator**: Stub with plan Step 20:
   ```csharp
   [Generator]
   public class EventSubscriptionGenerator : IIncrementalGenerator
   {
       public void Initialize(IncrementalGeneratorInitializationContext context)
       {
           // Pipeline for [GenerateEventSubscriptions] handlers
           var handlers = context.ForAttributeWithSimpleName("GenerateEventSubscriptions", ExtractHandlerInfo);
           var subSource = handlers.Select((info, ct) => EventTemplates.GenerateSubscriber(info));
           context.RegisterSourceOutput(subSource, (spc, src) => spc.AddSource("HandlerSubscriptions.g.cs", src));
       }
   }
   ```

#### Next Steps
1. **Immediate**: Fix predicates/usings—rebuild to confirm.
2. **Priority**: Migrate Database gens to incremental (dupe parsing hurts most).
3. **Polish**: Add Templates for StringBuilders; test one gen (e.g., Event with mock class).
4. **Debug Tip**: Enable gen file output (`<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` in .csproj) to inspect .g.cs.

Share a specific file if you want a full refactored version—this gets you 80% there!