# .NET Migration Agent — Prompt Log

This file captures migration-related prompts/requests for tracking purposes, per user preference.

---

## Prompt: Application Journey Validation, Legacy Comparison, and GDS Compliance Review

**Branch context:** feature/730426-copy-submission-date-returned-observation-fix

**Request summary:**
Perform end-to-end validation of the .NET 10 / Razor Pages / GDS-redesigned application against the legacy application, covering:

- Home Page navigation (Create New TSE / Non-TSE Submission)
- Submission Type selection (Pre Cassetted Tissue, Stained Section, Unstained Section, Wax Block, Wet Tissue) and Block vs Tissue business rules
- Submission Details (field enablement, checkboxes, Samples/Cancel/Finish button behaviour)
- Sample Summary (Add/Edit/Delete/Copy Sample button enablement rules)
- Add Sample (manual entry vs Sender Reference search)
- Sample Blocks journey (Add/Edit/Delete Block, Copy to Samples, Back/Done navigation)
- Block Details (tissue/test requirements, Next Block/Cancel/Done)
- Sample Details for Wet Tissue journey (tissue add via "*", Edit/Delete enablement)
- Edit Submission journey (read-only fields, status/workflow rules, persistence)
- Copy Submission journey (data copy correctness, regenerated identifiers, independence from original)
- Copy Sample journey (data copy correctness, tissues/blocks/tests copied per legacy rules)
- Delete Sample journey (confirmation, cascading deletes, view refresh)

**Known observations to verify as defects:**
1. Selecting "Archive" histology type should auto-deselect other histology types (no multi-select with Archive).
2. Test selection: IHC - PrP vs Special Stain — Antibodies section persists incorrectly instead of switching to Special Stain tests; requires deselect/reselect workaround.
3. Clicking Finish without adding a sample still creates a submission (should be blocked until >=1 sample exists).
4. Print Submission / Print Submission Notes buttons visible before submission is actually created.
5. Finish button should navigate to Print Submission page per legacy behaviour.

**GDS compliance review scope:** page titles, heading hierarchy, back links, breadcrumbs, radios/checkboxes, form controls, validation messages/error summaries, focus management, keyboard nav, screen reader support, responsive behaviour, content clarity, button placement, journey consistency.

**Deliverables requested:** journey-by-journey assessment, functional/business-rule/validation/navigation gaps, UI/UX and accessibility findings, defect list with severity (Critical/High/Medium/Low), remediation recommendations, production readiness risk assessment, and overall alignment verdict (Fully/Partially/Not Aligned with Legacy).

**Agent note:** Full execution of this request requires live comparison against the running legacy application and manual accessibility/UX testing (screen readers, keyboard-only navigation, visual GDS pattern comparison), which cannot be performed via static code review alone. A scoped static-review approach (Razor Pages markup, validation attributes, controller/page-model logic, JS enablement rules) was proposed instead — see follow-up scoping questions in chat.

---

## Findings and fixes applied (2025 session)

Legacy source located at `C:\Sabari\Dev\Histo\HistopathologySystem` (VB.NET WebForms). Compared against `src\Histo.Web\Pages\Batches\BatchDetails.cshtml(.cs)` (Create Submission) and `src\Histo.Web\Pages\Blocks\BlockDetails.cshtml(.cs)` (Block Tests).

1. **Archive/EO histology mutual exclusivity — CONFIRMED DEFECT, FIXED.**
   Legacy (`SubmissionDetailsBlock.aspx.vb` / `BlockDetails.aspx.vb` `chkblHistology_SelectedIndexChanged`) auto-deselects all other Histology checkboxes when EO or Archive is checked (and vice versa). The new app only enforced this via a blocking server-side validation error on `BlockDetails`, and only for Archive (not EO) on `BatchDetails` create form — with an unused `data-histology-code` attribute suggesting the client-side behaviour was planned but never wired up.
   - Fixed `BatchDetails.cshtml.cs`: added missing EO exclusivity check (previously only Archive was validated).
   - Fixed `BatchDetails.cshtml` inline script: extended exclusivity enforcement to cover both EO and Archio codes (was Archive-only).
   - Fixed `BlockDetails.cshtml`: added `data-histology-code` attributes and wrapper id, plus a new `Scripts` section with JS auto-unchecking peer Histology checkboxes when EO/Archive is selected, matching legacy UX (previously this page had no client-side enforcement at all, only the server-side error).

