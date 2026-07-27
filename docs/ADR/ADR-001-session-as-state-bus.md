# ADR-001: Session as Cross-Page State Bus

**Date:** Discovered 2026-07-27  
**Status:** Implicit — no record of an explicit decision  
**Source:** `HistopathologySystem/SessionVars.vb`, all ASPX code-behind files

---

## Context

The application needs to pass complex objects (DataTables, DataSets, DataViews, entity IDs, workflow flags) between multiple pages in multi-step workflows such as creating a submission, adding samples, and copying batches. There is no URL parameter mechanism, no hidden-field mechanism, and no client-side state.

## Decision

Use `System.Web.SessionState.HttpSessionState` (InProc, 90-minute timeout) as the primary state-passing mechanism between all pages. All session keys are defined as `Public Const String` values in a single file (`SessionVars.vb`). Pages read and write session directly using `Session.Item(SessionVars.SV_*)` string indexers.

## Consequences

**Positive:**
- Simple to implement per page; no additional infrastructure
- Avoids large query-string payloads for complex objects
- Centralised key registry (`SessionVars.vb`) prevents some typo errors

**Negative:**
- Session is InProc — not shared across IIS instances; incompatible with horizontal scaling or Azure App Service multi-instance deployment
- Session loss on worker recycle loses all in-progress form data silently
- DataTable/DataSet in session holds live schema metadata; session size can become significant for complex batches
- All 64 pages coupled to the same string-keyed session bag — refactoring any key name requires multi-file edits
- No type safety — session values cast with `CType(Session.Item(...), DataTable)` — wrong cast produces runtime exception not compile error
