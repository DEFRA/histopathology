---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
skill:
  id: windows-service-to-lambda
  name: Windows Service to AWS Lambda Migration
  version: 1.0.0
  owner: Cloud Migration Engineering
  intent: >
    Migrates .NET Framework 4.x Windows Service executables to AWS Lambda serverless
    functions. Analyses service source code, classifies complexity, generates Lambda handlers
    (C# .NET 10 isolated worker), produces Terraform HCL or CloudFormation YAML
    for all supporting AWS infrastructure, and outputs integration test stubs with mocked AWS
    SDK clients. Generic and reusable — not tied to any specific application name, business
    domain, or database platform.
  recommended_agent: windows-service-to-lambda
  execution_model: instruction-only with sequential skill gates and complexity-tiered lanes
  scope:
    includes:
      - Windows Service source code scanning (ServiceBase derivatives, timers, event handlers)
      - Trigger classification (timer interval → EventBridge cron, file watcher → S3 event, etc.)
      - ADO.NET data-access extraction (stored procedures, connection string keys, database names)
      - Windows-specific API surface identification (EventLog, Registry, ServiceController)
      - Complexity classification (Low / Medium / High) with timeout risk analysis
      - Lambda handler generation for Low and Medium complexity services (.NET 10)
      - Windows-to-Lambda pattern adaptation (see references/pattern-mapping.md)
      - IaC generation: EventBridge Scheduler, Lambda function, IAM role, SQS DLQ, Secrets Manager bindings, CloudWatch log group and alarms (Terraform HCL or CloudFormation YAML)
      - Integration test stub generation with mocked AWS SDK clients (Moq)
      - Trigger payload synthesis from IaC schedule config
      - Baseline fixture capture test mode for data-parity validation
      - High-complexity service review brief production (no code generation)
      - Migration reporting (self-contained HTML)
    excludes:
      - Deployment to AWS (Lambda function deployment, CloudFormation stack creation, Terraform apply)
      - Secret value provisioning (Secrets Manager / SSM values are referenced, not populated)
      - Step Functions workflow authoring (flagged as a recommendation only)
      - RDS Proxy provisioning (connection strings are updated to point to RDS Proxy; the proxy itself is not provisioned)
      - Modification of High-complexity services without explicit developer sign-off
      - Automated migration of MSDTC distributed transactions (manual gate required)
      - Behavioural changes to stored procedure calls or SQL command text
      - Modification of *.designer.vb, *.designer.cs, or auto-generated installer files
  inputs_required:
    - solutionFolder: "Absolute path to the repository root containing the .sln file"
    - sourceFramework: "Framework moniker of the service projects, e.g. net40, net462, net48"
    - iacToolchain: "IaC output format — terraform or cloudformation"
    - lambdaRuntime: "Target Lambda runtime — dotnet10"
    - awsRegion: "AWS deployment region, e.g. eu-west-2"
  outputs:
    - "migration-output/service-inventory.json — service list, triggers, patterns, complexity grades, risk flags"
    - "migration-output/migration-plan.json — phase breakdown, decisions, effort, review gates"
    - "migration-output/handlers/<ServiceName>/ — Lambda handler source files"
    - "migration-output/terraform/<ServiceName>/ — main.tf, variables.tf, outputs.tf (if Terraform)"
    - "migration-output/cloudformation/<ServiceName>/ — template.yaml (if CloudFormation)"
    - "migration-output/tests/<ServiceName>/ — test project with stubs and fixture data"
    - "migration-output/review-briefs/<ServiceName>.md — High-complexity service review briefs"
    - "migration-output/migration-report-<YYYY-MM-DD>.html — self-contained HTML report"
  success_criteria:
    - All Low and Medium complexity services have generated handlers, IaC, and test stubs
    - All High-complexity services have a review brief; no handler code generated without sign-off
    - Generated handlers build with zero errors (dotnet build)
    - No plaintext secrets in any generated file
    - No Windows file paths (backslashes, drive letters) in generated handlers or IaC
    - migration-output/ added to .gitignore
  safety:
    - Never hardcode secrets of any kind — use Environment.GetEnvironmentVariable sourced from Secrets Manager.
    - Never generate handler code for High-complexity services without explicit developer sign-off.
    - Never use msbuild or nuget.exe to build generated handlers — they target .NET 10 SDK.
    - Never copy ServiceBase, ProjectInstaller, or System.ServiceProcess code into handlers.
    - Never emit Windows file paths in generated handlers or IaC files.
    - Never alter stored procedure names, parameter names, or SQL command text without sign-off.
    - Never exceed the 15-minute Lambda timeout without flagging and requiring chunking.
    - Always add migration-output/ to .gitignore.
---

# Windows Service to AWS Lambda Migration (Skill)

This skill package provides a repeatable, complexity-tiered migration workflow for lifting
.NET Framework 4.x Windows Services to AWS Lambda. It covers analysis, planning, handler
generation, IaC generation, test stub generation, and reporting.

Execution model: **instruction-only with sequential skill gates and complexity-tiered lanes**.

---

## Complexity Lanes

| Lane | Complexity | Approach |
|------|-----------|----------|
| Lane 1 | Low | Fully automated — handler, IaC, and tests generated without gates |
| Lane 2 | Medium | Automated generation + mandatory developer review gate before IaC apply |
| Lane 3 | High | Review brief only — no handler code generated; developer sign-off required |

---

## Skills (Playbooks)

| Order | Skill File | Purpose | Key Inputs | Key Outputs |
|-------|-----------|---------|------------|-------------|
| 1 | [01-service-inventory.md](./01-service-inventory.md) | Scan solution for Windows Services, classify triggers, extract data-access patterns, classify complexity | `solutionFolder`, `sourceFramework` | `service-inventory.json` |
| 2 | [02-migration-planning.md](./02-migration-planning.md) | Interview for IaC/runtime/AWS decisions, assign lanes, produce zero-ambiguity plan | `service-inventory.json`, user decisions | `migration-plan.json` |
| 3 | [03-handler-generation.md](./03-handler-generation.md) | Generate Lambda handler source files for Lane 1 and Lane 2 services | `migration-plan.json`, service source files | Handler source files per service |
| 4 | [04-iac-generation.md](./04-iac-generation.md) | Generate Terraform HCL or CloudFormation YAML for all migrated functions | `migration-plan.json`, handler metadata | IaC files per service |
| 5 | [05-test-stub-generation.md](./05-test-stub-generation.md) | Generate integration test stubs with mocked AWS SDK clients and trigger payload synthesis | Generated handler files, `migration-plan.json` | Test project per service |
| 6 | [06-high-complexity-review.md](./06-high-complexity-review.md) | Produce structured review brief for Lane 3 services — no code generation | High-complexity service list from `migration-plan.json` | Review brief per service |
| 7 | [07-migration-reporting.md](./07-migration-reporting.md) | Synthesise all outputs into a self-contained HTML report | All previous outputs | `migration-report-<YYYY-MM-DD>.html` |

> **Skill 7 always runs last**, even when preceding skills partially blocked.
> Incomplete sections are marked "Deferred / Manual Gate" in the report.

---

## Templates

- [Lambda Handler Template (.NET 10 C#)](./templates/lambda-handler-dotnet10.cs)
- [Terraform Module Template](./templates/terraform-lambda-module.tf)
- [CloudFormation Template](./templates/cloudformation-lambda.yaml)
- [xUnit Test Stub Template (.NET 10)](./templates/xunit-test-stub.cs)
- [High-Complexity Review Brief Template](./templates/high-complexity-review-brief.md)

## Reference Materials

- [Pattern Mapping: Windows Service → Lambda](./references/pattern-mapping.md)
- [.NET Framework 4.x Windows Service Patterns](./references/dotnet4-service-patterns.md)
- [ADO.NET to Lambda Data Access Guide](./references/adonet-lambda-data-access.md)
- [IAM Least-Privilege Policy Reference](./references/iam-policy-reference.md)
- [Complexity Classification Rules](./references/complexity-classification-rules.md)

---

## Execution Sequence

```
[START]
  │
  ▼
1. service-inventory    ← READ-ONLY; produces complexity grades and risk flags
  │  complexity = Low | Medium | High (per service)
  ▼
2. migration-planning   ← interview user; assigns lanes; records all decisions
  │  Lane 1 (Low) → auto-generate
  │  Lane 2 (Medium) → generate + review gate
  │  Lane 3 (High) → review brief only
  ▼
3. handler-generation   ← Lane 1 + Lane 2 only; High-complexity services skip to step 6
  │  Translates Windows patterns to Lambda equivalents (see references/pattern-mapping.md)
  ▼
4. iac-generation       ← all Lane 1 + Lane 2 services
  │  Terraform HCL or CloudFormation YAML per function
  ▼
5. test-stub-generation ← all Lane 1 + Lane 2 services
  │  xUnit (dotnet10) with mocked AWS SDK
  ▼
6. high-complexity-review ← Lane 3 services only
  │  Review brief produced; no handler code written
  ▼
7. migration-reporting  ← ALWAYS runs; collects all outputs, writes HTML report
  │
  ▼
[END]
```

---

## Output Directory Layout

```
migration-output/
├── service-inventory.json
├── migration-plan.json
├── handlers/
│   └── <ServiceName>/
│       ├── Function.cs          (dotnet10 runtime)
│       ├── <ServiceName>.csproj
│       └── aws-lambda-tools-defaults.json
├── terraform/
│   └── <ServiceName>/
│       ├── main.tf
│       ├── variables.tf
│       └── outputs.tf
├── cloudformation/
│   └── <ServiceName>/
│       └── template.yaml
├── tests/
│   └── <ServiceName>.Tests/
│       ├── HandlerTests.cs
│       ├── Fixtures/
│       │   └── baseline-trigger-payload.json
│       └── <ServiceName>.Tests.csproj
├── review-briefs/
│   └── <ServiceName>.md
└── migration-report-<YYYY-MM-DD>.html
```

---

## Standards

This skill is loaded by the [windows-service-to-lambda agent](./../../agents/windows-service-to-lambda.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).