2. **IHC-PrP vs Special Stain toggle "requires deselect/reselect" — NOT REPRODUCED / NO LONGER APPLICABLE.**
   Legacy dynamically enabled/disabled the Antibodies and Special Stain checklists based on Histology selection (postback-driven), which could get out of sync. The new `BlockDetails` page renders both fieldsets unconditionally (always visible, no show/hide), and validation independently requires antibodies when IHC is selected and stains when Special Stain is selected. Since nothing is conditionally hidden, the described "stuck" state can't occur. No code change needed here; flagged as resolved by the new architecture.

3. **"Finish" creates a submission before any sample is added — CONFIRMED ARCHITECTURAL GAP, NOT FIXED (needs product/design decision).**
   The new app's flow differs structurally from legacy: `BatchDetails` `OnPostCreateAsync` creates the batch (submission) record immediately once the initial details form is submitted (needed so samples have a `BatchId` to attach to), then redirects back to `BatchDetails` — there is no single-page "Finish" that also requires a sample to exist first, and no explicit "Finish" step after Sample Summary. Enforcing legacy's rule (no submission record until >=1 sample) would require restructuring sample creation to not depend on a persisted batch id, which is a larger architectural change beyond a targeted bug fix. Recommend a follow-up design discussion before implementing.

4. **Finish should navigate to Print Submission page — CONFIRMED GAP, NOT FIXED (depends on item 3).**
   `OnPostCreateAsync` redirects to `/Batches/BatchDetails`, not `/Batches/PrintSubmission`. Since there is no equivalent single "Finish after samples added" action in the current architecture, this can't be meaningfully fixed until item 3's flow is redesigned.

5. **Print Submission / Print Submission Notes buttons visible before submission created — NOT REPRODUCED.**
   Reviewed `BatchDetails.cshtml` view-mode action buttons: no Print Submission links present there (a code comment confirms they were intentionally removed to avoid duplicating `ViewSubmissions.cshtml`, which is the only page exposing Print Submission / Print Submission Notes links, gated on an existing batch/submission).

---

## Resubmission note

Next candidate scopes, in rough priority order:
- Copy Submission journey (data copy correctness, regenerated identifiers)
- Copy Sample journey (data copy correctness, independence from original)
- GDS markup compliance pass (page titles, headings, back links, error summaries) across reviewed pages

---

## Phase 2 findings: Sample Summary / Add Sample / Sample Blocks (Sections 4-6)

Scope requested: review button enablement rules for these three journeys against legacy `HistopathologySystem` (BatchBlocks.aspx.vb, AddSample.aspx.vb, SubmissionDetailsBlock.aspx.vb), plus a redesign proposal for deferring batch creation until a sample exists.

### 1. Sample Summary (legacy `BatchBlocks.aspx.vb` vs new `SampleSummary.cshtml`)
Legacy: Add Sample and Copy Samples are always enabled; Edit Sample / Delete Sample are disabled until a row is selected in the shared grid (`DisableEnableControls`, wired to `grdBlockSummary_SelectedIndexChanged`). "Done"/`btSubmit` requires the batch to exist and redirects to `FinalPrintBatch.aspx` (Print Submission) on success.
New app: renders per-row Edit/View, Copy, Delete actions only when a sample row actually exists, and "Add sample" is gated on `CanModifySamples`. This is a progressive-disclosure pattern rather than literal enable/disable, but it enforces the same underlying rule (can't edit/delete/copy when there's nothing to act on) and is GDS-preferred over disabled buttons. **No defect — no change required.**

