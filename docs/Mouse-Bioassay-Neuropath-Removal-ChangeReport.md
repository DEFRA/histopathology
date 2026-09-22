# Change Report — Remove Mouse Bioassay / Neuropath User Area

**Branch:** `feature/742956-remove-mouse-bioassay-neuropath-user-area`
**Commit:** `28400a3`
**Reference doc:** `docs/Mouse-Bioassay-Neuropath-Removal-Analysis.md` (Phase 1 scope — User Area only)

## 1. Summary

The "Mouse Bioassay" and "Neuropath" user areas are being retired. This change:
1. Deactivates the two lookup rows in the database (soft delete, not hard delete).
2. Removes the `isNeuropath` business logic that auto-reversed PG-number sender refs into histology refs for users in the Neuropath area.
3. Adds a new validation rule so a User's **Group** and **Area** selections must be a valid combination (previously unvalidated).
4. Updates the on-screen Help text that listed "Mouse Bioassay"/"Neuropath" as example areas.

No UI screens were removed. The **User Area** field itself is unchanged — only the pick-list options and the downstream behaviour tied to the Neuropath option are affected.

## 2. What changed, file by file

| File | Change |
|---|---|
| `V20260917_01_Deactivate_MouseBioassay_Neuropath_UserAreas.sql` (new) | Sets `IsActive = 0` for `luUserArea` rows named "Mouse Bioassay" and "Neuropath". Transactional, with a post-check that rolls back if the update didn't take. |
| `Deploy.sql` | Adds Step 5 to `:r`-include the new script during deployment. |
| `src/Histo.Core/Domain/AnimalHelpers.cs` | Removed `ComputePgAutoHistologyRef` / `IsAfterYear01` — the PG-number auto-reversal logic that only applied when a user's area was Neuropath. |
| `src/Histo.Core/Domain/GroupAreaMappingHelpers.cs` (new) | New validation: any known group may pair with any of the 4 active areas; unknown or retired group/area names fail closed. |
| `src/Histo.Submissions/Interfaces/ISubmissionService.cs`, `Services/SubmissionService.cs` | `AddAnimalAsync` no longer takes an `isNeuropath` parameter; animals are always created with `HistologyRef = null` / `IsPGNumber = false` at creation time. |
| `src/Histo.Web/Pages/Submissions/AddSubmission.cshtml.cs` | Removed the `Session.UserArea == "Neuropath"` branch that fed `isNeuropath` into `AddAnimalAsync`. |
| `src/Histo.Web/Pages/Admin/EditAnimalRef.cshtml.cs` | Removed the hardcoded `isNeuropath: true` PG-reversal check on Histology Ref rename; now goes straight to standard Histology Ref format validation. |
| `src/Histo.Web/Pages/Bookings/BookBlockRef.cshtml.cs` | Removed hardcoded `isNeuropath: false` argument (no behaviour change here — it was already always `false`). |
| `src/Histo.Web/Pages/Admin/AddUser.cshtml.cs`, `EditUser.cshtml.cs` | `Validate()` now calls `GroupAreaMappingHelpers.IsAllowedCombination(groupName, areaName)` and rejects invalid Group/Area combinations with error: *"The selected area is not valid for the selected group."* |
| `src/Histo.Web/Pages/Help/Index.cshtml` | Example area list updated from "External Customers, Histopath, Mouse Bioassay, Neuropath" → "External Customer, Histopath, TB Diagnostics, Other VLA". |
| Tests | `AnimalHelperTests.cs` deleted (tested removed dead code). `SubmissionServiceAnimalTests.cs` rewritten without Neuropath cases. `BookBlockRefModelTests.cs` updated call signatures. All 264 tests pass. |

## 3. Where to validate in the UI

### A. User maintenance — Group/Area validation (net-new behaviour)
- **Screen:** Admin → **Add User** (`/Admin/AddUser`) and **Edit User** (`/Admin/EditUser`)
- **Steps:**
  1. Open Add User (or Edit an existing user).
  2. Select **User Group** = `Customer`, then select **User Area** = `Histopath` (an area not in the Customer whitelist).
  3. Submit the form.
  4. **Expected:** Error message on the Area field: *"The selected area is not valid for the selected group."*
  5. Repeat with a valid combination, e.g. Group = `Customer`, Area = `External Customer` or `TB Diagnostics` — should save successfully.
  6. Valid combinations to confirm:
	 - `Customer` → `External Customer`, `TB Diagnostics`
	 - `Histopathology User` → `Histopath`
	 - `Maintenance` → `Other VLA`

