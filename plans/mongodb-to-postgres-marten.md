# Change from MongoDB to Postgres/Marten

## Status
Completed

## Goal
Replace the existing `Bot.Database` MongoDB persistence implementation with PostgreSQL-backed Marten document storage while preserving the public database interfaces consumed by `Bot.Core` and other projects.

## Current State (Completed)
- `Bot.Database` now uses Marten/PostgreSQL persistence and no longer references `MongoDB.Driver`.
- `Bot.Main\Program.cs` continues to register database services through `Database.ServiceFactory.RegisterServices(sp)`, and `DatabaseFactory` now connects via Marten `IDocumentStore`.
- `DatabaseFactory` reads PostgreSQL connection settings and creates Marten-backed database services.
- Record classes are storage-neutral (`TownRecord`, `AnnouncementRecord`, `CommandMetricRecord`, `GameActivityRecord`, `GameMetricRecord`, `LookupRoleRecord`).
- `Test.Bot.Database` was updated and continues to pass.

## Target Design
- Use Marten as the document database layer over PostgreSQL.
- Prefer preserving `Bot.Api.Database` interfaces so most domain code remains unchanged.
- Replace Mongo-specific infrastructure with Marten infrastructure:
  - `IMongoClientFactory` -> Marten document store/session factory or provider.
  - `MongoClientFactory` -> `DocumentStore` creation/configuration.
  - `IMongoDatabase` constructor dependencies -> `IDocumentSession`/`IQuerySession` or a project-local abstraction around Marten sessions.
- Rename Mongo-specific records to storage-neutral names where practical, or introduce aliases during migration if minimizing churn is more important.
- Store the PostgreSQL connection string in configuration under `ConnectionStrings:Postgres` or `Database:ConnectionString` and keep secrets out of committed JSON.

## Implementation Steps
- [x] 1. Add package references to `Bot.Database` for Marten and PostgreSQL support, and remove `MongoDB.Driver` when no code depends on it.
- [x] 2. Introduce a Marten connection/provider abstraction in `Bot.Database` that owns `DocumentStore` creation and session lifetime.
- [x] 3. Update `DatabaseFactory` to read the PostgreSQL connection string from the app configuration/environment abstraction and create Marten-backed database services.
- [x] 4. Convert each Mongo record class to a Marten document record and rename to storage-neutral records:
  - `TownRecord`
  - `AnnouncementRecord`
  - `CommandMetricRecord`
  - `GameActivityRecord`
  - `GameMetricRecord`
  - `LookupRoleRecord`
- [x] 5. Configure Marten indexes for key query patterns:
  - Town lookup by `GuildId` and `ControlChannelId`.
  - Town lookup by `GuildId` and `DayCategory`.
  - Metrics lookup by day.
  - Role, announcement, and activity lookups by guild/channel identifiers.
- [x] 6. Rewrite each database implementation to use Marten sessions and LINQ queries instead of Mongo filters and collection operations.
- [x] 7. Preserve upsert/delete behavior in Marten-backed implementations.
- [x] 8. Update service registration in `Bot.Database\ServiceFactory.cs` to register Marten-specific factories/providers.
- [x] 9. Update tests in `Test.Bot.Database` for Marten-based wiring and behavior.
- [x] 10. ~~Add a one-time migration/export plan for existing Mongo data.~~ (Not needed for this setup: fresh clone with no existing database/data to migrate.)
- [x] 11. Run database tests and the full solution build.
- [x] 12. Remove unused Mongo classes, package references, environment variable names, and test helpers after parity is confirmed.

## Configuration Changes
- Replace `MONGO_CONNECT` and `MONGO_DB` with one of:
  - `ConnectionStrings:Postgres`
  - `Database:ConnectionString`
- For local development, store the PostgreSQL connection string in Visual Studio user secrets.
- For deployed environments, use environment variables with `__` for hierarchical keys, for example `ConnectionStrings__Postgres`.

## Validation Checklist
- All `Bot.Api.Database` interface tests pass.
- A town can be created, updated, queried by ID, queried by name, listed, and deleted.
- Metrics still aggregate by day.
- Lookup roles, announcements, and game activity persistence retain existing behavior.
- The app starts without MongoDB packages or Mongo environment variables.
- No secrets are committed.

## Risks and Open Questions
- Existing Mongo document IDs may not map directly to Marten identity conventions; identity strategy needs to be decided per document type.
- Marten session lifetime should be chosen carefully for a long-running Discord bot.
- A data migration tool may need to be a separate console command if production data must be migrated safely.
- Tests may require Docker/Testcontainers or an agreed local PostgreSQL instance.
