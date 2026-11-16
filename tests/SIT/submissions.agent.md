---
description: Runs SIT tests for the Submissions home-screen options (New submission, View submissions) in the running Histo.Web app using the integrated browser. Can run the whole file or a single suite.
tools: [open_browser_page, navigate_page, read_page, click_element, type_in_page, run_playwright_code, screenshot_page, run_in_terminal, get_terminal_output, read_file, grep_search]
---

# Submissions SIT Agent

## Running a subset

Run only what the user asks for, for example "run the View submissions suite", "run SB-07 to SB-10", or the whole file.

| Suite | ID prefix | Page |
|-------|-----------|------|
| 1. New submission | SB-01 to SB-06 | `/Batches/Cassetted` |
| 2. View submissions | SB-07 to SB-18 | `/Submissions/ViewSubmissions` |
| 3. Edit submission | SB-23 to SB-40 | `/Search/SearchSubmissions`, `/Batches/ReceiveBatch`, `/Batches/EditBatch` |
| 4. Copy to samples and Murine mouse ranges | SB-41 to SB-62 | `/Submissions/SubmissionDetailsBlock`, `/Blocks/CopyBlocks`, `/Submissions/SampleSummary`, `/Submissions/AddSubmission` |

## Prerequisites (all suites)

1. Start the app if `https://localhost:57879/` is not reachable: `dotnet run --project src\Histo.Web --launch-profile Histo.Web` (async terminal).
2. Local DB `(localdb)\MSSQLLocalDB`, database `histo`, with migrations from `src/Database/Migrations` applied.
3. Dev auth signs in as a valid local test persona. Default harness account: "Local Developer" (Group: Maintenance, Area: All Areas). This is a local convenience identity, not a production entitlement. The app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. For access-control or area-scoping cases, explicitly use the matching valid combination instead of assuming Maintenance/All Areas.

## Rules (all suites)

- Use a unique stamp `<yyyyMMddHHmmss>` for any data-creating run.
- For the New submission suite, create actual `SIT` batches so each supported submission type is validated in both categories. The app’s real `luSubmittedAs` lookup values are: `1=Wet Tissue`, `2=Wax Block`, `3=Stained Section`, `4=Unstained Section`, `5=Pre Cassetted Tissue`.
- The submission category is the batch type (`BatchTypeConstants.Tse = 0`, `BatchTypeConstants.NonTse = 1`), and the submission type is the `SubmittedAs` lookup code from table 11. The service layer is a thin pass-through; the actual matrix is driven by the `Cassetted` page and the `luSubmittedAs` lookup, not by a custom submission-service switch statement.
- For the final report, list each created submission ID and the `BatchType + SubmittedAs` pair it represents.
- Browser notes: if the tab is not visible, click via `evaluate(b => b.click())`; navigate to link `href`s instead of clicking them; a `PageNumber` past the last page returns the last page again.
- On HTTP 500 or an unexpected "There is a problem" box, read the app terminal and include the exception.
- If the session times out (`/SessionError`), navigate to `/` and continue.

---

## Suite 1: New submission

The New submission journey is data-creating. Use one unique `SIT` stamp per run and create a submission for every supported `BatchType × SubmittedAs` pair in the real lookup table.

Submission-type lookup (`luSubmittedAs`, table 11):

- TSE/Non-TSE category: `BatchType = 0` / `1`
- SubmittedAs values: `1=Wet Tissue`, `2=Wax Block`, `3=Stained Section`, `4=Unstained Section`, `5=Pre Cassetted Tissue`
- Matrix to cover: `2 categories × 5 types = 10 submissions`

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-01 | Page loads | Open `/Batches/Cassetted` | Heading "Submission type"; radios TSE (`#batchType-tse`) and Non-TSE (`#batchType-nontse`); select `#SubmittedAs`; Next button; Back and Cancel go to `/` |
| SB-02 | Submission type options | Read `#SubmittedAs` options | All five real values are present; no hidden/duplicate entries |
| SB-03 | Next with nothing selected | Click Next | Stays on page; error or browser validation; no 500 |
| SB-04 | Create TSE matrix | For each `SubmittedAs` value in `[1,2,3,4,5]`, choose `BatchType = TSE`, continue, fill the required batch header fields, save, add at least one sample with at least one tissue per sample, click Finish (see "Completing a submission"), and note the created submission id | A new batch is created for each row; `BatchType` persists as TSE; `SubmittedAs` persists to the chosen code; no 500 |
| SB-05 | Create Non-TSE matrix | Repeat SB-04 with `BatchType = Non-TSE` | A new batch is created for each row; `BatchType` persists as Non-TSE; `SubmittedAs` persists to the chosen code; no 500 |
| SB-06 | Cancel | Follow Cancel from `/Batches/Cassetted` | Returns to home; nothing is saved |