### B. User Area pick-list — Mouse Bioassay / Neuropath no longer selectable
- **Screen:** Admin → **Add User** / **Edit User**, User Area dropdown
- **Steps:** Open the Area dropdown.
- **Expected:** "Mouse Bioassay" and "Neuropath" no longer appear as options (once the SQL deactivation script has been deployed to that environment's database).
- **Note:** This depends on the new SQL script having been run against the target DB — it won't show in the UI purely from the code deploy.

### C. Add Sample / Add Submission — PG-number auto-reversal removed
- **Screen:** Submissions → **Add sample** (`AddSubmission`)
- **Steps:** As a user previously assigned to the Neuropath area, add a sample with a Sender Ref in PG-number format (e.g. `PG012302`).
- **Expected (before):** Histology Ref was auto-populated with the reversed value (e.g. `02/00123`).
- **Expected (after):** Histology Ref is left blank/unset — no auto-reversal occurs for any user area.

### D. Edit Sender/Histology Ref — PG-reversal check removed
- **Screen:** Admin → **Edit Sender / Histology Reference** (`EditAnimalRef`)
- **Steps:** Enter a Sample Ref in PG-number format and try to set a Histology Ref.
- **Expected (before):** If the entered Histology Ref didn't match the auto-reversed PG format, an error was shown: *"The Histology Ref is not correct for the PG Number entered."*
- **Expected (after):** That specific error path is gone; standard Histology Ref format validation applies instead (*"You must enter a valid Histology Ref."* only if the format itself is invalid).

### E. Book Block References — no visible change
- **Screen:** Bookings → **Book block references** (`BookBlockRef`)
- **Expected:** No behavioural difference (the `isNeuropath` argument here was already hardcoded `false`).

### F. Help page — updated wording
- **Screen:** **Help** page, section "1. General information" → field glossary describing "Area"
- **Steps:** Navigate to Help, find the "Area" bullet.
- **Expected:** Now reads: *"denotes the customer area (for example, External Customer, Histopath, TB Diagnostics, Other VLA)"* instead of listing Mouse Bioassay/Neuropath.

## 4. Deployment note

The SQL script `V20260917_01_Deactivate_MouseBioassay_Neuropath_UserAreas.sql` must be run (via `Deploy.sql` or standalone) against each target environment's database before the "Mouse Bioassay"/"Neuropath" options will disappear from the Area dropdown in that environment. The code changes alone do not remove the DB rows.

## 5. Out of scope (not changed)

Per the analysis doc, the **Histology Ref Type / Booking module** (`HistologyRefTypeCode.Neuropath`/`MouseProjects`, `BookHistologyRef`, `EditHistologyRef`) was explicitly left untouched pending a separate decision — this is Phase 1 (User Area) only.

---

# Validation Review (2026-09-17)

> Everything below was produced by re-reading the actual implemented code against the analysis doc's requirements, the legacy `HistopathologySystem` VB.NET source, and the live domain models. **DB verification (row counts, live lookup contents, SP internals) could not be performed this session — the terminal tool was disabled** — every finding below is from static code/legacy-source analysis; anything needing a live-DB check is flagged explicitly.

## 6. Batch Table Impact Analysis

`Batch.SubmittedArea` ("Entered Area") and `Batch.OtherSubmittedArea` ("Submitted Area") are `varchar(10)` **codes** (the numeric `luUserArea.ID` as a string), not the literal area name — confirmed in [`src/Histo.Submissions/Models/Batch.cs`](../src/Histo.Submissions/Models/Batch.cs) lines 96–114. The deactivation script only touches `luUserArea.IsActive`; it does **not** touch `Batch.SubmittedArea`/`OtherSubmittedArea` on any existing row. So no existing Batch row is modified, deleted, or orphaned by this change — the FK-style code stays exactly as it was.

**However, resolving that code back to a readable name is broken for historical rows**, because of a gap the analysis doc predicted (§1.2, "Business risk") but that turns out to be worse than a simple soft-delete concern:

- `ILookupService.GetUserAreasAsync()` has **no `includeInactive` parameter at all** (unlike `GetLookupDataAsync(tableId, includeInactive: bool)`, which every other lookup — Projects, Contacts, Fixation — uses correctly). See [`ILookupRepository.cs`](../src/Histo.Administration/Interfaces/ILookupRepository.cs) lines 55–60 vs. lines 18–20.
- `LookupRepository.GetUserAreasAsync()` calls a single fixed SP, `GetluUserArea` — per this codebase's established naming convention (no file anywhere references a `GetluUserAreaAll` variant, unlike `GetProjects`/`GetProjectsAll`), this is almost certainly the **active-only** result set.
- Once Mouse Bioassay/Neuropath are deactivated, **every page that resolves `Batch.SubmittedArea`/`OtherSubmittedArea` to a name for a historical batch that used one of those two areas will fail to find a match**:
  - [`BatchSummaryDisplayResolver.cs`](../src/Histo.Web/Pages/BatchSummaryDisplayResolver.cs) (used by `QualityData`, `ArchiveBlocks`, `ArchiveTissues`) falls back to **the raw numeric code** (e.g. `"2"`) when the lookup misses (line ~57: `... ? ea : batch.SubmittedArea`).
  - [`BatchDetails.cshtml.cs`](../src/Histo.Web/Pages/Batches/BatchDetails.cshtml.cs) (`EnteredAreaName`/`SubmittedAreaName`) falls back to **`null`**, which the view then renders as **"Not recorded"** — actively misleading, since a real historical value exists, it just can't be resolved anymore.
  - [`EditBatch.cshtml.cs`](../src/Histo.Web/Pages/Batches/EditBatch.cshtml.cs) line 377 has the *same* exposure via a **different** code path: it calls `_lookups.GetLookupDataAsync(LookupUserArea)` **without** `includeInactive: true` (unlike its own Project/Contact dropdowns two lines above it, which correctly pass `includeInactive: true`) — so it independently reproduces the identical bug via a method that *could* have supported the fix but wasn't asked to.
  - [`ReceiveBatch.cshtml.cs`](../src/Histo.Web/Pages/Batches/ReceiveBatch.cshtml.cs) line 293 also calls `GetUserAreasAsync()` for the same display purpose.
- Net effect: **any Batch created while its Entered/Submitted Area was Mouse Bioassay or Neuropath will show either a raw numeric code or "Not recorded" instead of the area name**, on `BatchDetails`, `EditBatch`, `ReceiveBatch`, `ArchiveBlocks`, `ArchiveTissues`, and `QualityData` — inconsistent between pages, and both forms are non-compliant with the analysis doc's own stated acceptance criterion ("Historical batches ... still display a readable area name").

No impact on `Batch.Status`, workflow transitions, reporting totals, or any other Batch column — this is purely a **display/resolution** defect, not a data-loss or workflow-breaking one. `BatchSummaryDisplayResolver`/`BatchDetails`/`EditBatch`/`ReceiveBatch` all still load, search, filter, and process these batches normally; only the Area label is wrong.

## 7. SubmitterArea (Neuropath vs Bioassay) Assessment

| | Neuropath | Mouse Bioassay |
|---|---|---|
| Legacy area name literal | `"Neuropath"` | (not found as a literal string check anywhere in `HistopathologySystem/*.vb` — Mouse Bioassay appears to have carried no equivalent special-cased business logic, only plain lookup-list membership) |
| Live behaviour tied to the literal area name | Extensive — 12 legacy files check `Session.Item(SessionVars.SV_HeaderUserArea) = "Neuropath"` directly (see §9 below) | None found |
| C# migrated equivalent | `Session.UserArea == "Neuropath"` in `AddSubmission.cshtml.cs` (now removed) + hardcoded `isNeuropath: true` in `EditAnimalRef.cshtml.cs` (now removed) | None found — Mouse Bioassay was a plain pick-list entry with no special-cased code path in either the legacy or migrated app |
| Existing-data risk after deactivation | Historical `Batch`/`User` rows referencing Neuropath's numeric code lose name resolution (§6) | Same mechanism, same risk, for any historical `Batch`/`User` row referencing Mouse Bioassay's code |

**Conclusion:** Neuropath had materially more legacy business logic wired to it (PG-number auto-reversal, "daybook" import UI, sample-override controls, Histology Ref format ranges) than Mouse Bioassay, which appears to have been a plain, behaviourally-inert pick-list option. The removal correctly targeted all of Neuropath's *behavioural* dependencies. The **display-resolution regression in §6 affects both areas equally** — it's a property of "any deactivated `luUserArea` row", not something specific to Neuropath's extra logic.

## 8. `luHistologyRefType` Impact Analysis

**No table literally named `luHistologyRefType` was found anywhere** — not in the legacy `HistopathologySystem/*.vb` source, not in the migrated C# codebase, not in any `docs/*.md` file (searched all three; zero matches). The closest real concept is:

- Legacy: `Common.vb` — a hardcoded VB `Enum HistologyRefType` (`eNeuropath = 1`, plus AbattoirSurvey/TBDiagnostic/GeneralPool/MouseProjects/PG-number), **not** a database lookup table.
- Migrated: [`Histo.Histology/Models/HistologyRef.cs`](../src/Histo.Histology/Models/HistologyRef.cs) — a hardcoded C# static class `HistologyRefTypeCode` (`Neuropath = 1`, `AbattoirSurvey = 2`, `TBDiagnostic = 3`, `GeneralPool = 4`, `MouseProjects = 5`), explicitly documented as mirroring the legacy enum, not a DB-backed pick-list.
- The actual **counters** (next-available ref number per type) are read via SP `GetHistologyRefs` and written via SP `EditHistologyRef` (real params confirmed in an earlier session: `@Type`, `@NextHistologyRef`, `@RowStamp`) — these operate against a small counter table, but its real name was never established as `luHistologyRefType` in any evidence found.
- Unlike `luUserArea`/`luProjects`/`luContacts`, there is **no `PickListMaintenance`/`EditLookupItem` CRUD UI** for Histology Ref Types — the 5 types are compiled-in constants, not editable rows.

**Impact of this change: none.** No file touched by this change (`V20260917_01_...sql`, `GroupAreaMappingHelpers.cs`, `AnimalHelpers.cs`, `AddUser`/`EditUser`/`AddSubmission`/`EditAnimalRef`/`BookBlockRef` page models) references `HistologyRefTypeCode`, `HistologyRefService`, `BookHistologyRef.cshtml`, or `EditHistologyRef.cshtml` at all. This matches the analysis doc's own explicit Phase 1 scoping ("Histology Ref Type ... left untouched pending a separate decision").

**If `luHistologyRefType` is a real table that exists only at the database level** (not referenced by name in either codebase), that is outside what static code review can confirm or refute — a live schema query (`sys.tables`/`sys.columns`) would be needed, which was not possible this session (terminal disabled). Recommend a DBA/live-DB confirmation pass before signing this off as "zero impact" with full confidence.

## 9. Dependency Analysis — modules, DB objects, services, APIs, reports, UI screens

### Directly modified (per the implemented diff)
| Layer | Object | Nature of change |
|---|---|---|
| DB (reference data) | `luUserArea` rows "Mouse Bioassay"/"Neuropath" | Soft-deactivated (`IsActive = 0`) |
| Domain | `Histo.Core.Domain.AnimalHelpers` | `ComputePgAutoHistologyRef`/`IsAfterYear01` deleted |
| Domain (new) | `Histo.Core.Domain.GroupAreaMappingHelpers` | New Group→Area whitelist validator |
| Service | `ISubmissionService`/`SubmissionService.AddAnimalAsync` | `isNeuropath` parameter removed |
| Page model | `AddSubmission.cshtml.cs`, `EditAnimalRef.cshtml.cs`, `BookBlockRef.cshtml.cs` | Neuropath-conditional branches removed |
| Page model | `AddUser.cshtml.cs`, `EditUser.cshtml.cs` | New Group/Area cross-validation call |
| UI copy | `Help/Index.cshtml` | Area example list updated |
| Tests | `AnimalHelperTests.cs` (deleted), `SubmissionServiceAnimalTests.cs`, `BookBlockRefModelTests.cs` (rewritten) | Reconciled with removal |

### Confirmed dependent but NOT modified — every reference to `GetUserAreasAsync()`/User Area resolution
`UserMaintenance.cshtml.cs`, `AddLookupItem.cshtml.cs`, `EditLookupItem.cshtml.cs`, `LookupItems.cshtml.cs`, `BatchDetails.cshtml.cs`, `EditBatch.cshtml.cs`, `ReceiveBatch.cshtml.cs`, `BatchSummaryDisplayResolver.cs`, `SearchSubmissions.cshtml.cs` — none of these were changed, and per §6, several of them now have a display defect as an indirect consequence of the DB deactivation, without any code in them having been touched.

### Stale documentation reference (cosmetic, non-functional)
[`CopyBlocks.cshtml.cs`](../src/Histo.Web/Pages/Blocks/CopyBlocks.cshtml.cs) lines 18–23 — a doc comment pointing to `AnimalHelpers.ComputePgAutoHistologyRef` "for the equivalent logic used elsewhere" — that method no longer exists. This is a **comment only**, not a compiled reference, so it does not break the build, but it's now a dangling pointer to deleted code and should be updated or removed.

### Confirmed NOT dependent (legacy business logic that was never migrated, unaffected either way)
- `BatchDetails.aspx.vb::InitialiseNeuropathImport()` / the "daybook" integration hidden for non-Neuropath users, and the hardcoded `GetProjectsByArea("1")` "neuropath project list" (line 806) — no equivalent exists anywhere in the migrated app; this was already an accepted, pre-existing migration gap (per `docs/Parity-Audit-Report.md`), not something this change touches or regresses.
- `SubmissionDetails.aspx.vb`/`SubmissionDetailsBlock.aspx.vb`/`SenderRef.ascx.vb`/`BatchBlockSummary.aspx.vb` "neuropath stuff" branches (sample-override controls, PG "daybook" lookups, Histology Ref range validation messages) — these were already superseded by the migrated app's own (already-documented, pre-existing) simplifications, e.g. `SubmissionDetailsBlock.cshtml`'s PM Date/Histology Ref were deliberately made fully read-only in an earlier, unrelated change.
- `BookHistologyRef.aspx.vb`/`EditHistologyRef.aspx.vb`/`HistologyRefService.BookCounterRangeAsync` (§8) — confirmed untouched.

### Pre-existing discrepancy surfaced by this review (not introduced by this change)
Legacy `BookBlockRef.aspx.vb` line 240 and `EditHistologyRef.aspx.vb` line 145 both **hardcode `Session.Item(SessionVars.SV_HeaderUserArea) = "Neuropath"`** unconditionally on entry — i.e. legacy always treated *any* user reaching these two standalone booking screens as if their area were Neuropath, regardless of their real assigned area, so PG-number sender refs were always auto-reversed there. The migrated `BookBlockRef.cshtml.cs` hardcoded `isNeuropath: false` (the *opposite* assumption) even before this change — meaning the "no behavioural difference, it was already always false" claim in §2/§3E of this report is accurate **relative to the code as it stood immediately before this change**, but that prior code was itself already a divergence from legacy behaviour (a gap predating this Phase 1 change, not created by it). Flagging for awareness; no action taken as part of this change.

### Reports / exports
No report or export template references either literal area name directly (searched `Histo.Reporting`/report-related pages — no hits). Reports that surface Area (e.g. via `BatchSummaryDisplayResolver`) inherit the same §6 display defect, not a separate one.

## 10. GxP / GDS Compliance Assessment

| Concern | Assessment |
|---|---|
| **Data integrity** | Historical `Batch`/`User` rows are not altered or deleted (confirmed §6/§7) — the underlying codes are preserved. The regression is in **presentation** (name resolution), not the stored data itself. No risk of silent data corruption from the SQL script as written (it only sets `IsActive`, guarded by a transactional post-check that rolls back on failure). |
| **Audit trail** | No audit-log entry is written for the `luUserArea` deactivation itself (confirmed: no `AuditLog` reference anywhere under `Pages/Admin`) — if audit trail coverage for reference-data changes is a GxP requirement here, this is a gap, but it's consistent with how every other pick-list deactivation in this app already works (e.g. deactivating a Project/Pathologist via `EditLookupItem` is likewise not audit-logged) — not a new gap introduced specifically by this change. User Group/Area edits via `AddUser`/`EditUser` are also not audit-logged, same pre-existing pattern. |
| **Historical record retention** | Satisfied by design — soft delete (`IsActive = 0`), not hard delete, as required by the analysis doc. Historical Batch/User rows retain their original `SubmittedArea`/`OtherSubmittedArea`/`AreaCode` values; nothing is deleted. |
| **Validation requirements** | The new Group→Area whitelist (`GroupAreaMappingHelpers`) is unit-testable, pure, and fails closed (returns `false`/rejects on null or unrecognised input) — a positive control. However, see the **critical existing-user lockout defect** in §11 below: the validation was added without the "pre-requisite data reconciliation" step the analysis doc itself called out as required *before* turning validation on (§3 item 2 of the analysis doc: "audit `tblUser` rows ... before ... the new Group→Area validation goes live"). That prerequisite does not appear to have been done. |
| **Regulatory/compliance risk** | The combination of (a) existing users on now-invalid Area/Group combinations, (b) `EditUser`'s Area dropdown silently defaulting away from their true value (§11), and (c) the new validation blocking saves — creates a real risk that an admin attempting an unrelated, routine edit (e.g. correcting a user's name) could unknowingly and silently change that user's Area, or be blocked from saving at all, with no diagnostic explaining why. In a regulated environment this is a **data-integrity-adjacent risk** (an unintended, unlogged field change) that should be remediated before this change is considered complete, not just before its next unrelated edit. |