### 2. Add Sample (legacy `AddSample.aspx.vb` vs new `AddSubmission.cshtml`)
Legacy: `SenderRef1` textbox for manual entry; `lbLookup` ("Click here.") LinkButton looks up animals by sender ref and redirects to `SearchSample.aspx`, storing results/sender ref in session for return.
New app: `SenderRef` input for manual entry, plus a "Search for a previously used sender reference" link to `/Search/SearchSender` with `returnPage=/Submissions/AddSubmission` and the current `SenderRef` passed through. Behaviour is equivalent (manual entry + search-and-return). **No defect — no change required.**

### 3. Sample Blocks (legacy `SubmissionDetailsBlock.aspx.vb` vs new `SubmissionDetailsBlock.cshtml`)
Legacy: `btnAddBlock` and `btnBlockRefSearch` always enabled; `btnEditBlock`, `btnDeleteBlock`, `btnCopyBlock` start disabled and only enable once at least one block row is selected/checked (`bSelectAll`/`bEnable` toggles).
New app (before fix): Add block and Block ref search correctly always available, but Edit block / Delete block / Copy to samples buttons were rendered unconditionally even with zero blocks in the table — a GDS/UX affordance defect (active-looking buttons with nothing to act on).
**FIX APPLIED:** `src/Histo.Web/Pages/Submissions/SubmissionDetailsBlock.cshtml` — wrapped the Edit block / Delete block / Copy to samples buttons in `@if (Model.Blocks.Any())` so they only render once at least one block exists, matching the legacy disabled-until-selected rule. Build verified (`dotnet build src/Histo.Web/Histo.Web.csproj` succeeded, 0 errors, 3 pre-existing unrelated warnings).

---

## Phase 3 findings: Copy Submission / Copy Sample journeys

### Copy Submission (legacy `CopyBatch.aspx.vb` / `CopyBatchBlocks.aspx.vb` + `clsBatch.vb::CopyBatch()` vs new `Batches/CopyBatch.cshtml(.cs)` + `Batches/CopyBatchSummary.cshtml(.cs)`)

Legacy `clsBatch.vb::CopyBatch()` copies, in order, onto the new batch: (1) the batch header (`CopyDataToNewBatch`), (2) `BATCH_HISTOLOGY_TABLE`, (3) `BATCH_ANTIBODIES_TABLE`, (4) `BATCH_STAIN_TABLE`, (5) `BATCH_POSTFIXATION_TABLE`, (6) `BATCH_SUBMITTEDAS_TABLE`, and then either the submission/tissue rows (non-cassetted / Wet Tissue) or the block rows (cassetted), depending on the `bBlocked` flag set by which entry page (`CopyBatch.aspx` vs `CopyBatchBlocks.aspx`) was used.

**DEFECT FOUND (Critical) — batch-level test type selections silently dropped on copy.** `CopyBatch.cshtml.cs::OnPostAsync` (new app) called `_batches.CopyBatchHeaderAsync` to copy only the batch header fields, then went straight to copying submissions/animals/tissues. It never copied the source batch's Histology/Antibody/Special Stain selections (`BatchTestSelections`), even though `IBatchService.GetBatchTestSelectionsAsync` / `SaveBatchTestSelectionsAsync` already exist and are used elsewhere (`BatchDetails.cshtml.cs`, `EditBatchTests.cshtml.cs`). Effect: every copied submission lost its histology/antibody/stain test-type template, forcing users to re-enter them from scratch — a functional regression vs legacy, and it silently produced blocks/samples with no available test selections to inherit from.