Example data pattern for each created batch:

- Customer ref: `SIT<stamp>-<batchType>-<submittedAs>`
- Project code / contact / species / fixation: use a valid existing lookup value from the app
- Batch date: today
- Check the created record in the app and confirm `Submission type` / `BatchType` match the chosen pair

Completing a submission (mandatory for every created batch, especially Wet Tissue):

A batch is not complete until it has samples. After "Create submission" lands on `/Submissions/SampleSummary?batchId=<id>`:

1. Click "Add sample" (`/Submissions/AddSubmission`), fill Sender reference (e.g. `SIT-<type>-01`, max ~10 chars) and click "Add sample".
2. On `/Submissions/SubmissionDetails` (Sample details): fill `#PMDate`, fill the sample `#Comment`, and click "Save details" (returns to the summary).
3. Click "Edit sample" on the summary row, then in the Tissues section select a `#TissueCode` (first real option), set `#NoPieces` (e.g. 2), fill the tissue `#Comment`, and click "Add tissue". Every sample must have at least one tissue; confirm the Tissues table shows the row and the summary shows "<n> tissue(s)" (not "None").
4. Return to `/Submissions/SampleSummary?batchId=<id>` (navigate directly; the Back link on the details page returns there) and click "Finish".
5. Expected: `/Batches/PrintSubmission` shows "Submission not received" and the submission number.

Always set a PM date of the previous day (today minus 1, `yyyy-MM-dd`) wherever a PM date field exists (`#PMDate` on Sample details, PM date on Sample blocks) and always fill a customer reference (`#NewCustomerRef` on Add block; use `SIT<stamp>-CUST-<nn>` wherever a customer ref field exists).

Comments: fill every comment field encountered (batch `#Create_Comments`, sample `#Comment`, tissue `#Comment`) with `SIT<stamp> <context>` text. The sample and tissue comments both use the id `#Comment` on the same page; fill each in its own step.

Block-based types: Wax Block, Stained Section, and Unstained Section (all use the Sample blocks flow; only Wet Tissue uses sample tissues):

1. "Add sample" with a unique sender ref per submission (sender refs identify the animal and are reused across batches; reusing one makes PM date/histology ref read-only and the block ref becomes 02). Use `SIT-<tag>-1`.
2. `/Submissions/SubmissionDetailsBlock`: set `#EditPMDate`, choose `#HistologyRefType` (first real option) and click "Get next ref" BEFORE "Add block"; otherwise "Enter a Histology Reference for this sample before adding a block" is shown. (The Get next ref reload clears the PM date; set it again before Save details.)
3. "Add block" (`/Blocks/BlockDetails`): fill `#NewNumberOfBlocks`, `#NewCustomerRef`, `#NewComment`, tick `#hist-1`, then for each tissue select `#NewTissueCode`, set `#NewTissueNoPieces`, click "Add tissue" (two tissues), then "Done".
4. Back on Sample blocks, set `#EditPMDate` (previous day) and click "Save details" (returns to the Sample summary); then "Finish" -> `/Batches/PrintSubmission`.
5. If Block details returns HTTP 500 "Could not find stored procedure", deploy the missing script from `src/Database/StoredProcedures` with `sqlcmd -S "(localdb)\MSSQLLocalDB" -d histo -E -b -i <script>` and retry.