## 11. Risks, Recommendations, and Conclusion

### 🔴 Critical — existing users on Mouse Bioassay/Neuropath cannot be safely edited
Confirmed by direct code read of both `AddUser.cshtml.cs`/`EditUser.cshtml.cs`:
- `EditUserModel.OnGetAsync` sets `AreaCode = user.AreaCode` (their real, stored area code) but `Areas = await _lookups.GetUserAreasAsync()` returns **only the 4 active areas**. The `AreaSelectList` built from `Areas` with the stale `AreaCode` as the "selected" value has **no matching `<option>`**, so the browser silently defaults the rendered dropdown to whatever option happens to be first in the list.
- On **Save**, the posted `AreaCode` is therefore whatever that silently-defaulted option was — **not** the user's original area — meaning simply opening and re-saving an existing Neuropath/Mouse-Bioassay user (even to fix an unrelated field like Name or Email) can **silently reassign their Area** without the admin realising it.
- If that silently-defaulted area also fails the new `GroupAreaMappingHelpers.IsAllowedCombination` check for their Group, the save is blocked entirely with the generic message *"The selected area is not valid for the selected group"* — which does not explain that the real cause is "your original area was retired."
- This is exactly the risk the analysis doc's own §3 item 2 (data reconciliation) and §6 (regulatory risk) called out as a **pre-requisite that must happen before validation goes live** — it does not appear to have been completed.
- **Recommendation:** before deploying, either (a) run the data-reconciliation step first (reassign every existing user currently on Mouse Bioassay/Neuropath to a valid area explicitly, as a deliberate, logged action) — the analysis doc's own recommended order — or (b) make `EditUser`'s Area dropdown `includeInactive`-aware so a user's true (even if now-deactivated) area is preserved/visibly shown until explicitly changed, rather than being silently swapped.