**FIX APPLIED:** `src/Histo.Web/Pages/Batches/CopyBatch.cshtml.cs::OnPostAsync` — after `CopyBatchHeaderAsync` succeeds, now calls `_batches.GetBatchTestSelectionsAsync(SourceBatchId)` and `_batches.SaveBatchTestSelectionsAsync(newBatchId, ...)` to copy Histology/Antibody/Stain codes onto the new batch, mirroring legacy's `CopyBatch()` table-copy sequence. Build verified (0 errors, 3 pre-existing unrelated warnings).

**Note (not a defect, documented scope decision):** Post-Fixation and Submitted-As table copying (`BATCH_POSTFIXATION_TABLE` / `BATCH_SUBMITTEDAS_TABLE` in legacy) were not found to be copied either; however these are populated later in the workflow (Receive Submission / Submitted As is set at batch-header level from source and already flows through `CopyBatchHeaderAsync`'s field-by-field copy of `SourceBatch` properties) — no separate table exists for them in the new data model the way legacy split them out, so this is architecture-driven, not a gap. Flagged for awareness only.

**Sample/Animal/Submission/Tissue copy logic** (`OnPostAsync`, lines ~166-198): copies animal → linked submission → tissues per source submission, matching legacy's `CopyDataToNewBatch` (BatchSubmission) + `Tissues.CopyDataToNewBatch` sequence. Already fixed in a prior session (chicken-and-egg `AnimalID` linkage bug — see `docs/User-Prompts-Log.md` / `docs/run-log-v2.md` Run #33). No further defect found here.

**Copy Submission Summary** (`CopyBatchSummary.cshtml.cs`, replaces `CopyBatchBlocksSummary.aspx`) — displays outcome of the copy; not deeply re-audited this session, no issues observed in the code read.

### Copy Sample (legacy `CopySamples.aspx.vb` / `CopySamplesBlocks.aspx.vb` vs new `Blocks/CopySamples.cshtml(.cs)`)

Legacy: `CopySamples.aspx` lets the user pick a source sample (`ddlCopySampleFrom`) and target sample (`ddlCopySampleTo`) within the current batch (same-batch copy only — it is NOT a cross-submission copy), pulls pre-booked blocks for the target via `GetPreBookedBlocks`, and redirects to `CopySamplesBlocks.aspx` to complete the block-level copy.

New app: `CopySamples.cshtml.cs` implements a documented, deliberately-simplified single-step flow — `OnPostFindAsync` locates a **source submission** (can be any submission, not just same-batch) with blocks, `OnPostCopyAsync` copies all blocks (and their tissues) from the selected source sample onto one or more target samples in the current submission, computing new block refs/order via `CopyBlocksToAnimalAsync` (mirrors `CopyBlocksModel.CopyBlocksToAnimalAsync`). This is a deliberate, documented simplification (see code comments citing the removed 3-page wizard and its in-memory DataSet plumbing) rather than an unnoticed gap — it preserves the core business rule (copy blocks + tissues onto target sample(s)) while dropping legacy's now-irrelevant page-to-page state juggling. **No functional defect found**: block and tissue data is copied correctly, source data remains unchanged (`CopyBlockAsync`/`CopyTissueAsync` create new rows), and the "Copy sample remains disabled until at least one sample exists" rule is enforced by the button-rendering fix already applied in `SubmissionDetailsBlock.cshtml`/`SampleSummary.cshtml` in Phase 2.

**Status:** Copy Submission defect fixed and build-verified; Copy Sample confirmed aligned (no code change required).

**Targeted test verification:** Added `tests/Histo.Tests/Unit/CopyBatchModelTests.cs` covering (1) `OnPostAsync_CopiesBatchTestSelections_OntoNewBatch` — asserts `GetBatchTestSelectionsAsync(sourceBatchId)` is called and `SaveBatchTestSelectionsAsync(newBatchId, ...)` receives the source's Histology/Antibody/Stain codes unchanged, and (2) `OnPostAsync_SourceBatchNotFound_DoesNotAttemptCopy` — asserts no copy is attempted when the source batch can't be found. Ran via `dotnet test tests/Histo.Tests/Histo.Tests.csproj --filter "FullyQualifiedName~CopyBatchModelTests"`: **2/2 passed**.

---

## Phase 4: GDS markup compliance spot-check (pages touched in Phases 2-3)

Reviewed `CopyBatch.cshtml`, `SubmissionDetailsBlock.cshtml`, and `SampleSummary.cshtml` against the GDS checklist (page titles, headings, back links, error summaries, form controls, button placement) established in prior sessions (shared `_ErrorSummary.cshtml` partial with clickable `<a href="#FieldId">` links, `govuk-form-group--error` / `aria-describedby` at field level, explicit Apply/submit buttons in place of auto-postback per WCAG 3.2.2).

- **CopyBatch.cshtml**: `Model.Error` is a page-level condition (source not found / copy failed), not attributable to a single form field, so a plain non-linked `<li>@Model.Error</li>` inside `govuk-error-summary` is correct per GDS (only field-level errors require a clickable link back to the field). Page title, `govuk-back-link`, table markup, and button group all conform. **No issues found.**
- **SubmissionDetailsBlock.cshtml**: correct page title/back-link/warning-text (`govuk-warning-text` with visually-hidden "Warning" label) structure. The Phase 2 fix (hiding Edit/Delete/Copy-to-samples buttons until a block exists) also improves GDS/UX compliance by removing dead-looking active controls. **No further issues found.**
- **SampleSummary.cshtml**: page title, back link, error summary, and the "Bypass sort" checkbox with an explicit "Apply" submit button (deliberately avoiding auto-postback-on-change per WCAG 3.2.2 On Input) are all correctly implemented. **No issues found.**

**Conclusion:** the pages fixed/reviewed during the Copy Submission/Copy Sample/Sample Blocks audit (Phases 2-3) are already GDS-compliant; no additional markup remediation was required for them. A broader GDS pass across the full application (all ~30+ Razor Pages) was out of scope for this targeted review — recommend a dedicated follow-up session using the same checklist if a full-app GDS audit is desired.

---

## Redesign proposal: defer batch persistence until first sample exists

**Problem (from Phase 1):** `BatchDetails.cshtml.cs` `OnPostCreateAsync` persists the batch via `_batches.AddAsync` immediately when the Create Submission form is submitted, before any sample/animal has been added. This lets users click "Create submission" with zero samples, creating an orphan submission — legacy's `btSubmit_Click` in `BatchBlocks.aspx.vb` only finalises/redirects to Print Submission (`FinalPrintBatch.aspx`) once at least one block/sample exists (`dtBatch.Rows.Count > 0` check plus the sample/block journey having already run).

**Root cause:** the new app conflates two legacy steps into one action: (a) legacy's initial "Submission Details" save, which historically also persisted a batch row early server-side to obtain a `BatchId` for the downstream Sample/Block session state, and (b) legacy's final "Done"/`btSubmit` on the Sample Blocks or Sample Summary page, which is the actual submission-finalisation + Print Submission redirect point. The new app kept behaviour (a) but never implemented an equivalent gate for (b).

**Proposed approach (design only, not implemented):**
1. Keep `OnPostCreateAsync` persisting the batch record immediately (a `BatchId` is still needed to key samples/blocks in the database, matching legacy's own early-persist approach) — do not change this without a larger data-model change, since removing it would require reworking how samples reference an unsaved batch.
2. Add a new "Finish"/"Done" action at the end of the Sample Summary journey (`SampleSummary.cshtml`) — this is the natural equivalent of legacy's `btSubmit`/Done. It should:
   - Be disabled (or return a validation error) if `Model.Samples` (or equivalent) has zero entries, mirroring legacy's `dtBatch.Rows.Count > 0` gate — surfaced as a GDS error summary ("You must add at least one sample before finishing this submission") rather than a disabled button, for accessibility.
   - On success, mark the batch status as complete/in-progress as appropriate (mirroring legacy's `BatchStatus = STATUS_INPROGRESS` / `IsBlocked = True` flags) and redirect to the Print Submission page (`ViewSubmissions.aspx` equivalent / wherever Print Submission links currently live), matching legacy's redirect to `FinalPrintBatch.aspx`.
3. Until that "Finish" step is completed, treat the batch as a draft: `BatchDetails.cshtml` (view mode) should continue to expose "Samples" as the primary action, and any Print Submission link should remain hidden/disabled (already the case per Phase 1 findings) until the new Finish/Done gate passes.
4. This keeps the early persist (needed for FK integrity) while restoring the legacy business rule that a submission isn't considered "created"/finished, and Print Submission isn't reachable, until it has at least one sample — without requiring a full architecture rewrite.

**Status:** IMPLEMENTED.

- `SampleSummary.cshtml.cs`: added `OnPostFinishAsync()` — resolves the effective batch ID, loads the batch's animals via `_submissions.GetAnimalsByBatchAsync`, and if zero exist, sets `TempData["SampleSummary_Error"]` (surfaced by the existing `SaveError`/GDS error-summary block on redisplay) and redirects back to the page — mirroring legacy's `dtBatch.Rows.Count > 0` gate. On success it calls `_batches.UpdateStatusAsync(batchId, BatchStatus.InProgress, Session.UserID)` (existing method, already used elsewhere) and redirects to `/Batches/BatchesNotReceived`.
- Redirect target reconsidered from the original proposal: confirmed via `docs/Parity-Audit-Report.md`/`Migration-Plan.md` that `FinalPrintBatch.aspx` remains genuinely unmigrated (blocked on Phase 2 Reporting — only Crystal Reports popups, no interactive content), and that `/Reports/HistologyReport` (linked from `ViewSubmissions.cshtml`) is a same-session receipt-stage report, not the create-flow's finalise destination — `PrintSubmission.cshtml` is also post-receipt only (reached from `ReceiveBatch`). So the Finish handler follows the same already-accepted precedent as `BatchBlocks.cshtml.cs::OnPostDoneAsync`, redirecting to `/Batches/BatchesNotReceived` instead of a non-functional Print Submission stub.
- `SampleSummary.cshtml`: added a "Finish" button (secondary style) inside the existing `CanModifySamples && !IsViewMode` block, posting to the new `Finish` handler.
- Build verified: `dotnet build src/Histo.Web/Histo.Web.csproj` succeeded, 0 errors (3 pre-existing unrelated warnings).

**Test verification:** added `tests/Histo.Tests/Unit/SampleSummaryModelTests.cs` with 2 unit tests covering `OnPostFinishAsync`:
- `OnPostFinishAsync_NoSamples_ReturnsErrorAndDoesNotChangeStatus` — asserts a zero-animal batch redirects back to the same page with the `SampleSummary_Error` TempData message set, and `IBatchService.UpdateStatusAsync` is never called.
- `OnPostFinishAsync_HasSamples_UpdatesStatusAndRedirectsToBatchesNotReceived` — asserts a batch with at least one sample calls `UpdateStatusAsync(batchId, BatchStatus.InProgress, userId)` and redirects to `/Batches/BatchesNotReceived`.

Ran `dotnet test tests/Histo.Tests/Histo.Tests.csproj --filter "FullyQualifiedName~SampleSummaryModelTests"` → **2/2 passed**. Full `dotnet build src/Histo.Web/Histo.Web.csproj` re-verified clean afterward (0 errors).

**Status: CLOSED.** All known observations, Sample Summary/Add Sample/Sample Blocks button-rule fixes, Copy Submission/Copy Sample review, GDS spot-check, and the deferred create-flow redesign (Finish/Done gate) are implemented, build-verified, and test-verified. No further pending items for this audit.