Pre Cassetted Tissue (`SubmittedAs = 5`) details:

1. Book the sender ref first: "Add sample" fails with "no pre-booked block" otherwise. At `/Bookings/BookBlockRef` fill `#SenderRefFrom` (e.g. `SITPC02`) and `#BlockRefFrom` (`01`) only; leave both "to" fields empty ("Block Ref to" must be greater than "from").
2. Then follow the block-based steps above (Add sample, Get next ref, Add block with two tissues, Done, Save details, Finish). The block ref is pre-filled in Add block, and the histology ref is mandatory for this type.

Cleanup:

- Record each created ID in the report and, if the environment allows it, clean up the temporary `SIT` submissions after the run or leave them clearly marked as test data.

## Suite 2: View submissions

The page can take about 10 seconds to load; use a navigation timeout of at least 60 s.

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-07 | Page loads | Open `/Submissions/ViewSubmissions` | Heading "View submissions"; filters: Submission number, Status, Project or contract code, Pathologist, Species, Fixation, Submitted by, Entered by, Histology ref, Sender ref, submitted and received date ranges; table Sub. Number, Project Code, Pathologist, Species, Date Submitted, Date Received / Rejected, Date Completed, Customer Received Date, Status, Select |
| SB-08 | Sort | Use each sortable header | Rows reorder; no error |
| SB-09 | Pagination | `?PageNumber=2` | Different rows; "Page 2 of n" |
| SB-10 | Search by number | Number from the first row; Search | Only that submission |
| SB-11 | Search by status | Choose a status; Search | Every row has that status |
| SB-12 | Search by date range | Submitted date from/to around a known date | Rows in range |
| SB-13 | Combined filters | Status and pathologist | Results satisfy both |
| SB-14 | No results | Number `99999999` | Empty result or message; no error |
| SB-15 | Invalid date range | From later than to | Validation or empty result; no 500 |
| SB-16 | Clear search | Follow "Clear search" | Filters reset |
| SB-17 | Select a submission | Click Select on a row | Details page opens; no error |
| SB-18 | Back | Follow Back | Returns to home |
| SB-19 | Every filter | One run per filter: `#SubmissionNumber`, `#Status`, `#ProjectContractCode`, `#ContactName`, `#Species`, `#Fixation`, `#SubmittedBy`, `#EnteredBy`, `#HistologyRef`, `#SenderRef`, `#SubmittedDateFrom/To`, `#ReceivedDateFrom/To`; use a known SIT batch (e.g. sender ref, histology ref, submitted by) | Every displayed row matches the filter; non-displayed filters (fixation, submitted/entered by, histology ref, sender ref) return the known batch or a changed row count |
| SB-20 | Sort every column | For each of Sub. Number, Project Code, Pathologist, Species, Date Submitted, Date Received / Rejected, Date Completed, Customer Received Date, Status click twice | Page 1 is ordered ascending then descending (compare dates as dd/MM/yyyy); the first click toggles direction if that column is already sorted |
| SB-21 | Pagination controls | Click `2`, `›`, `‹`, `»`, `«`; repeat with Status filter and with a sort applied | `«`/`‹` disabled on page 1; `»` goes to the last page; rows differ per page; filter and sort persist across pages |
| SB-22 | Export to excel | Click "Export to excel" with no filter and with Status = Not received | `view-submissions.xlsx` (`application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`) with 16 columns (Submission Number ... Completed Date); filtered export has header plus only the filtered rows (compare to the grid count); unfiltered export is capped at 200 rows, same as the grid (20 pages) |

Execution notes: all actions are POSTs (`?handler=Search&SortColumn=...&SortDesc=...`, `?handler=Search&PageNumber=n`, `?handler=ExportExcel`); each request takes about 7 s. Click header, pager and Search buttons with `page.evaluate(() => button.click())` plus `waitForNavigation` because the tab may not be visible. For exports use an in-page `fetch` POST of the search form data to `?handler=ExportExcel` and inspect the xlsx (zip) instead of waiting for a download event, which can hang.

## Suite 3: Edit submission