### 🟠 High — historical Batch Area display regression (§6)
`GetUserAreasAsync()` has no `includeInactive` capability, and `EditBatch.cshtml.cs` doesn't pass `includeInactive: true` to the alternative lookup path either. Recommendation: add an `includeInactive` overload to `GetUserAreasAsync()` (mirroring `GetLookupDataAsync`'s existing pattern) and use it everywhere a *stored* Batch/User area code is being resolved for display (not everywhere a *new* selection is being made — dropdowns for new/edit selections should stay active-only).

### 🟡 Medium — no audit trail for the deactivation or for Group/Area edits
Pre-existing app-wide pattern (not unique to this change), but worth flagging given the GxP framing of this specific request — if reference-data changes need to be independently auditable going forward, this is the point to add it.

### 🟢 Low — stale doc-comment reference
`CopyBlocks.cshtml.cs` still references the deleted `AnimalHelpers.ComputePgAutoHistologyRef` in a comment. Cosmetic; update or remove.

### ✅ Confirmed correct / no action needed
- `luHistologyRefType`/`HistologyRefTypeCode` — genuinely untouched, zero impact (§8).
- `Batch`/`User` row data itself — not deleted or corrupted, only display-affected (§6).
- Soft-delete SQL script — transactional, self-verifying, matches the established codebase convention for lookup deactivation.
- `ISubmissionService.AddAnimalAsync` signature change — all call sites confirmed updated consistently; no orphaned callers.
- Help text update — confirmed applied exactly as described.

