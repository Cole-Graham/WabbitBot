# AGENTS.md

## Purpose
- Central contract for coordinating autonomous helpers inside WabbitBot
- Clarifies shared goals, guardrails, and handoffs before execution starts
- Keeps agent behavior consistent across refactors, rewrites, and triage sessions
- Project status: initial development with no deployments; favor complete refactors over preserving legacy
  systems

## Technology Stack
- Language: C# 13.0 targeting .NET 9.0
- ORM: EF Core 9.0.0 backed by PostgreSQL through Npgsql 8.0.3
- Discord: DSharpPlus 5.0 powering bot connectivity and command routing
- Tooling: Source generators packaged in WabbitBot.SourceGenerators for compile-time glue

## Architecture Overview
- Projects: WabbitBot.Common, WabbitBot.Core, WabbitBot.DiscBot, WabbitBot.SourceGenerators
- WabbitBot.Common: shared contracts and infrastructure; establishes database foundations reused by Core
  and DiscBot
- WabbitBot.Core: vertical slice business logic with a Common directory for cross-slice utilities and
  services
- WabbitBot.DiscBot: platform wiring split between DiscBot (library-agnostic behaviors) and DSharpPlus
  (library-specific implementations)
- WabbitBot.SourceGenerators: compile-time generators for event bindings, command registration, and embed
  styling metadata
- Design restriction: runtime dependency injection is disallowed; consider compile-time DI only after
  exhausting simpler options
- Event classes carry primitive payloads, typically Guid identifiers, to keep the event bus isolated from
  feature concerns
- Service naming: only DatabaseService, FileSystemService, CoreService, and
  BotConfigurationService are valid; any other `Service` suffix flags legacy code pending refactor

## Event Wiring
- Event buses: GlobalEventBus orchestrates cross-project events; CoreEventBus scopes domain events inside
  WabbitBot.Core; DiscBotEventBus bridges Discord-centric workflows with automatic forwarding of global events
- Source generators emit subscription and publisher scaffolding, shared event class definitions, and
  Discord embed styling or command registration hooks to keep projects in sync
- Event types: Module Signals (intra-module), Integration Facts (cross-boundary), Async Queries (request-response),
  Lifecycle Broadcasts (system-wide), and Error Propagation (boundary faults)
- Events are immutable records implementing IEvent with EventId, Timestamp, and EventBusType metadata
- CRITICAL: Events are for communication only - NO database operations through events; CRUD handled via repositories/services

## Agent Catalog
### Primary Agent: Codex GPT-5
- Role: default engineering partner for architecture, data, and tooling updates
- Core strengths: deep refactor support, C# and .NET fluency, database design, documentation polish
- Risk posture: favors correctness and maintainability over speed; flag uncertainty instead of guessing
- Escalation triggers: conflicting instructions, destructive operations, missing domain context, or
  unexplained failing tests

### Reserved Slots
- Future specialized agents (security, monitoring, game balance) should register here with a short role
  summary

## Operating Principles
- Parse instructions in priority order: system > developer > user > repo rules > tool hints
- Capture assumptions and unresolved questions in the final response when they affect delivery
- Prefer iterative changes with explicit validation steps over large unverified rewrites
- Respect existing formatting and comment density unless the user instructs otherwise
- Keep traceability by referencing file paths and line numbers when discussing code

## Task Lifecycle
**Intake**
- Read the active files list and recent instructions before acting
- Confirm sandbox, approval, and network settings

**Plan**
- Use the planning tool for multi step work except when the task is trivially simple
- Update the plan after completing each major step

**Execute**
- Run commands with explicit working directories; avoid implicit cd usage
- Comment complex logic sparingly and only when it improves future readability

**Validate**
- Run or outline relevant tests; highlight gaps when validation is skipped
- Compare results against expectations and document notable diffs

**Deliver**
- Summarize changes succinctly, referencing files by path and line
- Provide actionable next steps or verification suggestions when appropriate

## Communication Standards
- Keep tone collaborative and concise; avoid filler language
- Use bullets for scan friendly updates; reserve code blocks for snippets that aid review
- Keep documentation lines at or below 120 characters
- Surface blockers or required approvals immediately with a proposed path forward
- Log noteworthy environment quirks or bugs for future sessions

## Tooling and Environment Notes
- Primary stack: .NET with C#, EF Core migrations, DSharpPlus Discord integrations
- Common commands: `dotnet build`, `dotnet test`, `dotnet ef migrations add`
- Repository includes PowerShell and Python helper scripts under `dev/tools`
- Be mindful of long running commands that may hold locks or mutate databases
- Keep code line lengths at or below 120 characters

## Testing Expectations
- Prefer targeted unit or integration tests near the change surface
- For schema updates, run EF Core migrations in a dry run or generate scripts before applying
- When tests are skipped due to time, document the rationale and suggested follow up

## Knowledge and Context
- Architecture references live under `docs/.dev/architecture`
- Refactor logs capture historical decisions in `docs/.dev/architecture/refactoring`
- Cross check `appsettings*.json` for environment specific toggles before modifying configuration

## Escalation and Safety
- Stop immediately if new unexpected repo changes appear during the session
- Defer to the user before deleting data, rewriting migrations, or changing production endpoints
- Note any potential data privacy concerns when dealing with user or match records

## Glossary
- WabbitBot Core: shared domain logic and services
- DiscBot: Discord front end built on DSharpPlus
- Proven Potential: scrimmage rating computation subsystem
- Leaderboard Service: aggregates standings across seasons and tournaments
- Event System: Immutable event-driven communication using tiered buses (Core, DiscBot, Global) with source generation
- IEvent: Interface for all events with EventId, Timestamp, and EventBusType metadata
- EventBoundary: Attribute marking classes for automatic event record and publisher generation
- EventBusType: Enum specifying event routing (Core=internal, DiscBot=internal, Global=cross-boundary)
- Request-Response: Async query pattern using event correlation via EventId and timeouts

