# Database Scripts — Histopathology Migration

This folder contains all SQL Server scripts written during the .NET 10 migration
of the Histopathology application (Histo).  Each script is numbered in run order.
One script (`LOCAL_DEV_ONLY_*`) is explicitly **never** to be deployed beyond a
developer's local machine.

---

## Scripts

| # | File | Scope | Summary |
|---|------|-------|---------|
| 1 | `001_create_histologyuser_login.sql` | All environments | Create SQL login and remap orphaned DB user |
| 2 | `002_create_getinprogressbatches.sql` | All environments | New SP: batches with status 6 (In Progress) |
| 3 | `003_create_getbatchesnotreceived.sql` | All environments | New SP: batches with status 1 (Not Received) |
| 4 | `004_create_getbatchesonhold.sql` | All environments | New SP: batches with status 5 (On Hold) |
| 5 | `005_create_getqcnotes.sql` | All environments | New SP: all QC notes |
| 6 | `006_create_getqcnote.sql` | All environments | New SP: single QC note by ID |
| 7 | `007_create_gettestsbybatchid.sql` | All environments | New SP: all tests for a batch (UNION ALL) |
| 8 | `008_grant_permissions.sql` | All environments | GRANT EXECUTE on new SPs to HistologyUser |
| 9 | `009_testdata_inprogress_batches.sql` | **LOCAL DEV ONLY** | Seed three batches as In Progress for smoke-testing |
| — | `LOCAL_DEV_ONLY_alter_getbatchqcnotes.sql` | **LOCAL DEV ONLY** | Removes linked-server joins from GetBatchQCNotes so the SP works on LocalDB |

---

## Justification

### 001 — HistologyUser SQL Login

**Root cause:** The migrated `appsettings.json` connection string uses SQL
authentication (`HistologyUser` / password).  The legacy application used Windows
Authentication; the `HistologyUser` database principal existed as an orphaned user
with no server-level login.  The ASP.NET Core application cannot authenticate
through Windows Negotiate, so a SQL login was required.

**Change:** `CREATE LOGIN HistologyUser` on the server + `ALTER USER … WITH LOGIN`
to repair the orphaned DB user.

**Production note:** The password in the script is the local-dev value.  Each
environment must supply its own credential via environment-specific configuration
(Key Vault / App Service settings).  Do not hard-code the production password.

---

### 002 – 004 — GetInProgressBatches, GetBatchesNotReceived, GetBatchesOnHold

**Root cause (migration gap):**  The legacy VB.NET app called a single
`GetBatchesWithStatus` SP and passed a status code parameter.  During the C#
migration the `BatchRepository` was refactored to call three separate, named SPs
(`GetInProgressBatches`, `GetBatchesNotReceived`, `GetBatchesOnHold`) following the
named-SP convention adopted across the rest of the migrated codebase.  However, the
corresponding `.PRC` files were never written and the SPs were never created in any
environment.  Every page that navigates to an in-progress, not-received, or on-hold
batch list would fail with "Could not find stored procedure" until this was fixed.

**Change:** Three new `CREATE OR ALTER PROCEDURE` scripts, each filtering `Batch`
by the appropriate `BatchStatus` code and returning the same column set as the
original `GetBatchesWithStatus` result.

**Column mapping:**

| SP column | Source column | Notes |
|---|---|---|
| `ID` | `Batch.ID` | PK |
| `Status` | `Batch.BatchStatus` cast to varchar(5) | Allows mapping to enum in C# |
| `CustomerRef` | `Batch.OtherSubmittedBy` | Legacy alias preserved |
| `Comments` | `Batch.Comments` | |
| `ReceivedDate` | `Batch.DateReceived` | |
| `CompletedDate` | `Batch.DateCompleted` | |
| `SubmittedByUserID` | `Batch.SubmittedBy` cast to int | |
| `UserAreaCode` | `Batch.SubmittedArea` cast to int | |
| `IsPreCassetted` | `Batch.Cassetted` cast to bit | |
| `RowStamp` | `Batch.RowStamp` | Optimistic concurrency |
| `BatchType` | `Batch.BatchType` cast to int | |

---

### 005 – 006 — GetQCNotes, GetQCNote

**Root cause (migration gap):**  The legacy app called `GetAllQCNotes` (no
parameter) and `GetQCNoteByID` (integer parameter).  These SPs returned a column
named `QCText` as the text field.  The migrated `QCNoteRepository` was updated to
call the renamed SPs (`GetQCNotes`, `GetQCNote`) and to alias the column as `Text`
to match the new `QCNoteModel`.  The date was also changed from a raw `datetime` to
a formatted `varchar(30)` (British date format, 103) to remove the formatting logic
from the application layer.  Neither SP nor its `.PRC` file was ever created in any
environment.

**Change:** Two new `CREATE OR ALTER PROCEDURE` scripts returning the corrected
column set.

**Column mapping:**

| SP column | Source | Notes |
|---|---|---|
| `ID` | `QCNotes.ID` | PK |
| `CreatedBy` | `QCNotes.CreatedBy` | Username string |
| `DateCreated` | `QCNotes.DateCreated` | Formatted DD/MM/YYYY |
| `Text` | `QCNotes.QCText` | Aliased to match model |
| `RowStamp` | `QCNotes.RowStamp` | Optimistic concurrency |