### Conclusion
The removal is **correctly scoped and cleanly implemented for the behavioural logic it targeted** (PG-number auto-reversal, Help text, the new Group/Area whitelist itself). At the time this section was first written it was **not yet safe to deploy as-is** for two reasons that were foreseeable from the analysis doc's own stated risks but were not fully closed out: (1) the existing-user data-reconciliation prerequisite, and (2) the Area-name display regression for historical Batch/User records. Both were display/edit-safety issues, not data-loss issues — the underlying data remained intact and recoverable in every case reviewed.

> **Update (2026-09-18): both issues have now been fixed and verified.** See §13 for the approach confirmation and the "Implementation status" note at the end of §13 for what was actually changed, file by file, and the test results.


## 12. End-to-End Test Flow (generated from `HistopathologySystem` legacy source + current code)

Legacy references below are exact file/line citations from `HistopathologySystem/*.vb`, gathered this session. Current-app references are the corresponding migrated page.

### Functional — new/expected behaviour

**F1. Group/Area validation blocks an invalid combination (Add User)**
- Page: `/Admin/AddUser`
- Steps: Group = `Customer`; Area = `Histopath`; Submit.
- Expected: Error on Area field — *"The selected area is not valid for the selected group."* No user created.
- Code ref: `AddUser.cshtml.cs::Validate()`, `GroupAreaMappingHelpers.IsAllowedCombination`.