Data-changing. Edit only a `SIT` submission created in this run (see Suite 1 and `end-to-end.agent.md`), never an existing one. Entry points to `/Batches/EditBatch`:

- Search submissions: select a row, then the "Edit submission" button in the action panel (enabled for Received, On Hold and In Progress only).
- Receive submission (`/Batches/BatchesNotReceived` -> Receive): the "Edit submission" button, offered while the submission is still Not received.

To cover every status, take one SIT submission through its lifecycle and test at each point: Not received (after Finish), Received (after Receive), In progress (after tissues are assigned to blocks), Completed (after dispatch). Use `#SubmissionNumber` on Search submissions to find it. Selecting a row in Search submissions sets view-only mode; opening Edit submission must clear it.

### 3a. Entry from Search submissions

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-23 | Not received | Search by submission number; Select the row | Edit submission is disabled (title "Editing is only available for submissions in Received, On Hold or In Progress status") |
| SB-24 | Received | Receive the submission, search again; Select | Edit submission is enabled and opens `/Batches/EditBatch` (heading "Edit submission") with the submission's values pre-filled |
| SB-25 | In progress | After tissues are assigned to blocks; Select | Edit submission enabled; status tag shows In progress |
| SB-26 | On hold | Put the submission on hold from Edit submission status (`/Batches/BatchesForEditing`); Select in Search submissions | Edit submission enabled; reset the status afterwards |
| SB-27 | Completed | After dispatch; Select | Edit submission disabled |
| SB-28 | Rejected | Reject a second SIT submission at Receive; Select | Edit submission disabled |
| SB-29 | Back and Save return | From Edit submission follow Back; repeat and Save | Both return to `/Search/SearchSubmissions` (with the original ReturnPage); not to the lab list |

### 3b. Entry from Receive submission

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-30 | Button present | Open Receive for a Not received SIT submission | Buttons Done, "Edit submission" and Cancel are shown |
| SB-31 | Open edit | Click "Edit submission" | `/Batches/EditBatch` opens with the same submission; nothing is received or saved by the click |
| SB-32 | Save returns to Receive | Change the comments; Save | Returns to `/Batches/ReceiveBatch` for the same submission, status still Not received, receipt fields unchanged; Back and Cancel from Edit also return to Receive |
| SB-33 | Receive still works | After SB-32 complete Receive (status Received, Date received, `#TimeReceived`, Received by); Done | `/Batches/PrintSubmission` shows "Submission received"; the edited values are kept |

### 3c. Edit submission form (run once via Search submissions on a Received SIT submission)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-34 | Read-only fields | Inspect the page | Entered by, Entered area and Submitted as are display-only; submission category radios (`#batchTypeField-tse`, `#batchTypeField-nontse`) are disabled; Submitted area is editable only when the signed-in user's area is Histopath, otherwise disabled |
| SB-35 | Edit header fields | Change Project, Pathologist, `#BatchDateStr`, Fixation, Species, Comments; Save | Returns to the entry list; reopening Edit and View submission show the new values; the status is unchanged |
| SB-36 | Required fields | Clear the histology selection; Save. Then with histology restored, uncheck both `#SafeToHandle` radios via `evaluate` and Save | "Select at least one histology type." and "Select whether the submission is adequately fixed."; nothing saved |
| SB-37 | Test dependencies | Archive plus another histology type; Special Stain with no stain; IHC with no antibody | "Archive cannot be combined with other histology types.", "Special Stain is selected — you must also select at least one special stain.", "IHC is selected — you must also select at least one antibody." |
| SB-38 | Locked test | On an In progress submission with a block, try to untick a histology/antibody/stain used on that block | Its checkbox is disabled (server rejects a forced post with "The test cannot be de-selected as it has been assigned to a block."); adding a new test type is allowed |
| SB-39 | Save and proceed to samples | Change comments; "Save and proceed to samples" | `/Submissions/SampleSummary?batchId=<id>` opens; Add/Edit sample are available (not view-only) and its Back link returns to Edit submission |
| SB-40 | Audit | After SB-35 search the audit log by submission for the id (`/AuditLog/AuditLogBySubmission`) | Entry for the edit by the signed-in user (SKIP with a note if not found) |

