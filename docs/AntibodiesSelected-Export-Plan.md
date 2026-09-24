# Plan: Add "Antibodies Selected" and "Test Type" columns to Excel export

**Status:** Verified against real DB schema. NOT YET IMPLEMENTED — implementation plan only.

**Estimated duration:** ~2–2.5 hours total (both columns, same migration/build/test pass)
| Step | Task | Estimate |
|------|------|----------|
| 1 | Write & deploy SP migration script (both new columns), verify manually via sqlcmd/SSMS | 40–50 min |
| 2 | `BatchSearchResult` model + `BatchRepository.MapSearchResult` mapping (both fields) | 15 min |
| 3 | `OnPostExportExcelAsync` changes in `SearchSubmissions.cshtml.cs` + `ViewSubmissions.cshtml.cs` | 20–25 min |
| 4 | Update `ViewSubmissionsModelTests` (16→18 columns) | 15 min |
| 5 | Build/test run + manual Excel export spot check against known batches | 20 min |
| 6 | Update `run-log-v2.md` / `session-metrics.md` | 5–10 min |

**Business need:**
1. View Submissions / Search Submissions Excel export should include the antibodies selected on the
   batch (Submission Details page).
2. Export should also include a "Test Type" column showing the submission type selected at creation
   time — one of: Pre Cassetted Tissue, Stained Section, Unstained Section, Wax Block, Wet Tissue.
   This is a single, fixed-at-creation value (the `Cassetted.cshtml` creation screen binds it as one
   selection, and Edit Submission renders it read-only/disabled) — distinct from the existing
   "Batch Type" (TSE/NonTSE) column already in the export.

Scope is the Excel export only — the on-screen grid is unaffected. Source data for both columns comes
from the same stored procedure already used for the export button: `GetSearchBatchDetails`.

---

## Verified DB facts (confirmed via live sqlcmd/sp_helptext this session)

- `dbo.BatchAntibodies` (`ID` int, `BatchID` int, `Code` varchar) — batch-level antibody selections.
- `dbo.luTSEAntibodies` (`Code` varchar(10), `Description` varchar(50), `IsActive` bit).
- `dbo.luNonTSEAntibodies` (`Code` varchar(10), `Description` varchar(50), `IsActive` bit).
- `Batch.BatchType`: `0` = TSE, `1` = NonTSE.
- Confirmed via a `CROSS APPLY` test against `BatchID = 32419`: `BatchAntibodies.Code` matches the
  lookup table's `Code` column (not `Description`). Example: `BA_Code = "Hantavirus"` →
  `luNonTSEAntibodies.Code = "Hantavirus"`, `Description = "Anti Hantavirus monoclonal A1C5 ab20035"`.
  This confirms the join must be on `Code`, and `Description` is the field to surface in the export.
- Codes are short alphanumeric strings and are **not guaranteed unique across the TSE/NonTSE tables**,
  so the join must be keyed by `Batch.BatchType` to avoid cross-table collisions.
