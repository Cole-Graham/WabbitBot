#### Step 6.8: Tests and Validation ⏳ IN PROGRESS

### Goal
Add comprehensive tests across generators, analyzers, EF Core integration, and event routing to validate the
generated architecture and prevent regressions.

#### 6.8a. Generator Snapshot Tests (WabbitBot.SourceGenerators.Tests)
- Add snapshot tests for DbContext, EntityConfigFactory, and DatabaseService generators
- Verify emitted code compiles and matches expected structure

#### 6.8b. Analyzer Tests (WabbitBot.Analyzers.Tests) — ✅ COMPLETED
- Add tests for release tracking, descriptors, and rule enforcement (WB001–WB006)
- Ensure RS2007/RS2008 remain clean via AdditionalFiles tracking

#### 6.8c. DbContext Integration + Performance — ⏳ IN PROGRESS
- CRUD roundtrip tests for representative entities, including JSONB serialization — ✅ DONE (Core.Tests: Player CRUD; Game + StateHistory; uuid[]/text[])
- Performance baseline tests for common queries; confirm indexes are effective — ⏳ PENDING

#### 6.8d. Event Integration — ✅ COMPLETED
- Cross-bus forwarding tests (Core → Global)
- Request/response correlation tests with timeouts

### Deliverables
- Green test suites across projects; CI step to run all tests
- Baseline performance results documented