**F2. Group/Area validation accepts each of the 3 valid combinations**
- Page: `/Admin/AddUser`
- Steps: repeat for `Customer`→`External Customer`, `Customer`→`TB Diagnostics`, `Histopathology User`→`Histopath`, `Maintenance`→`Other VLA`.
- Expected: user created successfully in each case.

**F3. Mouse Bioassay/Neuropath no longer appear in the Area dropdown (post-DB-deploy)**
- Pages: `/Admin/AddUser`, `/Admin/EditUser`
- Precondition: `V20260917_01_...sql` deployed to the target DB.
- Expected: Area `<select>` shows exactly 4 options.

**F4. PG-number Sender Ref no longer auto-reverses into a Histology Ref (Add sample)**
- Legacy ref: `AddSubmission.aspx.vb` lines 162, 187–201, 524–558 (`bNeuropath` branch feeding `objAnimal.NewRecord(..., bNeuropath)`); `AddSample.aspx.vb` lines 171–204, 423, 454, 566, 604 (identical pattern).
- Current page: `/Submissions/AddSubmission`
- Steps: Add a sample with Sender Ref `PG012302` (any user, any area — the area check no longer exists).
- Expected: Histology Ref is left blank; no auto-reversed value appears anywhere on the saved Animal record.
- Code ref: `AddSubmission.cshtml.cs`, `ISubmissionService.AddAnimalAsync` (no `isNeuropath` parameter).

