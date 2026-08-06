# Playbook 07: Migration Reporting

**Purpose**: Collect all outputs from Skills 01–06 and produce a single self-contained
HTML migration report. This skill always runs last, even when preceding skills partially
blocked — incomplete sections are marked "Deferred / Manual Gate" in the report.

**Input**: All previous skill output files  
**Output**: `migration-output/migration-report-<YYYY-MM-DD>.html`

---

## Step 1 — Collect All Skill Outputs

Load the following files. For any file that does not exist (skill was blocked or skipped),
mark the corresponding report section as "Deferred — manual gate required."

| File | Source Skill | Used In Report Section |
|------|-------------|----------------------|
| `migration-output/service-inventory.json` | Skill 01 | Service Inventory |
| `migration-output/migration-plan.json` | Skill 02 | Migration Decisions, Phase Breakdown |
| `migration-output/handlers/*/Function.cs` | Skill 03 | Generated Handlers Summary |
| `migration-output/terraform/**/main.tf` | Skill 04 | IaC Generation Summary |
| `migration-output/cloudformation/**/template.yaml` | Skill 04 | IaC Generation Summary |
| `migration-output/tests/**/HandlerTests.cs` | Skill 05 | Test Coverage Summary |
| `migration-output/review-briefs/*.md` | Skill 06 | High-Complexity Services |

---

## Step 2 — Produce the HTML Report

Write `migration-output/migration-report-<YYYY-MM-DD>.html` as a **self-contained file**
(all CSS inline, no external dependencies). Include the following sections.

### Section 1 — Executive Summary

| Field | Value |
|-------|-------|
| Migration Programme | Agentic Cloud Migration |
| Report Date | `<YYYY-MM-DD>` |
| Services Inventoried | `<summary.total>` |
| Services Migrated (Lanes 1+2) | `<count of Lane 1 + Lane 2 services with completed handlers>` |
| Services Deferred (Lane 3) | `<count of Lane 3 services>` |
| Blocking Issues | `<count of blocking issues across all skills>` |
| IaC Toolchain | `<iacToolchain>` |
| Lambda Runtime | `<lambdaRuntime>` |
| Target AWS Region | `<awsRegion>` |

Include a traffic-light status per service:
- 🟢 **Completed** — handler, IaC, and tests generated; no blocking issues
- 🟡 **Review Gate** — Lane 2 service; developer review pending before IaC apply
- 🔴 **Deferred** — Lane 3; review brief produced; awaiting sign-off
- ⚫ **Blocked** — a skill encountered an unresolvable error

### Section 2 — Service Inventory Summary

Table: Service Name | Language | Source Framework | Complexity | Lane | Trigger Type | Timeout Risk

For each service, include a row. Highlight High-complexity rows in amber.

### Section 3 — Migration Decisions

Record all decisions captured in `migration-plan.json → decisions`:
- IaC toolchain, Lambda runtime, AWS region
- VPC configuration
- Secrets backend and path prefix
- DLQ retention and alerting thresholds
- Per-service schedule expressions

### Section 4 — Generated Artefacts Per Service

For each Lane 1 / Lane 2 service, produce a summary card:

```
┌─────────────────────────────────────────────────────────────┐
│  BatchProcessorService                          ✅ Completed │
├─────────────────────────────────────────────────────────────┤
│  Original Trigger  │ System.Timers.Timer @ 300,000ms         │
│  Lambda Trigger    │ EventBridge rate(5 minutes)              │
│  Runtime           │ dotnet10                                 │
│  Timeout           │ 300s                                     │
│  Memory            │ 256 MB                                   │
│  IaC               │ Terraform — main.tf + variables.tf       │
│  Tests             │ xUnit — HandlerTests.cs (3 tests)        │
│  Stored Procs      │ usp_ProcessBatch                         │
│  MIGRATED FROM     │ 4 Windows API calls replaced             │
│  TODO Comments     │ 0 manual review items                    │
└─────────────────────────────────────────────────────────────┘
```

### Section 5 — Pattern Mapping Applied

Table listing every Windows-to-Lambda pattern substitution made across all handlers:

| Original Pattern | Lambda Equivalent | Occurrences | Services Affected |
|-----------------|-------------------|-------------|------------------|
| `System.Timers.Timer` | EventBridge Scheduler `rate(...)` | 3 | BatchProcessor, ReportDispatch |
| `EventLog.WriteEntry` | `ILambdaLogger.LogInformation` | 7 | BatchProcessor, ReportDispatch |
| `ConfigurationManager.AppSettings` | `Environment.GetEnvironmentVariable` | 12 | All |

### Section 6 — Timeout Risk Analysis

For each service flagged with `timeoutRisk: true`:
- Original batch loop description
- Worst-case runtime estimate
- Mitigation chosen (`sqs-pagination` / `step-functions` / `increase-batch-filter`)
- Implementation status (Applied in handler / Deferred to manual implementation)

### Section 7 — High-Complexity Services (Lane 3)

For each High-complexity service, include:
- Service name and assigned developer
- Blocker count and blocker descriptions
- Link to review brief (`migration-output/review-briefs/<ServiceName>.md`)
- Sign-off status (Pending / Approved)

### Section 8 — Blocking Issues

Table of all blocking issues raised across skills:

| Service | Skill | Issue | Recommended Resolution |
|---------|-------|-------|----------------------|
| `<ServiceName>` | Skill 03 Handler Generation | Build error CS0246: type `CustomType` not found | Add NuGet reference for the assembly containing `CustomType` |

### Section 9 — Next Steps

Checklist:
- [ ] Review and merge generated handlers into source repository feature branch
- [ ] Enable `schedule_enabled = true` in Terraform / CloudFormation **only after** smoke test passes
- [ ] Provision Secrets Manager secrets with production values (out of scope for this agent)
- [ ] Run `dotnet test` test suites against the test environment
- [ ] Complete developer sign-off on all Lane 3 review briefs
- [ ] Decommission original Windows Services after Lambda smoke test passes
- [ ] Add `migration-output/` to `.gitignore` if not already present

### Section 10 — Appendix: Generated File Inventory

Complete flat list of all files produced by this migration run, with relative paths.

---

## Step 3 — Add migration-output/ to .gitignore

Check the repository root `.gitignore`. If `migration-output/` is not already listed, append it:

```
# Windows Service → Lambda migration outputs (generated artefacts — do not commit)
migration-output/
```

---

## Step 4 — Confirm Report Written

Print the absolute path of the written HTML report file and confirm:
```
Migration report written: migration-output/migration-report-<YYYY-MM-DD>.html
```
