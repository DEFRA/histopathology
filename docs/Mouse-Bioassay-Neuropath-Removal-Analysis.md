# Impact Analysis & Implementation Plan: Removing "Mouse Bioassay" and "Neuropath"

> Status: **Analysis only — not yet implemented.** Produced 2026-09-12 in response to a requirement to remove the Mouse Bioassay and Neuropath user areas from the application, restricting the system to 4 areas (External Customer, Histopath, Other VLA, TB Diagnostics) with a strict Group→Area mapping.

---

## 0. Critical scoping clarification needed before implementation

Two **separate, unrelated** concepts in this codebase both use the words "Neuropath"/"Mouse":

| Concept | What it is | Where it lives | Affected by this requirement? |
|---|---|---|---|
| **User Area** (`luUserArea`, lookup table 13) | The "area" a user/submission belongs to (External Customer, Histopath, Mouse Bioassay, Neuropath, Other VLA, TB Diagnostics) — drives Group→Area role mapping, batch ownership/access, dropdowns | `ISessionService.UserArea`, `User.AreaCode/AreaName`, `Batch.UserAreaCode`, table 13 across ~15 pages | **Yes — this is what the requirement describes** |
| **Histology Ref Type** (`HistologyRefTypeCode`) | A classification for booking/reserving histology reference *number ranges* (Neuropath <20000, AbattoirSurvey <30000, TBDiag <40000, GeneralPool <60000, MouseProjects <90000) | `HistologyRef.cs`, `HistologyRefService.BookCounterRangeAsync`, `BookHistologyRef`/`EditHistologyRef` pages, `AnimalHelpers.ComputePgAutoHistologyRef` (PG-number auto-reversal tied to `Session.UserArea == "Neuropath"`) | **Ambiguous — needs a decision** |
| **Historical ICC_Sub archive tables** (`ViewImportedData`) | Read-only legacy data-migration archive views literally named `Get2001NEUROSUB`, `Get2002MOUSESUB`, `Get2005TBDIAGSUB` etc. | `SubmissionRepository.ImportedDataProcs`, `Search/ViewImportedData` | **No — historical read-only archive, do not touch** |

**Recommendation:** treat this as Phase 1 = User Area only. The Histology Ref Type counters are a numbering-range convention, not a user/group permission — removing the *User Area* doesn't obligate removing the *ref-type counter*, since existing Neuropath-numbered refs (registers <20000) still exist in the DB and someone still needs to book into that range for legacy data continuity, even if no user's *Area* is called "Neuropath" anymore. **Confirmation required:** should Histology Ref Type "Neuropath"/"Mouse Projects" also be removed from the Booking module, or left as-is (a pure numbering-range artifact with no user-facing "area" implication)? The rest of this plan flags every place this ambiguity bites.

---

## 1. Impact Analysis

### 1.1 Current state (as implemented, confirmed by code read)
- **Groups** and **User Areas** are two *independent* DB-driven lookups (`GetluUserGroup` / `GetluUserArea`, table 13) — there is **no existing validation anywhere in the codebase** that restricts which Area a Group can have. `AddUser`/`EditUser` let an admin pick *any* Group + *any* Area combination today. This means:
  - The requirement "only allow Customer→{External Customer, TB Diagnostics}, Histopathology User→Histopath, VLA Maintenance→Other VLA" is **net-new business logic**, not a relaxation of existing rules.
  - There is currently no concept of a "VLA Maintenance" group name in code — the group is called `"Maintenance"` (`SessionService.IsMaintenance => GroupName == "Maintenance"`). Confirmation is needed on whether "VLA Maintenance" is a rename or just how the existing "Maintenance" group is being referred to — if it's a rename, that's an additional, separate change (session role checks, nav visibility, `BatchAccessDecision`, `Help/Index.cshtml`, DB group lookup row).