**F5. Edit Sender/Histology Ref — PG-format cross-check removed**
- Legacy ref: none directly (this screen doesn't have a direct legacy 1:1 page — it's a consolidated admin utility) — logic ported from the same `NewRecord`/PG-reversal family.
- Current page: `/Admin/EditAnimalRef`
- Steps: Enter a Sample Ref in PG format; enter a Histology Ref that would previously have failed the PG-reversal cross-check.
- Expected: only standard Histology Ref format validation applies (*"You must enter a valid Histology Ref."*); the old PG-mismatch-specific error is gone.

### Regression — legacy features that must still work unchanged

**R1. Non-Neuropath areas were never affected by the old PG-reversal logic — confirm still true**
- Legacy ref: `AddSubmission.aspx.vb` line 187 (`If ... = "Neuropath" Then bNeuropath = True Else bNeuropath = False`) — every other area already produced `bNeuropath = False`.
- Steps: Add a sample as a `Histopath`/`TB Diagnostics`/`External Customer` user with a PG-format Sender Ref both before and after this change.
- Expected: identical behaviour (no auto-reversal) in both cases — confirms this change is additive-safe for non-Neuropath users.

**R2. Book Block References — behaviour unchanged (with the caveat in §9)**
- Legacy ref: `BookBlockRef.aspx.vb` line 240.
- Current page: `/Bookings/BookBlockRef`
- Steps: Book a range of block refs for a PG-format Sender Ref range.
- Expected: identical output before/after this change (both hardcode no PG-reversal) — note this does NOT match legacy's own forced-Neuropath behaviour on this specific screen (pre-existing gap, see §9).

**R3. All other Group/Area combinations for existing, untouched users remain saveable**
- Steps: Edit a user whose Group/Area is already one of the 4 valid combinations (e.g. `Maintenance`/`Other VLA`); change only their Name; save.
- Expected: succeeds normally, no new validation error triggered.

**R4. TSE/Non-TSE, Submission type, and all other BatchDetails/Cassetted fields unaffected**
- Steps: create a new submission end-to-end (any area) exactly as before.
- Expected: no change in behaviour — this change touches Area/Group and PG-reversal only.

### Existing-data — the critical scenario this review surfaced

**E1. Editing an existing user already on Mouse Bioassay or Neuropath**
- Precondition: at least one `tblUser` row exists with `AreaCode` pointing to Mouse Bioassay or Neuropath, and the DB deactivation script has been deployed.
- Page: `/Admin/EditUser` for that user.
- Steps: Open the Edit User page; do not touch the Area dropdown; change only the Name field; Save.
- **Expected per this review: FAILS or silently reassigns the Area** — see §11 Critical finding. This is the highest-priority scenario to execute in a real environment before sign-off.

**E2. Viewing an existing Batch entered under Mouse Bioassay/Neuropath**
- Precondition: at least one `Batch` row with `SubmittedArea`/`OtherSubmittedArea` = the deactivated area's code, DB script deployed.
- Pages: `/Batches/BatchDetails` (view mode), `/Batches/EditBatch`, `/Archive/ArchiveBlocks`, `/Archive/ArchiveTissues`, `/QC/QualityData`.
- Steps: Open each page for that batch.
- **Expected per this review:** `BatchDetails`/`EditBatch` show *"Not recorded"* for Entered/Submitted Area; `ArchiveBlocks`/`ArchiveTissues`/`QualityData` show the raw numeric code instead of a name. Neither matches the analysis doc's stated acceptance criterion.

**E3. User Maintenance list — existing users on the deactivated areas**
- Page: `/Admin/UserMaintenance`
- Steps: View the list including deactivated-area users (`ShowDeactivated` on if needed).
- Expected: `ResolveAreaName` falls back to the lookup dictionary (active-only) only if the `GetUsers` SP's own `AreaName` column is empty — needs live-DB confirmation of whether that SP's join already handles inactive rows (not verifiable this session).

### Negative scenarios

**N1. Attempt to create a user with an invalid Group/Area pair**
- Covered by F1.

**N2. Attempt to select a null/blank Group or Area**
- Steps: Submit Add User with Group or Area left as `-- Select --`.
- Expected: existing "Select a user group"/"Select a user area" required-field errors fire first (unchanged) — the new cross-field check only runs when both `GroupCode > 0 && AreaCode > 0` (confirmed in `Validate()`), so it does not mask the pre-existing required-field messages.

**N3. Attempt to enter a malformed Histology Ref after the PG-check removal**
- Page: `/Admin/EditAnimalRef`
- Steps: Enter a Histology Ref that fails standard format validation (not a PG-mismatch case, just structurally invalid).
- Expected: *"You must enter a valid Histology Ref."* still fires — confirms the removal only removed the PG-specific cross-check, not general format validation.

---

## 13. Approach Confirmation — Is the Recommended Fix the Best Approach?

