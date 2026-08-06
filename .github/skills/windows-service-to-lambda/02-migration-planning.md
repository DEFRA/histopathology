# Playbook 02: Migration Planning

**Purpose**: Read the approved `service-inventory.json`, interview the user to capture all
AWS-specific decisions, assign each service to a complexity lane, and produce a
`migration-plan.json` that drives all subsequent skills without further ambiguity.

**Input**: `migration-output/service-inventory.json`, user decisions via interview  
**Output**: `migration-output/migration-plan.json`

---

## Step 1 — Read and Validate the Approved Inventory

1. Load `migration-output/service-inventory.json`.
2. Confirm `summary.total` matches the number of entries in `services`.
3. Check that no service has `complexity: null` — any unclassified service must be classified
   before planning proceeds.
4. Extract the list of Low, Medium, and High complexity services as three separate groups.

---

## Step 2 — User Interview

Ask the following questions in order. Record every answer in `migration-plan.json` —
no decision may be left implicit or derived at a later step.

### Block A — IaC and Runtime

| # | Question | Allowed Values | Default |
|---|----------|---------------|---------|
| A1 | What IaC toolchain should be used? | `terraform`, `cloudformation` | — (required) |
| A2 | What Lambda runtime should be used? **Auto-detect** by running `dotnet --list-sdks` on the local machine, selecting the highest major version, and mapping to the runtime identifier (e.g. SDK 10.x → `dotnet10`). Override only if the target Lambda execution environment differs from the local SDK. | `dotnet10` | Auto-detected from local SDK |
| A3 | What is the target AWS region? | Any valid AWS region code, e.g. `eu-west-2` | — (required) |

### Block B — Networking

| # | Question | Allowed Values | Default |
|---|----------|---------------|---------|
| B1 | Should Lambda functions run inside a VPC? | `yes`, `no` | `no` |
| B2 | *(If B1 = yes)* Provide subnet IDs (comma-separated) | `subnet-xxxxxxxx,...` | — |
| B3 | *(If B1 = yes)* Provide security group ID | `sg-xxxxxxxx` | — |

> If the service connects to RDS, confirm that the Lambda VPC config allows outbound TCP
> on the database port (1433 for SQL Server, 1521 for Oracle, 5432 for PostgreSQL).

### Block C — Secrets and Configuration

| # | Question | Allowed Values | Default |
|---|----------|---------------|---------|
| C1 | Secrets backend? | `secrets-manager`, `ssm-parameter-store` | `secrets-manager` |
| C2 | Secrets Manager path prefix (if C1 = secrets-manager) | e.g. `/prod/batchprocessor/` | `/prod/<ServiceName>/` |
| C3 | SSM path prefix (if C1 = ssm-parameter-store) | e.g. `/prod/batchprocessor/` | `/prod/<ServiceName>/` |

### Block D — Dead Letter Queue

| # | Question | Allowed Values | Default |
|---|----------|---------------|---------|
| D1 | SQS DLQ message retention period (days)? | 1–14 | `14` |
| D2 | Alert on DLQ message count? | `yes`, `no` | `yes` |
| D3 | *(If D2 = yes)* Alert threshold (number of messages before alarm triggers)? | Integer ≥ 1 | `1` |

### Block E — Scheduling

For each service with `triggers[].type == "SystemTimersTimer"` or `"ThreadSleepLoop"`:

| # | Question | Allowed Values |
|---|----------|---------------|
| E1 | Confirm or adjust the proposed EventBridge schedule expression for `<ServiceName>`? | Confirm / provide corrected `rate(...)` or `cron(...)` expression |
| E2 | Should the EventBridge rule be enabled immediately on deployment? | `yes`, `no` (default: `no` for safety) |

### Block F — Batch / Timeout Risk Services

For each service where `timeoutRisk: true`:

| # | Question | Allowed Values |
|---|----------|---------------|
| F1 | Mitigation approach for `<ServiceName>` (timeout risk confirmed)? | `sqs-pagination`, `step-functions`, `increase-batch-filter` |
| F2 | *(If sqs-pagination)* Maximum records per Lambda invocation? | Integer |
| F3 | *(If step-functions)* Note: Step Functions authoring is out of scope for this agent — confirm manual implementation will be handled separately? | `confirmed` |

### Block G — High-Complexity Services

For each service where `complexity: "High"`:

| # | Question | Allowed Values |
|---|----------|---------------|
| G1 | Confirm that `<ServiceName>` will receive a review brief only (no auto-generation)? | `confirmed` |
| G2 | Who is the assigned developer for manual migration of `<ServiceName>`? | Free text (name) |

---

## Step 3 — Assign Lanes

Based on the complexity grades from `service-inventory.json` and the user interview:

| Complexity | Lane | Treatment |
|-----------|------|-----------|
| Low | 1 | Handler, IaC, and test stubs generated without gates |
| Medium | 2 | Handler, IaC, and test stubs generated; developer review gate before IaC apply |
| High | 3 | Review brief only; no handler or IaC generated until sign-off |

Override lane assignment only if the user explicitly downgrades a Medium service to Lane 1
after confirming they have reviewed the relevant risk.

---

## Step 4 — Produce migration-plan.json

Write `migration-output/migration-plan.json`:

```json
{
  "planDate": "<ISO-8601 timestamp>",
  "decisions": {
    "iacToolchain": "terraform",
    "lambdaRuntime": "dotnet10",  // auto-detected from highest installed SDK (dotnet --list-sdks)
    "awsRegion": "eu-west-2",
    "vpcEnabled": false,
    "subnetIds": [],
    "securityGroupId": null,
    "secretsBackend": "secrets-manager",
    "secretsPathPrefix": "/prod/",
    "dlqRetentionDays": 14,
    "dlqAlertEnabled": true,
    "dlqAlertThreshold": 1
  },
  "phases": [
    {
      "phase": 1,
      "description": "Lane 1 — Low complexity services (fully automated)",
      "services": ["BatchProcessorService"]
    },
    {
      "phase": 2,
      "description": "Lane 2 — Medium complexity services (automated + review gate)",
      "services": ["ReportDispatchService"]
    },
    {
      "phase": 3,
      "description": "Lane 3 — High complexity services (review brief; manual migration)",
      "services": ["LegacyFileWatcherService"]
    }
  ],
  "servicePlans": [
    {
      "name": "BatchProcessorService",
      "lane": 1,
      "complexity": "Low",
      "timeoutRisk": false,
      "timeoutMitigation": null,
      "lambdaFunctionName": "batch-processor-service",
      "runtime": "dotnet10",
      "handler": "BatchProcessorService::BatchProcessorService.Function::FunctionHandler",
      "timeoutSeconds": 300,
      "memorySizeMb": 256,
      "scheduleExpression": "rate(5 minutes)",
      "scheduleEnabled": false,
      "secretReferences": ["/prod/batchprocessor/MainDbConnection"],
      "dlqName": "batch-processor-service-dlq",
      "iacFiles": {
        "terraform": "migration-output/terraform/BatchProcessorService/",
        "cloudformation": null
      },
      "testProject": "migration-output/tests/BatchProcessorService.Tests/",
      "reviewBrief": null,
      "assignedDeveloper": null
    }
  ]
}
```

**Decision gate**: Present the plan to the user. Confirm:
1. Are lane assignments correct?
2. Are schedule expressions accurate?
3. Are Lambda timeout values appropriate per service?
4. Is the DLQ and alerting configuration correct?

Do not proceed to handler generation until the user approves the plan.
