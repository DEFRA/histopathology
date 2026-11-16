# Histopathology System — Knowledge Transfer (KT) Document

> Audience: engineers, testers and support staff taking over the Histo.Web (.NET 10) application.
> Companion docs: [HLD](HLD.md), [LLD](LLD.md), [Target-Architecture](Target-Architecture.md), [ADRs](ADR/), [Runbook](Runbook.md), [Test-Strategy](Test-Strategy.md), [User-Manual](User-Manual.md), [AI-Agent-Prompt-Library](AI-Agent-Prompt-Library.md).
> Note: [Runbook.md](Runbook.md) describes the legacy WebForms build. This document covers the migrated system.

---

## 1. Purpose and Background

The Histopathology System manages pathology submissions from creation through receipt, histology processing, quality control, reporting, dispatch and archiving. It was migrated from a VB.NET WebForms / .NET Framework 4.0 application (3 projects, 64 ASPX pages, 9 Crystal Reports, SQL auth) to a **C# / .NET 10 modular monolith** with GOV.UK Design System UI, Entra ID SAML SSO and Azure hosting.

Migration principle: **behaviour is preserved**. The legacy source (VB.NET) is the reference for any business-rule question.

## 2. Technology Stack

| Area | Choice |
|---|---|
| Runtime / UI | .NET 10, ASP.NET Core Razor Pages, GOV.UK Frontend |
| Data access | Dapper over stored procedures only (ADR-002, ADR-005) |
| Database | Azure SQL (`histopathology`), Managed Identity auth |
| Authentication | Entra ID via SAML 2.0 (ITfoxtec.Identity.Saml2), cookie session |
| State | Server-side session as state bus (ADR-001) |
| Reporting | Razor report views rendered to PDF with Playwright (ADR-004) |
| Logging / telemetry | Serilog + Azure Monitor / Application Insights (OpenTelemetry) |
| Tests | xUnit (+ Moq, FluentAssertions) in `tests/Histo.Tests`; browser SIT/regression agents in `tests/SIT` |
| Hosting | Container on Azure App Service (Linux), Key Vault for secrets |
| Solution | `HistopathologySystem.slnx` |

## 3. Solution Structure

```
src/
  Histo.Web              Razor Pages host: pages, auth, session, layout, Program.cs
  Histo.Submissions      Batch (submission), animal (sample), tissue domain
  Histo.Histology        Blocks and histology references
  Histo.QualityControl   QC notes and quality data
  Histo.Reporting        Report models/services and PDF rendering
  Histo.AuditLog         Audit log queries
  Histo.Administration   Users and pick lists
  Histo.Core             Shared domain types
  Histo.Infrastructure   IDbConnectionFactory, IAppLogger, AppOptions, Dapper type handlers
  Database/              Deploy scripts, Migrations, StoredProcedures
tests/
  Histo.Tests            Unit tests
  SIT                    Agent definitions and QA reports for browser testing
docs/                    HLD, LLD, ADRs, plans, run logs
```

### Dependency rules
- `Histo.Web` references every module.
- Domain modules reference `Histo.Infrastructure` (and `Histo.Core`) only; no cross-module references; none reference `Histo.Web`.
- Cross-module orchestration happens in `Histo.Web` services.
- Each module exposes a `*Module.cs` extension that registers its own internals; `Program.cs` calls them.

### Module internals pattern
`Interfaces/` (service and repository contracts) → `Services/` (business rules) → `Repositories/` (Dapper + stored procedure calls) → `Models/` (POCOs).

## 4. Key Entry Points in Histo.Web

| Item | Location |
|---|---|
| Startup / DI / middleware | `src/Histo.Web/Program.cs` |
| Base page model (session, access checks) | `Pages/HistoPageModel.cs`, `Pages/GridPageModel.cs` |
| Claims (group, user id, area) | `Auth/AppClaimTypes.cs`, `Auth/HistopathologyClaimsTransformation.cs` |
| Single sign-in enforcement | `Auth/SingleSessionPolicy.cs` |
| Maintenance-only page filter | `Auth/MaintenanceOnlyPageFilter.cs` |
| Security headers | `Security/SecurityHeaders.cs` |
| Session wrapper | `Services/SessionService` |
| Telemetry | `Telemetry/` |
| Excel export | `ExcelExportHelper.cs` |
| Config | `appsettings.json` (placeholders such as `__EXPORT_PATH__` replaced at deploy time) |