> Added 2026-09-18, after confirming `GetluUserAreaAll` exists in the live database (removing the only open uncertainty from the plan below).

### Is it the best approach? Yes, with one refinement.

The core plan (add `includeInactive` to `GetUserAreasAsync`, use it only for *display* resolution, keep *selection* dropdowns active-only, and gate the Group/Area validation on whether Area actually changed) is now the **correct and lowest-risk approach**, because:

- It follows the codebase's own established, proven convention (`GetLookupDataAsync`'s `includeInactive` → `...All` SP pattern) rather than inventing a new mechanism.
- It requires zero schema changes and zero new SPs.
- It cleanly separates two different concerns that were previously conflated: "what can be *newly chosen*" (must stay active-only) vs. "what must still be *displayed/reported*" (needs inactive-inclusive).

### Remaining risk after this fix

| Risk | Level | Status after fix |
|---|---|---|
| Existing Neuropath/Bioassay users silently reassigned on unrelated edits | 🔴 Critical | **Resolved** — dropdown preserves their real value, validation only fires on an actual Area change |
| Historical Batch Area shows raw code / "Not recorded" | 🟠 High | **Resolved** — `BatchDetails`/`EditBatch`/`ReceiveBatch`/`ArchiveBlocks`/`ArchiveTissues`/`QualityData` all resolve the real name via `includeInactive: true` |
| Search/filter dropdowns hide Bioassay/Neuropath as filter criteria | 🟠 High *(new, per the audit-retention requirement)* | **Resolved** — `SearchSubmissions` filter switches to the inactive-inclusive call |
| No audit trail on the deactivation itself or on Group/Area edits | 🟡 Medium | **Not addressed** — pre-existing app-wide gap, out of scope unless separately requested |
| `CopyBlocks.cshtml.cs` stale comment referencing deleted method | 🟢 Low | **Not addressed** — cosmetic only |

No 🔴/🟠 items remain open after implementing the planned fix. The 🟡/🟢 items are pre-existing, low-stakes, and can be deferred.

### Recommended approach (final, as implemented)

1. `GetUserAreasAsync(bool includeInactive = false)` across interface/repository/service.
2. Switch `BatchSummaryDisplayResolver`, `BatchDetails.cshtml.cs`, `EditBatch.cshtml.cs`, `ReceiveBatch.cshtml.cs`, `UserMaintenance.cshtml.cs`, `SearchSubmissions.cshtml.cs` to `includeInactive: true` (display/filter — never blocks new assignment).
3. `EditUserModel`: inject the user's current (possibly-retired) area into the dropdown if missing from the active list; only run `GroupAreaMappingHelpers.IsAllowedCombination` when the posted Area actually differs from the original.
4. `AddUser.cshtml.cs`: no change (already correctly active-only).

### Implementation status — done (2026-09-18)

All four steps above have been implemented exactly as planned:

- `ILookupRepository`/`ILookupService`/`LookupRepository`/`LookupService.GetUserAreasAsync` now take `bool includeInactive = false`; the `true` path calls `GetluUserAreaAll` (confirmed to exist in the live DB), the `false` path is unchanged (`GetluUserArea`).
- `BatchSummaryDisplayResolver.cs`, `BatchDetails.cshtml.cs`, `EditBatch.cshtml.cs`, `ReceiveBatch.cshtml.cs`, `UserMaintenance.cshtml.cs`, `SearchSubmissions.cshtml.cs` all now call `GetUserAreasAsync(includeInactive: true)` for display/filter purposes.
- `EditUser.cshtml.cs`: added `EnsureCurrentAreaVisibleAsync` (called from both `OnGetAsync` and `OnPostAsync`), which appends the user's true stored Area to the `Areas` list if it's missing from the active-only set — so the dropdown/`SelectList` always has a matching `<option>` and never silently defaults to a different area. `Validate()` now tracks `_originalAreaCode` (re-fetched from the DB on `OnPostAsync`) and only runs `GroupAreaMappingHelpers.IsAllowedCombination` when the posted `AreaCode` differs from that original value — so re-saving an unrelated field (e.g. Name) for a user already on Mouse Bioassay/Neuropath no longer risks silent reassignment or a spurious validation block.
- `AddUser.cshtml.cs` confirmed unchanged, as planned — new users still can't be assigned to a retired area.
- Test mocks for `GetUserAreasAsync` updated across 6 unit test files to match the new two-parameter signature; `SearchValidationTests` updated to assert the new `GetUserAreasAsync(true, ...)` call instead of the old `GetLookupDataAsync(13, ...)` call.
- Full solution build and test run: **264 passed, 0 failed, 1 skipped** (the 1 skip is the pre-existing, unrelated integration test health-check gate).

No 🔴/🟠 risk items remain open. The change is ready to deploy for the Phase 1 (User Area) scope described in this report.

