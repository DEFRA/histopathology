# AI Agent Prompt Library — Histopathology System

Reusable prompts for AI coding and testing agents working on this repository. Replace `<placeholders>` before use.

## 0. How to Use

- Give the agent a goal, scope, constraints and a required output (see template in section 9).
- Point it to the relevant docs and files instead of pasting large content.
- Always ask it to build and test after changes: `dotnet build HistopathologySystem.slnx`, `dotnet test tests\Histo.Tests\Histo.Tests.csproj`.
- Log notable prompts in [User-Prompts-Log.md](User-Prompts-Log.md).

### Standing context (prepend to any prompt)

```
Context: C# / .NET 10 modular monolith (HistopathologySystem.slnx) migrated from a VB.NET WebForms app.
UI: ASP.NET Core Razor Pages with GOV.UK Frontend. Data: Dapper + stored procedures only.
Modules: Histo.Web, Histo.Submissions, Histo.Histology, Histo.QualityControl, Histo.Reporting,
Histo.AuditLog, Histo.Administration, Histo.Core, Histo.Infrastructure.
Rules: domain modules must not reference each other or Histo.Web; behaviour must match legacy;
no secrets in code; keep changes minimal; add unit tests for new behaviour.
Legacy source: C:\Sabari\Dev\Histo\HistopathologySystem (VB.NET). Docs: docs\HLD.md, docs\LLD.md, docs\ADR\.
```

## 1. Onboarding and Understanding

| ID | Prompt |
|---|---|
| ON-01 | `Explain the architecture of this solution using docs\Target-Architecture.md and the project files. Include module dependencies and request flow for a Razor Page that loads a batch.` |
| ON-02 | `Trace the "<journey>" journey (for example Create submission → Add sample → Add block → Print) through Razor Pages, services, repositories and stored procedures. List files in order.` |
| ON-03 | `Find where <business rule> is enforced (page model, service, SP, client script) and show the exact locations.` |
| ON-04 | `List all pages in src\Histo.Web\Pages for <folder>, with the user groups allowed and the services each page calls.` |
| ON-05 | `Summarise ADR-00<n> and show where in code it is applied.` |

## 2. Legacy Parity and Migration

| ID | Prompt |
|---|---|
| MG-01 | `Compare legacy <Page>.aspx(.vb) with src\Histo.Web\Pages\<Area>\<Page>.cshtml(.cs). Report differences in fields, validation, button enablement, navigation and data written. Classify each as Defect, Intentional change or No issue, with severity.` |
| MG-02 | `Migrate legacy page <Page>.aspx to Razor Pages following the GOV.UK pattern in docs\GovUK-Frontend-Upgrade-Pattern.md. Keep behaviour identical, use existing services, add a unit test, and update docs\Functionality-Traceability-Matrix.md.` |
| MG-03 | `Convert legacy stored procedure call <ClassName.Method> into a repository method using Dapper, following ADR-002 and ADR-005. Do not use inline SQL.` |
| MG-04 | `Audit the "<journey>" journey against legacy. Produce: journey assessment, gaps (functional, business rule, validation, navigation), defects with severity, remediation, and a verdict (Fully / Partially / Not aligned). Do not change code yet.` |
| MG-05 | `Update docs\Parity-Audit-Report.md and docs\Functionality-Traceability-Matrix.md with the results of the latest changes.` |

## 3. Feature Development

| ID | Prompt |
|---|---|
| FD-01 | `Add <feature> to <page>. Follow existing page model patterns (HistoPageModel, Session, ReturnPage). Show validation with the GOV.UK error summary. Add unit tests. Run build and tests.` |
| FD-02 | `Add a new service method <Name> to <Module>, with interface, Dapper repository method calling stored procedure <usp_Name>, DI registration in <Module>Module.cs, and tests with mocked repository.` |
| FD-03 | `Add a new stored procedure <usp_Name> under src\Database\StoredProcedures plus a migration script under src\Database\Migrations. Keep it idempotent.` |
| FD-04 | `Add a PDF report <Name> in Histo.Reporting using the existing Razor + Playwright pattern; match the legacy Crystal Report layout in <rpt file>.` |
| FD-05 | `Add Excel export to <page> using ExcelExportHelper, matching the columns shown in the table.` |

## 4. Bug Fixing

| ID | Prompt |
|---|---|
| BF-01 | `Bug: <description>. Steps: <steps>. Expected: <expected>. Actual: <actual>. Find the root cause, compare with legacy behaviour, fix minimally, add a regression test, and run build and tests. Report the cause and the change.` |
| BF-02 | `Investigate HTTP 500 on <url> at <time>. Check the page model, service, SP parameters and Dapper type mapping. Propose and apply a fix.` |
| BF-03 | `Dates display or save incorrectly on <page>. Check DateTimeTypeHandler / NullableDateTimeTypeHandler registration and the stored procedure date format (dd/MM/yyyy strings).` |
| BF-04 | `Verify whether this reported defect is reproducible by reading code only: <defect>. Cite files and lines. Do not change code unless confirmed.` |

