# Playbook 06: High-Complexity Review Brief

**Purpose**: Produce a structured, developer-actionable review brief for every Lane 3
(High-complexity) service. No handler code is generated. The brief documents the migration
risk, Windows-specific surface that must be redesigned, data-access dependencies, and a
step-by-step manual migration checklist requiring developer sign-off.

**Input**: Lane 3 service list from `migration-plan.json`, original service source files  
**Output**: `migration-output/review-briefs/<ServiceName>.md` per High-complexity service

> **No code is generated in this skill.** All output is documentation only.

---

## Step 1 — Load High-Complexity Services

From `migration-output/migration-plan.json`, extract all `servicePlans` where `lane: 3`.
For each, load:
- The original service source files (`.cs` / `.vb`)
- The `service-inventory.json` entry for this service
- The assigned developer name (`assignedDeveloper` field)

---

## Step 2 — Produce Review Brief

For each High-complexity service, write
`migration-output/review-briefs/<ServiceName>.md` using the template below.

---

### Template: High-Complexity Migration Review Brief

```markdown
# Migration Review Brief: <ServiceName>

**Migration Programme**: Agentic Cloud Migration  
**Service**: <ServiceName> (`<projectFilePath>`)  
**Complexity Grade**: High  
**Assigned Developer**: <assignedDeveloper>  
**Brief Generated**: <YYYY-MM-DD>  
**Status**: ⚠️ AWAITING DEVELOPER SIGN-OFF — no automated code generation until approved

---

## 1. Service Summary

**Business Purpose**  
[Describe what this service does in business terms: what data it processes, when it runs,
what downstream systems depend on it. Derived from class/method names, comments, and SP names.]

**Original Trigger Type**  
[e.g. System.Timers.Timer at 600,000ms intervals — fires every 10 minutes]

**Proposed Lambda Trigger**  
[e.g. EventBridge Scheduler `rate(10 minutes)` — PENDING developer confirmation]

---

## 2. Windows-Specific API Surface (Must Be Replaced)

List every Windows-only API call found in the service. For each, state the file path,
the line where it appears, and the recommended replacement.

| API Call | File | Line | Recommended Replacement | Risk |
|----------|------|------|------------------------|------|
| `EventLog.WriteEntry(...)` | `ServiceClass.cs` | 45 | `ILambdaLogger.LogInformation(...)` | Low |
| `Registry.GetValue(...)` | `ConfigHelper.cs` | 12 | `Environment.GetEnvironmentVariable(...)` via SSM Parameter Store | Medium |
| `System.IO.Pipes.NamedPipeServerStream` | `PipeListener.cs` | 78 | Architectural redesign required — see Section 4 | High |

---

## 3. Data-Access Dependencies

### 3.1 Connection Strings

| Config Key | Provider | Proposed Secret ARN |
|-----------|---------|---------------------|
| `MainDbConnection` | `System.Data.SqlClient` | `<secretsPathPrefix><ServiceName>/MainDbConnection` |

### 3.2 Stored Procedure Calls

List all stored procedures found in the service. These must be preserved verbatim in the
migrated handler.

| Stored Procedure | Parameters | Transaction? | Bulk Copy? | Notes |
|-----------------|-----------|-------------|-----------|-------|
| `usp_ProcessLegacyBatch` | `@BatchId INT, @RunDate DATETIME` | Yes (`SqlTransaction`) | No | Called in main processing loop |
| `usp_ArchiveProcessed` | `@BatchId INT` | No | No | Called after successful batch |

### 3.3 MSDTC / Distributed Transaction Assessment

[If MSDTC is present, describe the scope: which databases are enlisted, whether the
transaction spans service boundaries. Lambda does not support MSDTC — the developer must
either collapse the distributed transaction into a compensating transaction pattern or
introduce a two-phase commit via Step Functions.]

---

## 4. Complexity Blockers

List each reason this service was classified High complexity, and the required design
decision before migration can proceed.

| Blocker | Impact | Required Decision |
|---------|--------|------------------|
| Named pipe listener (`NamedPipeServerStream`) | Service receives requests via named pipe from another Windows process | Determine which callers use the pipe; redesign as SQS queue + Lambda trigger or API Gateway |
| MSDTC distributed transaction across `MainDb` and `AuditDb` | Lambda cannot enlist in DTC | Redesign as compensating transaction or delegate to Step Functions |
| `SqlBulkCopy` of 500,000 rows — worst-case runtime ~25 min | Exceeds Lambda 15-min timeout | Implement SQS pagination: 5,000 rows per invocation = ~5,000 invocations at ~18s each |

---

## 5. Recommended Migration Approach

Provide a step-by-step migration guide specific to this service's blockers.
This is guidance, not generated code — the assigned developer follows these steps manually.

### Step A — Resolve Named Pipe Dependency (if applicable)
1. Identify all callers of the named pipe server (search solution for `NamedPipeClientStream` connecting to this service's pipe name).
2. For each caller: replace the named pipe call with an SQS `SendMessage` call to a new `<ServiceName>-input` queue.
3. Replace `NamedPipeServerStream` in this service with an SQS `ReceiveMessage` Lambda trigger.
4. Test caller → SQS → Lambda invocation end-to-end before decommissioning the pipe.

### Step B — Resolve MSDTC (if applicable)
1. Audit which operations within the transaction must be atomic.
2. If the `AuditDb` insert can be made idempotent (insert-if-not-exists), remove it from the DTC scope and promote it to a post-commit step.
3. If true atomicity is required across databases, implement a Step Functions workflow with a compensating transaction on failure.
4. Test rollback scenarios before removing the `TransactionScope`.

### Step C — Resolve Bulk Copy Timeout Risk (if applicable)
1. Wrap `SqlBulkCopy` in a paginated loop: read N rows at a time using `OFFSET / FETCH NEXT` or a `BatchQueue` status column.
2. Write the current page offset/cursor to a DynamoDB tracking table or SQS message attribute.
3. On Lambda timeout, the next invocation reads the cursor and continues from where the previous run left off.
4. Validate idempotency: if the same page is processed twice, no duplicate records must be inserted.

### Step D — Standard Handler Generation (after blockers resolved)
Once all blockers are resolved, the assigned developer invokes:
```
@windows-service-to-lambda generate-handlers
```
and manually downgrades this service to Lane 2 in `migration-plan.json` for supervised
automated generation.

---

## 6. Manual Migration Checklist

The assigned developer must complete and sign off each item before handler generation proceeds.

- [ ] All Windows-specific API calls replaced or removal plan documented (Section 2)
- [ ] Named pipe dependency resolved (Section 5A) — or confirmed as not applicable
- [ ] MSDTC removed or compensating transaction pattern implemented (Section 5B) — or confirmed as not applicable
- [ ] Bulk copy pagination strategy confirmed and tested in isolation (Section 5C) — or confirmed as not applicable
- [ ] All stored procedure names and parameters verified against the live database schema
- [ ] Lambda timeout for this service set and confirmed in `migration-plan.json`
- [ ] Connection string keys mapped to Secrets Manager paths
- [ ] Developer sign-off: **[ ] APPROVED FOR HANDLER GENERATION**

**Sign-off name**: ________________________________  
**Sign-off date**: ________________________________

---

## 7. Risk Register

| Risk | Severity | Likelihood | Mitigation |
|------|---------|-----------|-----------|
| Named pipe callers not identified | High | Medium | Full solution search for `NamedPipeClientStream` required |
| DTC removal introduces data inconsistency | Critical | Low | Compensating transaction + regression test suite required |
| Bulk copy pagination causes duplicate rows | High | Medium | Idempotency key on target table required |
```

---

## Step 3 — Catalogue All Review Briefs

After producing all review briefs, write a summary entry in the migration report data
(used by Skill 07):

```json
{
  "highComplexityServices": [
    {
      "name": "<ServiceName>",
      "briefPath": "migration-output/review-briefs/<ServiceName>.md",
      "blockerCount": 3,
      "assignedDeveloper": "<name>",
      "signedOff": false
    }
  ]
}
```

> **Do not proceed to handler generation for any High-complexity service until
> `signedOff: true` is recorded.** The migration reporting skill will flag all unsigned
> services as deferred in the final report.
