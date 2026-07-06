# Pandatech.EFCore.Audit

NuGet library (`Pandatech.EFCore.Audit`, package id `Pandatech.EFCore.Audit`) that adds an automatic
audit trail to Entity Framework Core 8+. It captures entity changes through EF Core interceptors and
hands them to a consumer the host app implements. Features: per-property transform/rename/ignore,
composite-key support, manual bulk auditing, transaction-aware publishing, and a row-level read
permission per entity.

## How it works

- **Registration:** `builder.AddAuditTrail<TConsumer>(assemblies)` scans the given assemblies for
  `AuditTrailConfigurator<TEntity>` subclasses, registers the consumer, the two interceptors, and the
  tracking service. `optionsBuilder.AddAuditTrailInterceptors(sp)` wires the interceptors into the
  DbContext.
- **Interceptors** (`src/EFCore.Audit/Interceptors`): `SaveChangesAuditorInterceptor` snapshots
  tracked entities on `SavingChanges`, then publishes on `SavedChanges` for non-transactional saves.
  `TransactionAuditorInterceptor` publishes on transaction commit instead, so a save inside a
  transaction is audited once, at commit.
- **`AuditTrailTrackingService`** holds per-DbContext state (keyed by `ContextId.InstanceId`),
  removes unchanged properties, applies configured transforms, and dispatches an
  `AuditTrailEventData` to the host's `IAuditTrailConsumer`.
- **Config model** (`src/EFCore.Audit/Configurator`, `Models`): `AuditTrailConfigurator<TEntity>`
  is the fluent base (`RuleFor`, `SetServiceName`, `SetReadPermission`, `WriteAuditTrailOnEvents`).

## Layout

- `src/EFCore.Audit` — the shipped library (multi-targets `net8.0;net9.0;net10.0`).
- `test/EFCore.Audit.Tests` — xUnit v3 test project (`OutputType=Exe`, single-target `net10.0`).
- `test/EFCore.Audit.Demo` — `Sdk.Web` sample app (Postgres); NOT shipped, NOT a test project.

## Rules

- **NEVER bump the trailing `.0` on the EF Core package versions.** The library pins
  `Microsoft.EntityFrameworkCore` / `.Relational` to the **floor** of each major
  (`8.0.0` / `9.0.0` / `10.0.0`) on purpose. As a library we only declare the *minimum* EF Core we
  support; the real version is resolved by the consuming application. Raising these floors forces
  every consumer to upgrade for no benefit. Every other `<PackageReference>` may be bumped to latest
  stable; these three-per-TFM EF Core refs are the deliberate exception. (Non-EF packages, e.g.
  analyzers, still follow the usual latest-stable bump.)
- Analyzers run in Rider only, not at build (`RunAnalyzersDuringBuild=false`); enforcement is Rider
  live analysis + Code Cleanup. `GenerateDocumentationFile=true`, so keep public APIs documented
  (0 CS1591) — end state is a 0-warning build.
- Single `SaveChangesAsync` per operation; the audit publish is driven by the interceptors, do not
  add extra saves.

## Build & test

```bash
dotnet build EFCore.Audit.slnx --configuration Release   # expect 0 warnings, 0 errors
dotnet test  EFCore.Audit.slnx --no-build --configuration Release
```

## Release

Bump `<Version>` + `<PackageReleaseNotes>` in `src/EFCore.Audit/EFCore.Audit.csproj`. Pushing to
`main` triggers `.github/workflows/main.yml`, which packs and publishes to NuGet. Dev branch is
`development`.