- Deliberately **not** filtering by `IsActive` in the new subquery — an already-selected antibody must
  still display even if its lookup entry was later deactivated (matches the existing "resolve, don't
  filter, an already-stored selection" convention used elsewhere in this app).

### Test Type (`SubmittedAs`)

- `dbo.BatchSubmittedAs` (`BatchID` int, `Code` varchar(10)) — per-batch submission-type selections.
  Confirmed via `sp_helptext AddSubmittedAs`: `INSERT INTO BatchSubmittedAs (BatchID, Code) VALUES (...)`.
- `dbo.luSubmittedAs` (`Code` varchar(10), `Description` varchar(50), `IsActive` bit) — confirmed via
  `GetEditableLookupProcs @ID = 11` → `GetluSubmittedAs` → `sp_helptext` shows
  `SELECT Code, Description, IsActive FROM luSubmittedAs`.
- Unlike antibodies, this is a **single** lookup table — no TSE/NonTSE-style split, so no
  `BatchType`-keyed join is needed.
- In normal usage this is a **single value per batch**: `Cassetted.cshtml.cs` binds `SubmittedAs` as
  one `int?` selection (not a list), and `BatchDetails.cshtml.cs` calls `SaveSubmittedAsAsync` exactly
  once per batch with a single code — confirmed by reading both call sites directly. Edit Submission
  renders the field read-only/disabled, so it can never change after creation either. `STRING_AGG` is
  still used defensively in the SQL below (harmless — it just returns the one value when there's only
  one row), guarding against the rare/unexpected case of more than one row, which an existing report
  builder test (`HistologyReportDataSetBuilderTests.BuildBatchTable_MultipleSubmittedAsRows_ConcatenatesResolvedNames`)
  shows the codebase already handles gracefully — but this is not expected to occur in practice.
- Known `luSubmittedAs` codes (from repo memory, already DB-confirmed): 1=Wet Tissue, 2=Wax Block,
  3=Stained Section, 4=Unstained Section, 5=Pre Cassetted Tissue, 6=Fresh Frozen (inactive).
- This is **distinct from the existing "Batch Type" (TSE/NonTSE) column** already in the export —
  both will now appear as separate columns.

---

## SQL change — `GetSearchBatchDetails`

Add these two columns to **both** branches of the SP (`@All = 0` / TOP 200, and the `ELSE` branch),
immediately after `[Batch].[BatchStatus]` in each `SELECT` list. No other change to the SP —
`WHERE`/`JOIN`/params/`ORDER BY` are untouched, so this is a pure additive change with zero regression
risk to existing search behaviour.

```sql
,(
    SELECT STRING_AGG(lu.Description, ', ') WITHIN GROUP (ORDER BY lu.Description)
    FROM BatchAntibodies ba
    JOIN (
        SELECT Code, Description, 0 AS BatchType FROM luTSEAntibodies
        UNION ALL
        SELECT Code, Description, 1 AS BatchType FROM luNonTSEAntibodies
    ) lu ON lu.Code = ba.Code AND lu.BatchType = Batch.BatchType
    WHERE ba.BatchID = Batch.ID
) AS AntibodiesSelected
,(
    SELECT STRING_AGG(lu2.Description, ', ') WITHIN GROUP (ORDER BY lu2.Description)
    FROM BatchSubmittedAs bsa
    JOIN luSubmittedAs lu2 ON lu2.Code = bsa.Code
    WHERE bsa.BatchID = Batch.ID
) AS TestTypeSelected
```

Save as a new migration file:
`src/Database/Migrations/V20260922_01_Add_AntibodiesSelected_And_TestType_To_GetSearchBatchDetails.sql`
— a full `ALTER PROCEDURE` with the existing body plus the two new columns (SQL Server has no way to
add output columns without redeploying the full proc body).

---

## Code changes

1. **`src/Histo.Submissions/Models/SearchModels.cs`** — add to `BatchSearchResult`:
   - `public string? AntibodiesSelected { get; init; }`
   - `public string? TestTypeSelected { get; init; }` (named to avoid collision with the unrelated
     `AnimalTissueSearchResult.SubmittedAs` property already in this file)
2. **`src/Histo.Submissions/Repositories/BatchRepository.cs`** — in `MapSearchResult(dynamic row)`, map
   both via the existing `Str()` helper: `AntibodiesSelected = Str(dict, "AntibodiesSelected")`,
   `TestTypeSelected = Str(dict, "TestTypeSelected")`.
3. **`src/Histo.Web/Pages/Search/SearchSubmissions.cshtml.cs`** — in `OnPostExportExcelAsync()`, append a
   17th header `"Antibodies Selected"` / `r.AntibodiesSelected` and an 18th header `"Test Type"` /
   `r.TestTypeSelected`, both after `"Completed Date"`.
4. **`src/Histo.Web/Pages/Submissions/ViewSubmissions.cshtml.cs`** — same 17th/18th column addition in
   its `OnPostExportExcelAsync()`.
5. **`tests/Histo.Tests/Unit/ViewSubmissionsModelTests.cs`** — extend
   `OnPostExportExcelAsync_ReproducesLegacy16ColumnExportTable` to assert 18 headers; add
   `AntibodiesSelected`/`TestTypeSelected` values to the test's `BatchSearchResult` fixture.

---

## Verification steps (once implemented)

1. Run the migration script against the local `histo_bacpack_03Aug2026` DB.
2. `dotnet build --nologo` — 0 errors.
3. `dotnet test --nologo` — all pass, including the updated `ViewSubmissionsModelTests`.
4. Manual spot check: export Excel from ViewSubmissions/SearchSubmissions and confirm the two new
   columns show correct values matching Submission Details for a few known batches — including one
   batch with no antibodies selected (blank, not an error) and confirming Test Type shows exactly the
   single submission type recorded at creation for a few known batches.
5. Update `docs/run-log-v2.md` and `docs/session-metrics.md` with the completed run, per the
   established session documentation pattern.