## Suite 4: Copy to samples and Murine mouse ranges

Data-creating. Two copy features exist:

- **During sample creation**: Sample summary (`/Submissions/SampleSummary`) -> "Copy sample" on a row -> `/Submissions/AddSubmission?sourceAnimalId=<id>`, which creates a new sample from the source sample, optionally a whole range of mouse numbers.
- **During Assign tissues to blocks**: `/Batches/BatchesReceived` -> View -> Edit sample (`/Submissions/SubmissionDetailsBlock`, "Sample blocks") -> tick blocks -> "Copy to samples" -> `/Blocks/CopyBlocks`. The batch-wide Sample blocks page (`/Batches/BatchBlocks`) has no Copy to samples button, only "Copy from a previous submission" (`/Blocks/CopySamples`).

Setup (once per run): create a Non-TSE Wax Block submission (see Suite 1) with **Species = Murine** (`#Create_SpeciesId`), receive it (`/Batches/BatchesNotReceived`), then from `/Batches/BatchesReceived` open it. Source sample sender ref `SIT<last 6 of stamp>`; on its Sample blocks page choose `#HistologyRefType` (General Pool) and "Get next ref", record the ref (format `NN/NNNNN`), and add one block with at least one tissue (as in the block-based steps above). Create two more plain samples `SIT<6>-B` and `SIT<6>-C` on the same submission, with no histology ref, to act as copy targets.

Mouse-range prerequisites: the "Alternatively, assign mouse ranges" fields (`#MouseNumberFrom`, `#MouseNumberTo`) only appear on Copy sample and only when the signed-in user's Area is Histopath. The `Local Developer` / `All Areas` harness persona does not see them: use a Histopath-area user (the regression baseline user is). The code does not check the species, so Murine is the intended data set rather than an enforced rule. Mouse numbers are `MC` plus 6 digits and are shared across submissions, so use an unused block `MC9<5 digits from the stamp>` and confirm none exist first with `/Search/SearchSender`. A mouse number that already exists elsewhere is reused with its existing histology ref (SB-58).

### 4a. Copy to samples (Assign tissues to blocks)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-41 | Button visibility | On the source sample's Sample blocks page, with and without blocks, and in view-only mode (opened from Search submissions via View) | "Copy to samples" appears only when the sample has at least one block and the page is not view-only; it is not on `/Batches/BatchBlocks` |
| SB-42 | Nothing selected | Click "Copy to samples" with no block ticked | Stays on Sample blocks; nothing copied; no 500 |
| SB-43 | Copy page | Tick the block (`name=blockIds`); "Copy to samples" | `/Blocks/CopyBlocks` opens with "Blocks to copy" listing block ref, customer ref and comment, and "Copy to sample(s)" checkboxes listing every other sample of the submission, not the source; "Select all" (`#select-all-samples`) is shown when JavaScript runs and ticks/unticks all samples; "Auto generate histology refs" (`#AutoGenerateHistologyRefs`) is shown; Back and Cancel return to the sample's Sample blocks page |
| SB-44 | No target | Click "Copy blocks" with no sample ticked | "Select at least one sample to copy the block(s) to."; nothing copied |
| SB-45 | Copy without refs | Tick `SIT<6>-B`; leave Auto generate unticked; Copy blocks | Redirect to Sample blocks with "Copied 1 block(s) to 1 sample(s)."; the target now has the block with the same tissues and test ticks, and still no histology ref |
| SB-46 | Copy with auto refs | Tick `SIT<6>-C`; tick Auto generate; Copy blocks | "Copied 1 block(s) to 1 sample(s). Assigned a histology ref to 1 sample(s)."; the target's histology ref is the next ref of the same type as the source's (for example the next General Pool number), different from the source's |
| SB-47 | Existing ref kept | Copy the block again to `SIT<6>-C` with Auto generate ticked | "No histology refs were generated because the selected samples already have one."; its ref is unchanged |
| SB-48 | Several targets | Add two more samples, tick all via "Select all", Auto generate ticked | Message reports the number of samples copied to and refs assigned; each target gets a distinct ref |
| SB-49 | Source without a ref | Source sample without a histology ref; copy to a target with Auto generate | Blocks are copied; message says no refs were generated because the reference type could not be determined from the source sample |
| SB-50 | No other samples | Submission with a single sample; open Copy to samples | "No other samples are available in this submission to copy blocks to."; no Copy blocks button |
| SB-51 | Access | Open `/Blocks/CopyBlocks` directly with no `CopyBlockIds` pending (new tab) | Redirects back to Sample blocks; no 500 |