## 5. Page Areas and Business Domains

| Folder (`Pages/`) | Function |
|---|---|
| `Submissions` | AddSubmission, SampleSummary, SubmissionDetails, SubmissionDetailsBlock, ViewSubmissions |
| `Batches` | New/cassetted submission, BatchDetails, EditBatch, EditBatchTests, CopyBatch, Receive, DateReturned, Dispatch/Archive lists, Print submission, status editing, on-hold |
| `Blocks` | BlockDetails, CopyBlocks, CopySamples |
| `Bookings` | Book block ref, book histology ref, edit histology ref |
| `QC` | QC notes, quality data |
| `Search` | Submissions, outputs (tests), PM dates, archive location, block refs, sender, samples, unused histology refs, imported data |
| `Reports` | HistologyReport, QCNote, SubmissionNotes (PDF) |
| `AuditLog` | By date, user, submission |
| `Archive` | Archive blocks and tissues |
| `Admin` | User maintenance, pick lists, species, animal ref edit, fix completed dates |
| `Errors`, `Help`, `AccessDenied`, `SessionError` | Supporting pages |

### Domain model (simplified)
`Batch (Submission)` 1—* `BatchSubmission/Animal (Sample)` 1—* `Tissue` / `Block`; batch-level test selections (Histology, Antibodies, Special Stains) are templates inherited by blocks. A sender reference identifies an animal and is **shared across submissions** — reusing it changes data on earlier submissions.

## 6. Security and Access Model

- Sign-in: Entra ID SAML (`/Saml2/Acs`, `/Saml2/Logout`), `ForceAuthn=true`.
- After sign-in, claims are built from `tblUser`: group name, user id, group id, area name/id.
- **Groups:** Customer, Histopathology User, Maintenance (see [User-Manual](User-Manual.md) for the permissions matrix).
- Area restriction: Histopathology Users and Maintenance users see batches according to `BatchAccessDecision` and the user's area.
- Single session per user (`SessionStamp` claim); idle timeout `Session:IdleTimeoutMinutes` (default 120) applies to cookie and session.
- Cookies are `Secure`; antiforgery is enforced on POST; security headers are added globally.
- Secrets: never commit credentials. The DB uses Managed Identity; the legacy plaintext SQL password must not be reintroduced.

## 7. Local Development

Prerequisites: .NET 10 SDK, Node (for `package.json` front-end assets), access to the dev Azure SQL (Entra login) or a local copy of the DB, Playwright browsers for PDF generation.

```powershell
dotnet restore HistopathologySystem.slnx
dotnet build HistopathologySystem.slnx
dotnet run --project src\Histo.Web --launch-profile Histo.Web   # https://localhost:57879/
dotnet test tests\Histo.Tests\Histo.Tests.csproj
```

Local configuration: use user-secrets or `appsettings.Development.json` for `ConnectionStrings:HistologyDb`, `Saml2:*`, `AppSettings:ExportPath`. Set `DataProtection:KeysPath` to a shared path when running more than one instance.

## 8. Deployment and Environments

| Env | URL | Notes |
|---|---|---|
| Dev | `https://dev-cde.azure.defra.cloud` | SAML SP entity id for dev |
| TST | `https://tst-cde.azure.defra.cloud` | SIT / regression target |

Pipeline replaces `__…__` tokens in `appsettings.json`. Database changes are scripts in `src/Database/Migrations`, stored procedures in `src/Database/StoredProcedures`, deploy scripts in `src/Database/Deploy`.

## 9. Testing