---

### 007 — GetTestsByBatchID

**Root cause (migration gap):**  The legacy VB.NET app (class
`clsBatchSummary.CreateTestSummaryData`) built the combined test list entirely in
application memory: it called three separate SPs to fetch Histology, Antibody, and
Stain rows separately, then merged and sorted them in VB collections.  The migrated
C# layer moved this join logic into a single stored procedure for performance and
correctness, but the SP was never written or deployed to any environment.  The
Quality Data page (`/QC/QualityData`) would fail with "Could not find stored
procedure" for every batch.

**Change:** One new SP using `UNION ALL` across `BlockHistology`,
`BlockAntibodies`, and `BlockStain`, joined back to `BatchBlock` and `Animal`, with
LEFT JOINs to the relevant lookup tables.  Two derived columns are computed in SQL
rather than in the application:

- **`OnHold`** — `1` when the parent `BatchBlock.Status = 2`, else `0`.
- **`Archived`** — `1` when both `ArchiveLocation` and `ArchivedDate` are non-null,
  else `0`.

The `TestType` discriminator column (`'Histology'`, `'Antibodies'`, `'Stain'`) is a
string literal in each branch of the UNION, allowing the C# model to map test type
without a separate lookup.

---

### 008 — Grant Permissions

**Root cause:**  The six new SPs (scripts 002–007) are not covered by any
pre-existing `GRANT EXECUTE` statement.  Without this grant the `HistologyUser`
account — used by the migrated app — would receive "The EXECUTE permission was
denied" for each new SP.

**Change:** Six `GRANT EXECUTE … TO [HistologyUser]` statements.

---

### 009 — Test Data (LOCAL DEV ONLY)

**Purpose only:** Makes three existing batch rows visible on the "In Progress"
batch list during local smoke-testing.  Batch IDs 29401, 29402, and 29404 were
already present in the copied dev database; setting them to status 6 (In Progress)
provides enough data to verify the page renders correctly.

**Why it must not be deployed:** These are real batch records in the copied
database.  Changing their status in any real environment would corrupt business
data.  The script contains a safety guard that raises an error if `@@SERVERNAME`
does not match a LocalDB instance name.

---

### LOCAL_DEV_ONLY — ALTER GetBatchQCNotes

**Root cause:**  The production `GetBatchQCNotes` SP joins a linked server named
`DEFACPVWPSQL001` to resolve species descriptions.  That linked server is only
available from the production and staging SQL Server instances.  A LocalDB dev
instance has no linked server, so executing the production SP fails immediately with
"Could not find server 'DEFACPVWPSQL001' in sys.servers."

**Change (local only):**  `ALTER PROCEDURE` that replaces the linked-server JOIN
with a direct read of `Batch.Species` (raw numeric code).  The C# `QCNoteModel`
already handles a numeric species code, so the page still renders; the species
display is a code rather than a description until the app is pointed at a real
instance.

**Why it must not be deployed:**  The production SP intentionally joins the linked
server for a richer description.  Deploying this version to any non-LocalDB
environment would silently degrade the species display for all users.

The script contains a safety guard that raises an error if `@@SERVERNAME` does not
match a LocalDB instance name.

---

## Known Remaining Gaps

The following stored procedures are called by migrated repositories but have no
`.PRC` file and were not discovered to be missing until further testing.  They will
need the same treatment as the scripts above when those pages are reached during
integration testing:

| SP name | Called by | When it will fail |
|---|---|---|
| `EditBatchStatus` | `BatchRepository.UpdateStatusAsync` | Edit batch status page |
| `GetAllBatchComments` | `BatchRepository.GetCommentsAsync` | Batch comments view |
| `GetBatchesLinkedToBlocks` | `BlockRepository` | Block-to-batch linking page |
| `GetHistologyDispatched` | `HistologyRepository` | Dispatch report page |
| `GetStainDispatched` | `StainRepository` | Dispatch report page |
| `GetAntibodiesDispatched` | `AntibodiesRepository` | Dispatch report page |
| `EditBatchCompletedDate` | `BatchRepository.UpdateCompletedDateAsync` | Complete batch action |

These should be scripted and added to this folder once their correct column sets
are confirmed from the legacy `.PRC` files in `HistopathologySystem/Database/`.

---

## Run Order (All Environments)

Run scripts **001 → 008** in sequence, once, on every environment database before
deploying the migrated application.

```
001_create_histologyuser_login.sql
002_create_getinprogressbatches.sql
003_create_getbatchesnotreceived.sql
004_create_getbatchesonhold.sql
005_create_getqcnotes.sql
006_create_getqcnote.sql
007_create_gettestsbybatchid.sql
008_grant_permissions.sql
```

## Run Order (Local Dev — Complete Picture)

To get the application fully functional against a LocalDB instance, run all of the
above **plus** the following scripts in this order.  These two scripts must **never**
be run in test, staging, or production.

```
009_testdata_inprogress_batches.sql          -- LOCAL DEV ONLY: seed In Progress batches
LOCAL_DEV_ONLY_alter_getbatchqcnotes.sql     -- LOCAL DEV ONLY: remove linked-server joins
```

Both scripts contain a `@@SERVERNAME` safety guard that raises an error and aborts
if executed against a non-LocalDB instance.
