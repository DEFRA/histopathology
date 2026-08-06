# Obtaining Test Profile IDs

> **Note:** This document is application-specific and must be populated by the
> team responsible for the target application before performing manual visual
> sign-off of migrated PDF reports against real data (the final post-pipeline step).
>
> It is not used by any agent during the automated pipeline run. No agent loads
> this file. It exists as a placeholder for the development team to fill in with
> the SQL queries appropriate to their application's database schema.
>
> Replace all content below the divider with application-specific guidance.

---

## Purpose

Describes how to obtain valid entity IDs for manually rendering and signing off
each migrated PDF report against real data, once the application compiles on .NET 10.

This supports the manual checklist items in Step 5 of the migration pipeline:

> *"Perform manual visual sign-off: render each report with real data and compare
> to the originals before go-live."*

It is not executed or referenced during the automated pipeline. No agent reads
this file.

---

## Prerequisites

- Read-only access to the target application's database
- Knowledge of the application's core entity schema (profile, version, status)

---

## How to Use

Replace the example queries below with the actual table and column names from
the target application. The goal is to obtain IDs that exercise the key rendering
paths for each report type:

1. A published entity ID — for testing normal (non-draft) report output
2. A draft entity ID — for testing the DRAFT watermark rendered by `_PdfLayout.cshtml`
3. Edge-case IDs, as appropriate to the report types in scope:
   - An entity with no optional sections populated (null-guard testing)
   - An entity with the maximum number of sections populated (layout stress test)
   - Two entity IDs for comparison reports (Archetype C)

---

## Example Queries

Replace these with the actual schema. The column and table names below are
illustrative only.

```sql
-- Example: retrieve a published entity version ID
SELECT TOP 1 Id
FROM EntityVersions
WHERE Status = 'Published'
ORDER BY UpdatedAt DESC;

-- Example: retrieve a draft entity version ID
SELECT TOP 1 Id
FROM EntityVersions
WHERE Status = 'Draft'
ORDER BY UpdatedAt DESC;

-- Example: retrieve two IDs for a comparison (Archetype C) report
SELECT TOP 2 Id
FROM EntityVersions
WHERE Status = 'Published'
ORDER BY UpdatedAt DESC;
```

---

## Contact

If you do not have database access, contact the application's data team or DBA.

---

*Created as a placeholder by the PDF migration pipeline. Populate with
application-specific content before performing post-migration manual sign-off.*
