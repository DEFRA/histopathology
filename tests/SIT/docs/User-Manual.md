# Histopathology Submissions System — User Manual

## 1. Introduction

The Histopathology Submissions System is used to create and track pathology submissions from creation through receipt, laboratory processing, quality control, reporting, dispatch and archiving.

## 2. Signing In and Out

1. Open the system address supplied by your administrator.
2. Sign in with your Microsoft work account. You are asked to sign in again each time you start a new session.
3. The home page shows only the options available to your user group.
4. Use **Sign out** when finished.

Session rules:
- You are signed out after a period of inactivity (120 minutes by default). Return to the home page and sign in again.
- Signing in on another device ends your earlier session.
- If you see **Access denied**, you have no account or group in the system. Contact an administrator.

## 3. User Groups

| Area | Customer | Histopathology User | Maintenance |
|---|:-:|:-:|:-:|
| New submission, View submissions | Yes | Yes | Yes |
| Search by PM date, View samples | Yes | Yes | Yes |
| Laboratory (receive, assign blocks, status, quality data, QC notes, archive) | No | Yes | Yes |
| Search and reports, Audit logs | No | Yes | Yes |
| Booking | No | Yes | Yes |
| Administration (users, pick lists, edit sender/histology ref) | No | No | Yes |

Histopathology Users and Customers only see submissions for their own area.

## 4. Home Page

Panels shown depend on your group: **Submissions**, **Search**, **Laboratory**, **Search and reports**, **Booking**, **Administration**.

## 5. General Controls

- **Back** link returns to the previous step.
- Fields marked with an error message must be corrected before continuing. An error summary appears at the top of the page and each message links to its field.
- Buttons only appear when they can be used (for example Edit and Delete appear once a sample exists).
- Result tables can be sorted by clicking a column heading and paged using the page links.
- **Export to Excel** is available on search and view pages.
- Dates are entered with the date picker and shown as dd/mm/yyyy.

## 6. Submissions (all groups)

### 6.1 Create a new submission
1. Home → **New submission**.
2. Select the submission type: Pre Cassetted Tissue, Stained Section, Unstained Section, Wax Block or Wet Tissue.
3. Complete **Submission details**: contact and reference details, tick the histology types, antibodies and special stains required.
   - Selecting **Archive** or **EO** clears the other histology types; they cannot be combined.
   - Antibodies are required when IHC is selected; stains are required when Special Stain is selected.
4. Select **Samples** to continue.

### 6.2 Add samples
1. On **Sample summary**, select **Add sample**.
2. Enter the **Sender reference**, or use **Search for a previously used sender reference** and select a result.
3. Complete the sample details and save.
4. For wet tissue, add tissues to the sample. For cassetted types, add blocks (see 6.3).

Sender references identify the animal and are shared across submissions. Reusing one updates the animal's details on earlier submissions, so only reuse a reference for the same animal.

### 6.3 Add and manage blocks
1. Open the sample and select **Add block**.
2. Enter block details and select tests (histology, antibodies, special stains).
3. Use **Next block**, **Done** or **Cancel**.
4. **Edit block**, **Delete block** and **Copy to samples** appear only once at least one block exists.

### 6.4 Edit, copy and delete
- **Edit sample / Delete sample**: from Sample summary. Deleting asks for confirmation and removes the sample's tissues, blocks and tests.
- **Copy samples**: copies blocks and tissues from a chosen source sample to one or more target samples. The source is unchanged.
- **Copy submission**: Home → View submissions → select a submission → **Copy**. Header details, test selections, samples, tissues and blocks are copied to a new submission with new references.
- **Edit submission**: some fields are read-only once the submission has progressed; status rules apply.

### 6.5 Print
From **View submissions**, open **Print submission** or **Print submission notes** to produce a PDF.

### 6.6 View submissions
Home → **View submissions**. Filter, sort, export to Excel, and select a row to open it.

## 7. Laboratory (Histopathology User, Maintenance)

| Task | Menu option | Steps |
|---|---|---|
| Receive a submission | Receive submissions | Select a batch from the not-received list, check details, confirm receipt |
| Assign tissues to blocks | Assign tissues to blocks | Open a received batch, assign tissues to blocks, enter histology references |
| Edit status | Edit submission status | Select a batch, change status, save |
| Enter quality data | Enter quality data | Select a batch, record results per test, set date returned |
| QC notes | Edit QC notes | Find by reference, add or edit notes |
| Archive | Archive submission | Select a batch, archive blocks and tissues with a location |

Use the **Go** box on list pages to jump straight to an ID or reference.

## 8. Search and Reports (Histopathology User, Maintenance)

| Option | Purpose |
|---|---|
| Search submissions | Find submissions by sender, date, status, species and more |
| Search outputs | Analyse submissions by test; tick at least one test |
| Search by PM date | Find samples by post-mortem date |
| Search archive locations | Find archived material by location |
| Search block refs | Find blocks by reference |
| View samples | List samples with details |
| View old ICC_Sub data | Read imported legacy data |
| Unused histology refs | List histology references not yet used |
| Audit logs | View changes by date, user or submission |

Reports (PDF): Histology report, QC note, Submission notes.

## 9. Booking (Histopathology User, Maintenance)

Booking menu: **Book block ref**, **Book histology ref**, **Edit histology ref**. Enter the required range or reference and confirm. Check the values carefully; booked references are used on live records.

## 10. Administration (Maintenance)

- **User maintenance**: add or edit users, set group and area. Users cannot be deleted; deactivate them instead.
- **Pick list maintenance**: add or edit lookup items and species. Items cannot be deleted; deactivate them.
- **Edit sender/histology ref**: correct a sender or histology reference on an animal.

## 11. Troubleshooting

| Problem | Action |
|---|---|
| Option missing from home page | Your group does not allow it; contact an administrator |
| "Session expired" page | Go to the home page and sign in again |
| "Sorry, there is a problem with the service" | Note the time and page, then contact support |
| Cannot save a record | Read the error summary and fix each listed field |
| Submission not visible | It belongs to another area, or filters hide it |
| PDF or Excel does not download | Retry; if it fails again report the time and page |

## 12. Support

Contact your local administrator or the service desk with: your username, the page address, the time, and a description of what you did.