## 5. Code Review and Quality

| ID | Prompt |
|---|---|
| CR-01 | `Review the changes on the current branch for correctness against legacy, module boundary violations, security (OWASP Top 10), and missing tests.` |
| CR-02 | `Check src\Histo.Web for pages missing access checks, antiforgery, input validation, or that expose data outside the user's area (see BatchAccessDecision).` |
| CR-03 | `Run a GOV.UK compliance pass on <page>: title, heading hierarchy, back link, form controls, error summary, focus on error, keyboard use, labels and hints.` |
| CR-04 | `Find hardcoded secrets, connection strings or plaintext credentials in the repository and report them without printing the secret values.` |

## 6. Testing

| ID | Prompt |
|---|---|
| TS-01 | `Write xUnit tests for <Class.Method> in tests\Histo.Tests\Unit using Moq and FluentAssertions. Cover the happy path, not-found, validation failure and legacy edge cases.` |
| TS-02 | `Run unit tests and summarise failures with probable cause. Do not edit tests to make them pass without explaining why.` |
| TS-03 | `Run the regression agent (tests\SIT\regression.agent.md) at <smoke|core|full> tier against <TST URL | https://localhost:57879/>. Report PASS / FAIL / REGRESSION / FIXED / DRIFT per case.` |
| TS-04 | `Run the <submissions|laboratory|booking|search-and-reports|administration|audit-log|end-to-end> SIT agent from tests\SIT and report defects with severity and evidence.` |
| TS-05 | `Create SIT test cases for <journey> in the same format as tests\SIT\*.agent.md, with a run stamp, a unique sender ref, and cleanup rules (deactivate, do not delete).` |
| TS-06 | `Compare the legacy and new PDF output for <report> (tests\SIT\e2e-compare and docs\pdf-op-compare) and list layout or data differences.` |

SIT rules to include whenever data is created: one stamp `SIT<yyyyMMddHHmmss>`, new sender ref per run, only touch own data, no Receive/Save/Dispatch/Archive on others' data, never request or type credentials (user signs in to Entra).

Authentication and access model for agents: the app-valid production combinations are `Customer -> External Customer` or `TB Diagnostics`, `Histopathology User -> Histopath`, and `Maintenance -> Other VLA`. A local harness account such as `Local Developer` with `Group: Maintenance, Area: All Areas` may be used for convenience in test environments, but it is not a production entitlement and should not be assumed for access-control scenarios. When a suite is testing user group or area restrictions, set the persona to the valid combination under test instead of defaulting to Maintenance/All Areas.

## 7. Database

| ID | Prompt |
|---|---|
| DB-01 | `List the stored procedures used by <module/repository> with parameters and the tables they touch.` |
| DB-02 | `Review stored procedure <usp_Name> for SQL injection, missing transactions, and performance (missing indexes, scans).` |
| DB-03 | `Write an idempotent migration that adds <column/table> and updates the affected stored procedures and Dapper models.` |

## 8. Documentation, DevOps and Operations

| ID | Prompt |
|---|---|
| DO-01 | `Update docs\LLD.md for the changes in <module>. Keep the existing format; do not duplicate content from other docs.` |
| DO-02 | `Write an ADR for <decision> in docs\ADR using the existing ADR format (context, decision, consequences).` |
| DO-03 | `Review Program.cs for security headers, cookie settings, session timeout, forwarded headers and data protection key persistence for multi-instance hosting.` |
| DO-04 | `Prepare release notes from the git log between <tagA> and <tagB>, grouped as Features, Fixes and Database changes.` |
| DO-05 | `Diagnose SSO failure: compare Saml2 settings in appsettings.json with the Entra enterprise app (identifier, reply URL, logout URL, metadata URL) and list mismatches.` |
| DO-06 | `Update docs\KT-Document.md and docs\User-Manual.md to reflect the changes in this branch.` |

## 9. Prompt Template

```
Goal: <one sentence outcome>
Scope: <files, pages, modules; what is out of scope>
Reference: <legacy file, doc, ADR, ticket>
Constraints: <behaviour parity, no new dependencies, minimal change, GOV.UK pattern>
Steps (optional): <ordered steps>
Verification: dotnet build + dotnet test; <SIT tier or manual check>
Output: <summary, defect table with severity, changed files, test results>
Do not: <commit/push, delete data, change unrelated files>
```

## 10. Guardrails for Agents

- Read before editing; make minimal, reversible changes.
- Ask before destructive or shared-system actions (delete, force push, reset, deploy, data changes on shared environments).
- Never print or request secrets or credentials; users sign in to Entra themselves.
- Do not bypass module boundaries or put inline SQL in code.
- Treat legacy behaviour as the source of truth; flag, don't silently change, deliberate differences.
- State what could not be verified (for example live legacy comparison, screen reader testing).
- Report outcomes honestly, including failing tests and unresolved defects.
