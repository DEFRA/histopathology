# ADR-002: Stored Procedures as the Exclusive Data Access Pattern

**Date:** Discovered 2026-07-27  
**Status:** Implicit — established at application inception (circa 2003, per `clsDataAccess.vb` header)  
**Source:** `HistopathologySystem/DataAccessLib/clsDataAccess.vb`, all `HistopathologyLib` domain classes

---

## Context

The application requires data access to a Microsoft SQL Server database. A design choice was made at application inception about how application code should interact with the database.

## Decision

All database interactions use named stored procedures called via ADO.NET through the `DataAccess` / `TBCultureDA` wrapper chain. No inline SQL, no dynamic SQL concatenation, no ORM, no LINQ to SQL, no direct table access from application code.

Parameters are always passed as typed `ParameterList` objects using the `QuickAddInputParam` helper.

## Consequences

**Positive:**
- No SQL injection risk from application code — all queries are parameterised stored procedure calls
- Database schema and query logic are entirely encapsulated in the SQL Server database; the application only knows procedure names
- `DataAccess` is reusable across all domain classes via inheritance from `TBCultureDA`
- Consistent parameter handling via `ParameterList.QuickAddInputParam`

**Negative:**
- All business query logic lives in the database, not in source control with the application — no application-side tests for query correctness
- Schema changes require coordinated updates to stored procedures and application code
- DataSet/DataTable results are accessed by integer table index (constants in `clsBatch.vb`) — fragile when stored procedure result sets change
- Migration to cloud SQL (Azure SQL, managed identity) requires changes only to the connection string and auth mechanism; stored procedure names and signatures are unchanged
- No migration tooling — no Flyway, DbUp, or SQL project; schema is managed manually on the server