### 4b. Copy sample (Sample summary) with a single sender ref

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-52 | Copy sample, block type | On Sample summary click "Copy sample" for the source; enter a new sender ref `SIT<6>-D`; Add sample | Returns to Sample summary; the new sample has the source's block(s) and tissues, and its own histology ref drawn from the same type |
| SB-53 | Copy sample, Wet Tissue | Same on a Wet Tissue SIT submission | Tissues copied; no block ref; histology ref left to be chosen on Sample blocks once blocks are assigned |
| SB-54 | Sender ref required | Copy sample with empty Sender reference (non-Histopath user or no range entered) | "Enter the sender reference." or, for a Histopath user, "Enter either the Sender ref or the mouse number ranges."; nothing created |

### 4c. Copy sample with a Murine mouse-number range (Histopath user)

| ID | Scenario | Steps | Expected |
|----|----------|-------|----------|
| SB-55 | Fields shown | Copy sample on the Murine SIT submission | "Alternatively, assign mouse ranges" with `#MouseNumberFrom` and `#MouseNumberTo` (max 8 chars) and the hint about `MC000105`; as a non-Histopath user they are absent |
| SB-56 | Range happy path | From `MC9<5 digits>` to the number plus 2 (three samples), Sender reference empty; Add sample | Returns to Sample summary with exactly three new samples with sender refs `MC...` in ascending order; each carries the source's tissues (Wet Tissue) or blocks and tissues (block type) |
| SB-57 | Consecutive histology refs | Open each new sample's Sample blocks page (or search View samples by sender ref) | Histology refs are a consecutive run in mouse-number order, same type as the source (for example `26/40010`, `26/40011`, `26/40012`), none equal to the source's ref and none duplicated |
| SB-58 | Existing mouse number | Range that includes one `MC` number already used on another SIT submission (not this one) | The existing sample is reused and keeps its histology ref; only samples without a ref get new refs from the run; no duplicate-ref error |
| SB-59 | Range input validation | Run each: only From; only To; both a Sender reference and a range; `MC12`, `AB000105`, `MC00010A`; From greater than To; From equal to To; a 1001-number range; a range with a number already on this submission | "Enter both the from and to mouse numbers."; same; "Enter either the Sender ref or the mouse number ranges."; "The mouse number format is MC followed by 6 digits, i.e. MC000105." (three cases); "The from number must be less than the to number." (both cases); "The mouse number range cannot exceed 1000 entries. Use a smaller range or create a bulk job."; "Mouse number MC... already exists on the new submission. Alter the range and try again."; nothing created in each case and the error summary links to the mouse number field |
| SB-60 | No source ref | Source sample without a histology ref; range of two | Samples created with tissues/blocks; no histology refs assigned; no 500 |
| SB-61 | Murine data | After SB-56 open the submission in View submissions and Search submissions | Species shows Murine; the new `MC` samples appear in View samples (`/Search/ViewSamples`) with the expected histology refs |
| SB-62 | Copy submission (staged) | `/Batches/CopyBatch` Change on a sample, enter a mouse range | One staged row per mouse number; nothing is written until Finish; after Finish the samples exist once each (SKIP if the Change button is not offered) |

---

## Report

One table per suite run:

```
| ID | Result (PASS/FAIL/SKIP) | Evidence / failure detail |
```

State which suites ran. End with a one-line summary (`n passed, n failed, n skipped`) and any app-log exceptions seen.
