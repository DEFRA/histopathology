# ADR-005: Data Access Strategy — Retain Stored Procedures via Dapper

**Date:** 2026-07-27  
**Status:** Decided  
**Decision maker:** User (confirmed during `modernise-to-modular-monolith.agent` run)  
**Source:** Data Access Decision Gate in `modernise-to-modular-monolith.agent` (v1.1)

---

## Context

The legacy Histopathology System uses **100% stored procedures** as its data access pattern (confirmed in [ADR-002](ADR-002-stored-procedures-exclusive-data-access.md) and `docs/LLD.md` Section 2.1). All domain classes in `HistopathologyLib` inherit from `TBCultureDA`, which wraps `DataAccess.FillDataTable`, `ExecuteQuery`, `UpdateDataSet` etc.

The stored procedures reside in the `Histology` SQL Server database (`VM-APHADEV-003`). They are:
- Fully working in production
- Not under version control (remediated in Phase 0)
- Never modified by the application migration

The migration target is C# 14 / .NET 10. The legacy `DataAccess` / `TBCultureDA` / `ParameterList` chain is a custom ADO.NET abstraction written in 2003 and is not portable to .NET 10.

### Options considered

**Option A — Retain Stored Procedures via Dapper**  
Keep all stored procedures unchanged in the database. Replace the legacy `DataAccess`/`TBCultureDA` chain with Dapper as a thin modern wrapper. Map stored procedure result sets to typed POCO classes instead of DataSet/DataTable.

**Option B — Migrate to Entity Framework Core**  
Replace stored procedure calls with EF Core DbContext, LINQ queries, and EF migrations. Stored procedures would be retired incrementally.

---

## Decision

**Option A — Retain Stored Procedures via Dapper.**

**User's statement (2026-07-27):** *"The stored procedures are already available in the Database, just needs to be called from codebase."*

---

## Rationale

| Factor | Rationale |
|---|---|
| Zero database risk | No stored procedure is modified. The database is the stable anchor during a large application rewrite. Changing SPs alongside changing the application doubles the failure surface. |
| Fastest path to working system | Dapper calls stored procedures with two lines of code per SP. The mapping work is 1:1 with the existing `ParameterList` calls. |
| No schema ownership | The `Histology` database is a separate operational concern. Introducing EF migrations would require database DBA coordination for every schema change — not appropriate during an application-layer migration. |
| Existing SPs are parameterised | All SPs use typed parameters (confirmed via `ParameterList.QuickAddInputParam` — no dynamic SQL). Dapper's parameterised SP calling is a direct replacement. |
| POCO models are smaller scope | Replacing DataSet/DataTable with POCOs only requires defining the result columns — it does not require redesigning the database schema. |

---

## Implementation

**Wrapper library:** `Dapper` NuGet v2.x  
**Connection factory:** `Histo.Infrastructure::IDbConnectionFactory` / `SqlConnectionFactory`  
**Repository pattern:** One `I{Domain}Repository` interface + `{Domain}Repository : I{Domain}Repository` per module  
**Connection string:** Managed Identity (`Authentication=Active Directory Default`) — no SQL auth credentials

```csharp
// Standard Dapper SP call pattern used throughout all repositories
using var conn = _db.CreateConnection();
var result = await conn.QueryAsync<BatchPoco>(
    "GetCommonBatchDetails",
    new { ID = batchId },
    commandType: CommandType.StoredProcedure);
```

**Stored procedure source control:** All SPs extracted to `database/stored-procedures/*.sql` in Phase 0. No SP is modified during the migration.

---

## Consequences

**Positive:**
- No database change risk during the application migration
- Dapper is a mature, well-tested library with first-class .NET 10 support
- POCO models provide compile-time type safety vs the legacy DataTable column-name string indexing
- Repository interfaces enable mocking in unit tests (legacy `TBCultureDA` inheritance made mocking impossible)
- Stored procedure source now in version control (Phase 0 task)

**Negative:**
- Business logic remains partially split between the application layer (C# services) and the database layer (stored procedures) — this is accepted as the current state
- Any future stored procedure change requires DBA coordination + SQL script review
- Dapper does not provide automatic change tracking — all updates must call explicit update SPs (same pattern as today)

**Downstream impact:**
- `docs/Target-Architecture.md` — data access section reflects Dapper/SP pattern
- `docs/Migration-Plan.md` Phase 0 — includes SP source extraction to version control
- `docs/Migration-Plan.md` Phase 4 — repository implementations use Dapper, not EF
- All `Histo.*` domain modules implement `IXxxRepository` backed by Dapper
- `Histo.Infrastructure` project provides `IDbConnectionFactory`, not `DbContext`
