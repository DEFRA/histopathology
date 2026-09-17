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
| `src/Histo.Core/Domain/GroupAreaMappingHelpers.cs` (new) | New whitelist: `Customer` → External Customer/TB Diagnostics; `Histopathology User` → Histopath; `Maintenance` → Other VLA. Fails closed on unknown group/area. |
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
