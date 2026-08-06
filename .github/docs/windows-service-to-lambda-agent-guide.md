# Windows Service to Lambda Migration Agent — What It Does & Change Log

**File:** `.github/agents/windows-service-to-lambda.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Windows Service to Lambda Migration Agent** migrates scheduled and event-driven .NET Framework 4.x Windows Service executables to AWS Lambda serverless functions. It reads existing service source code, generates equivalent Lambda handlers in **C# (.NET 10 isolated worker)**, produces Terraform HCL or CloudFormation YAML for supporting infrastructure, and outputs integration test stubs.

Currently used on the Defra Civica LAP programme to migrate `ProficiencyTesting*` Windows Services to AWS Lambda.

---

### Target Architecture

| Legacy Pattern | AWS Lambda Replacement |
|---|---|
| `System.Timers.Timer` / Task Scheduler | EventBridge Scheduler or scheduled CloudWatch Events |
| File-watcher service | S3 event trigger → Lambda |
| Queue-draining worker | SQS trigger → Lambda |
| `OnStart` background threads | Lambda handler invoked per event |
| `EventLog.WriteEntry` | CloudWatch Logs |
| Registry / `Microsoft.Win32` | SSM Parameter Store |
| Secrets in config | Secrets Manager |
| Shared in-process state | DynamoDB / ElastiCache / S3 |

---

### Key Migration Constraints

| Constraint | Lambda limit | Mitigation |
|---|---|---|
| Stateless execution | No persistent in-process state between invocations | Move shared state to DynamoDB / ElastiCache / S3 |
| Timeout | 15-minute hard cap | Chunk long batch jobs via SQS pagination or Step Functions |
| Cold-start | Connection overhead on first invocation | RDS Proxy or lazy-singleton patterns for ADO.NET connections |
| Windows-only APIs | `Microsoft.Win32`, `System.ServiceProcess`, `EventLog`, `Registry` not available | Replace with CloudWatch, SSM Parameter Store, Secrets Manager |

---

### Skills

| Skill | File | Purpose |
|---|---|---|
| `windows-service-to-lambda` | `.github/skills/windows-service-to-lambda/SKILL.md` | Full migration pipeline: analysis, code generation, IaC, tests, report |

---

### Outputs Produced

| Output | Path | Description |
|---|---|---|
| Lambda handler(s) | `src/<ServiceName>/` | C# .NET 10 isolated worker Lambda handler |
| IaC templates | `infrastructure/` | Terraform HCL or CloudFormation YAML for Lambda + triggers + IAM |
| Integration test stubs | `tests/<ServiceName>.Tests/` | xUnit stubs with mocked AWS SDK clients |
| Review briefs | `review-briefs/<ServiceName>.md` | Manual review notes for complex migration decisions |
| Migration report | `migration-report-<YYYY-MM-DD>.html` | Styled HTML report covering all changes, assumptions, and blockers |

---

### Guardrails Summary

**The agent will:**
- Replace all Windows-specific APIs with AWS equivalents (CloudWatch, SSM, Secrets Manager)
- Generate C# .NET 10 output regardless of source framework version
- Chunk batch jobs exceeding the Lambda 15-minute timeout limit
- Use RDS Proxy or lazy-singleton patterns to mitigate cold-start connection overhead
- Pin exact NuGet and AWS SDK package versions

**The agent will never:**
- Use `Microsoft.Win32`, `System.ServiceProcess`, `EventLog`, or `Registry` in generated code
- Hardcode AWS credentials, ARNs, or account IDs in generated code
- Generate stateful Lambda handlers — all shared state must use external stores
- Include secrets in any output file

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review IaC and IAM permissions before deployment |
| Defra SDS standards applied | GitHub Copilot guide, C# coding standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Windows Service to Lambda Migration agent guide.

---

## References

- [`.github/agents/windows-service-to-lambda.agent.md`](./../agents/windows-service-to-lambda.agent.md) — agent definition
- [AWS Lambda .NET documentation](https://docs.aws.amazon.com/lambda/latest/dg/lambda-csharp.html)
- [AWS Lambda quotas](https://docs.aws.amazon.com/lambda/latest/dg/gettingstarted-limits.html)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/windows-service-to-lambda.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