- Removing 2 of 6 Area values narrows every Area-populated dropdown across the app (all fed live from table 13 — no hardcoded C# list to edit).
- `Session.UserArea == "Neuropath"` is read directly in one business-logic branch (`AddSubmission.cshtml.cs`) to decide whether to auto-reverse a PG-format Sender Ref into a Histology Ref. If Neuropath is removed as an Area, this branch becomes permanently `false` (dead code) — `isNeuropath`/`ComputePgAutoHistologyRef` and its call chain become removable rather than merely dormant.
- `EditAnimalRef.cshtml.cs` currently hardcodes `isNeuropath: true` **unconditionally, regardless of the actual user's area** — a pre-existing latent bug that becomes moot (dead code) if Neuropath is removed, but should be flagged/fixed either way.
- Historical data: existing `Batch.UserAreaCode`/`User.AreaCode` rows already reference the Mouse Bioassay/Neuropath area codes. These cannot be silently orphaned — any submission or user record created under those areas must still resolve to a readable label for historical display (audit logs, "Entered area"/"Submitted area" summary rows, exports).

### 1.2 Business risk if done incorrectly
- Hard-deleting the lookup rows (rather than deactivating) will break every historical batch/user summary view that resolves `AreaCode` → name (`BatchSummaryDisplayResolver`, `UserMaintenance.ResolveAreaName`, audit log entries) — these currently show "—"/blank only when a code has no matching row.
- Adding the new Group→Area restriction without first auditing existing `tblUser` rows could lock out or silently corrupt data for any user whose current Group/Area combination doesn't match the new whitelist (e.g. an existing "Maintenance" user whose Area happens to be "Neuropath").
- `AnimalHelpers.ComputePgAutoHistologyRef` currently only ever returns non-null for the (soon-removed) Neuropath area — its 8 existing unit tests (`AnimalHelperTests.cs`) and the `SubmissionServiceAnimalTests.cs` Neuropath test become tests of dead code and should be reconciled with the removal, not just left in place asserting behaviour nobody can trigger anymore.

---

## 2. Files / Modules Likely Affected

| Layer | File | Why |
|---|---|---|
| **Reference data (DB)** | `luUserArea` table (via `GetluUserArea`/related SPs) | Source of truth for the 6→4 area list — must be changed at the DB, not in C# |
| Session/domain | `src/Histo.Web/Services/SessionService.cs`, `ISessionService.cs` | `UserArea`/`UserAreaID` unaffected structurally; `IsMaintenance` naming if "VLA Maintenance" is a rename |
| Domain helper | `src/Histo.Core/Domain/AnimalHelpers.cs` | `ComputePgAutoHistologyRef(senderRef, isNeuropath)` — becomes dead code if Neuropath area is gone |
| Domain helper | `src/Histo.Core/Domain/SenderRefHelpers.cs` | `IsPgNumber`/PG-number parsing is tied to the same Neuropath-only feature; review whether still needed |
| Service | `src/Histo.Submissions/Services/SubmissionService.cs`, `ISubmissionService.cs` | `AddAnimalAsync(..., bool isNeuropath, ...)` parameter |
| Page model | `src/Histo.Web/Pages/Submissions/AddSubmission.cshtml.cs` | `Session.UserArea == "Neuropath"` branch |
| Page model | `src/Histo.Web/Pages/Admin/EditAnimalRef.cshtml.cs` | Hardcoded `isNeuropath: true` (pre-existing bug, becomes dead/removable) |
| Page model | `src/Histo.Web/Pages/Bookings/BookBlockRef.cshtml.cs` | `isNeuropath: false` hardcoded — harmless but part of the same call chain to clean up |
| **New validation logic (doesn't exist yet)** | New `Histo.Core.Domain` helper, e.g. `GroupAreaMappingHelpers.IsAllowedCombination(groupName, areaName)` | Enforces the Group→Area whitelist — must be added |
| UI (Group/Area dropdowns) | `Admin/AddUser.cshtml(.cs)`, `Admin/EditUser.cshtml(.cs)` | Add server-side validation call + Area options filtered/validated per selected Group |
| UI (Area dropdowns, no Group context) | `Admin/AddLookupItem.cshtml(.cs)`, `Admin/EditLookupItem.cshtml(.cs)`, `Admin/LookupItems.cshtml.cs`, `Batches/BatchDetails.cshtml(.cs)`, `Batches/EditBatch.cshtml(.cs)`, `Batches/ReceiveBatch.cshtml(.cs)`, `Search/SearchSubmissions.cshtml(.cs)`, `BatchSummaryDisplayResolver.cs` | All populate an Area `<select>` from `GetUserAreasAsync()` — automatically shrinks to 4 once the DB lookup is updated; no code change needed here **unless** Group-conditional filtering is also wanted on these forms |
| Booking module (if Histology Ref Type scope is confirmed in-scope) | `Histo.Histology/Models/HistologyRef.cs`, `Histo.Histology/Services/HistologyRefService.cs`, `Web/Pages/Bookings/EditHistologyRef.cshtml`, `Web/Pages/Submissions/SubmissionDetailsBlock.cshtml.cs` | `HistologyRefTypeCode.Neuropath`/`MouseProjects`, dropdown `<option>`s, counter upper-bound switch |
| Docs (help/UI copy) | `src/Histo.Web/Pages/Help/Index.cshtml` (line 630) | Literal "External Customers, Histopath, Mouse Bioassay, Neuropath" text |
| Tests | `tests/Histo.Tests/Unit/AnimalHelperTests.cs`, `SubmissionServiceAnimalTests.cs`, `BookHistologyRefModelTests.cs` (if Ref Type in scope) | Rewrite/retire Neuropath-specific assertions |
| Docs (historical record, update not delete) | `docs/Parity-Audit-Report.md` (D-3, D-4, D-6, D-8), `docs/LLD.md`, `docs/Test-Strategy.md`, `docs/TSE-NonTSE-Submission-Workflow-Redesign.md`, `docs/Functionality-Traceability-Matrix.md` | Already document these as "being decommissioned" — should be updated to "decommissioned" once done |
| **Not affected — do not touch** | `Search/ViewImportedData.cshtml(.cs)`, `SubmissionRepository.ImportedDataProcs` | Historical read-only ICC_Sub archive views (`NEUROSUB`/`MOUSESUB` in SP names) — a different, historical-data concept, unrelated to live user/area configuration |

---

## 3. Required Code, Configuration, and Database Changes

1. **Database (reference data)** — in `luUserArea`: deactivate (`IsActive = 0`, do **not** hard-delete) the Mouse Bioassay and Neuropath rows, matching the app's existing `includeInactive` soft-delete convention (`ILookupService.GetLookupDataAsync(tableId, includeInactive:)`) already used for deactivated pick-list items elsewhere. Hard deletion would break FK-style code resolution for historical batches/users.
2. **Data reconciliation (pre-requisite, must happen before #3)** — audit `tblUser`/`Batch` rows currently set to Mouse Bioassay/Neuropath and confirm a remediation plan (reassign active users to a valid area; leave historical batch records as-is, resolved via the now-inactive-but-still-present lookup row).
3. **New Group→Area validation** — add a pure, unit-tested domain helper (e.g. `Histo.Core.Domain.GroupAreaMappingHelpers`) encoding the exact whitelist table from the requirement, and call it from `AddUserModel.Validate()`/`EditUserModel.Validate()` (currently only checks `GroupCode <= 0`/`AreaCode <= 0`, no cross-field rule).
4. **Remove the Neuropath PG-reversal feature** (only if the Area itself, not just the Histology-Ref-Type, is confirmed for removal): delete `AnimalHelpers.ComputePgAutoHistologyRef`, the `isNeuropath` parameter throughout `ISubmissionService.AddAnimalAsync`/`SubmissionService.AddAnimalAsync`, and its two call sites (`AddSubmission.cshtml.cs`, `EditAnimalRef.cshtml.cs`, `BookBlockRef.cshtml.cs`).
5. **Booking module** (only if Histology Ref Type is confirmed in scope): remove `HistologyRefTypeCode.Neuropath`/`MouseProjects`, their `<option>` entries on `EditHistologyRef.cshtml`/`BookHistologyRef.cshtml`, and their counter-range enforcement branch in `HistologyRefService.BookCounterRangeAsync`.
6. **Help text** — update the Area description list on `Help/Index.cshtml`.

## 4. Validation and Authorization Updates
- Add the new cross-field Group↔Area check (item 3 above) — this is the only actual *authorization-adjacent* rule change; no existing authentication/claims code needs to change (Group name checks like `IsHistoUser`/`IsMaintenance` are unaffected structurally).
- If "VLA Maintenance" is a genuine rename of "Maintenance", every `GroupName ==` comparison (`SessionService`, `BatchAccessDecision` callers, `_NavPartial.cshtml`, `Help/Index.cshtml.cs`) needs updating consistently, plus the DB group lookup row — this needs explicit confirmation, as it's a much larger blast radius than the Area removal alone.

## 5. Regression Test Scenarios / Acceptance Criteria
- Area dropdown on every affected page shows exactly 4 options: External Customer, Histopath, Other VLA, TB Diagnostics.
- `AddUser`/`EditUser`: selecting Customer + Histopath (an invalid combination) is rejected with a clear GDS error message; each of the 3 valid combinations succeeds.
- Existing users previously assigned Mouse Bioassay/Neuropath cannot be re-saved without first correcting their Area (or are migrated in advance per item 2).
- Historical batches/users created under the old areas still display a readable area name (not blank/"—") on `BatchDetails`, `EditBatch`, `ArchiveBlocks`/`ArchiveTissues`, audit log entries.
- `AnimalHelperTests`/`SubmissionServiceAnimalTests` updated to reflect removal (either deleted or asserting `ComputePgAutoHistologyRef` no longer exists/is never invoked) — no orphaned tests asserting dead behaviour.
- Full regression pass on `dotnet test` (currently 285 total) after removal — expect a net reduction in test count for the removed Neuropath-specific cases.

## 6. Risks, Dependencies, Migration Considerations
- **Highest risk: the Histology Ref Type scope ambiguity (§0)** — implementing this without confirmation could either under-deliver (leaving "Neuropath"/"Mouse Projects" visible in the Booking module) or over-deliver (breaking numbering-range continuity for pre-existing Neuropath/Mouse-numbered histology refs still in the database).
- **"Maintenance" vs "VLA Maintenance" naming** — needs explicit confirmation; if it's a rename, scope grows substantially (session/claims/nav/DB group row).
- Reference-data change (DB) is outside this app's own migrations/EF-less Dapper approach — must be coordinated with DBA/whoever owns the `luUserArea` seed data, not something this codebase can "migrate" itself.
- Existing production data reconciliation (users/batches currently on the areas being removed) must complete **before** the new Group→Area validation goes live, or valid existing accounts will be unable to save.

## 7. Open Questions (blocking implementation)

1. Is Histology Ref Type "Neuropath"/"Mouse Projects" (Booking module counters) in scope, or only the User Area?
2. Is "VLA Maintenance" a rename of the existing "Maintenance" group, or just descriptive phrasing for the same group?
3. What is the remediation plan for existing `tblUser`/`Batch` rows currently on the areas being removed?