- **Unit:** `tests/Histo.Tests` (for example `Unit/CopyBatchModelTests.cs`). Run with `dotnet test`.
- **SIT / regression:** agent definitions in `tests/SIT/*.agent.md` (`regression`, `end-to-end`, `submissions`, `laboratory`, `booking`, `search-and-reports`, `administration`, `audit-log`). Tiers: Smoke → Core → Full; benchmark dated 2026-11-10 on TST.
- SIT rules: one run stamp `SIT<yyyyMMddHHmmss>`, new sender ref per run, only modify data created by the run, deactivate (never delete) created users/pick-list items.
- Strategy: [Test-Strategy.md](Test-Strategy.md); traceability: [Functionality-Traceability-Matrix.md](Functionality-Traceability-Matrix.md); parity: [Parity-Audit-Report.md](Parity-Audit-Report.md).

## 10. Architecture Decisions

| ADR | Decision |
|---|---|
| 001 | Session as state bus (replaces WebForms session keys) |
| 002 | Stored procedures are the exclusive data-access route |
| 003 | Per-page role check |
| 004 | PDF generation (replaces Crystal Reports) |
| 005 | Dapper + stored procedures |
| 006 | Manual login page bridge |

## 11. Known Issues and Design Decisions

- **Submission created before first sample:** the batch row is persisted at the initial details submit so samples can attach to a `BatchId`. Legacy deferred creation until ≥1 sample existed. A redesign is proposed in [TSE-NonTSE-Submission-Workflow-Redesign.md](TSE-NonTSE-Submission-Workflow-Redesign.md); needs a product decision.
- **Finish → Print Submission redirect** depends on the item above.
- **Copy Sample** is a deliberate single-step simplification of the legacy 3-page wizard; it can copy from any submission, not only the same batch.
- **Post-fixation / Submitted-As** are not separate tables in the new model (no copy needed).
- **Dates:** some legacy stored procedures return `dd/MM/yyyy` strings; handled by `DateTimeTypeHandler` / `NullableDateTimeTypeHandler`. Register them before any Dapper call.
- **Audit log:** column `DateTime` is mapped to `AuditLogEntry.ChangedAt` in `AuditLogDapperSetup`.
- **Lookup data:** users and pick-list items cannot be deleted, only deactivated.
- Mouse Bioassay / Neuropath removal: see [Mouse-Bioassay-Neuropath-Removal-Analysis.md](Mouse-Bioassay-Neuropath-Removal-Analysis.md).

## 12. Operations and Troubleshooting

| Symptom | Check |
|---|---|
| Redirect loop or SSO failure | Saml2 entity ids / reply URLs match Entra enterprise app; IdP metadata URL reachable |
| `Access denied` page | User exists in `tblUser`, group and area are set |
| Unexpected `SessionError` | Idle timeout elapsed or user signed in elsewhere (single-session policy) |
| HTTP 500 / "Sorry, there is a problem with the service" | Application Insights traces and Serilog console output by time; reproduce locally |
| PDF report fails | Playwright browsers installed in the container image |
| Excel export fails | `AppSettings:ExportPath` exists and is writable |
| Sessions lost after restart or scale-out | Configure `DataProtection:KeysPath` on shared storage |
| Wrong dates | Dapper type handlers registered; SP date format |

## 13. Working Conventions

- Follow the GOV.UK pattern: progressive disclosure instead of disabled buttons, error summary + inline errors, back links, page titles.
- Compare with legacy source (`HistopathologySystem`, VB.NET) before changing a business rule; record the decision in `docs/` (see [migration-run-journal.md](migration-run-journal.md), [run-log-v2.md](run-log-v2.md)).
- New behaviour requires a unit test; verify with `dotnet build` and `dotnet test`.
- Prompts to the migration agent are logged in [User-Prompts-Log.md](User-Prompts-Log.md) and [DotNet-Migration-agent.md](../DotNet-Migration-agent.md).

## 14. KT Session Checklist

- [ ] Walk through solution structure and dependency rules
- [ ] Run the app locally and sign in
- [ ] Trace one journey end to end (New submission → Sample → Block → Print)
- [ ] Review the stored procedure and migration scripts
- [ ] Run unit tests and the Smoke SIT tier
- [ ] Review known issues (section 11) and open decisions
- [ ] Confirm access to Azure resources, Entra app, App Insights, pipelines
- [ ] Hand over contacts for product owner, DBA, Entra admin
