# Identity Migration Orchestrator Agent — What It Does & Change Log

**File:** `.github/agents/identity-migration.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Identity Migration Orchestrator** coordinates the migration of modern ASP.NET Core (.NET 10) applications from Windows Authentication to Microsoft Entra ID. It detects the migration scenario, runs shared pre-flight steps, and routes to the correct specialist sub-agent. Specific to the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**.

> **Scope:** .NET 10 ASP.NET Core only. .NET Framework / OWIN applications are out of scope.

---

### Sub-Agents Invoked

| Sub-Agent | File | Protocol |
|---|---|---|
| SAML Sub-Agent | `.github/agents/identity-migration-saml.agent.md` | SAML 2.0 via Sustainsys.Saml2 |
| OIDC Sub-Agent | `.github/agents/identity-migration-oidc.agent.md` | OIDC + PKCE via Microsoft.Identity.Web |

The orchestrator does not implement protocol-specific logic — that lives exclusively in the sub-agents.

---

### Scenarios Supported

| Scenario | Detection signal | Sub-agent routed to |
|---|---|---|
| `WindowsAuthToSAML` | `AddNegotiate()` in `Program.cs` + user wants SAML | SAML Sub-Agent |
| `WindowsAuthToOIDC` | `AddNegotiate()` in `Program.cs` + user wants OIDC | OIDC Sub-Agent |
| `NewSAML` | No auth wiring + user wants SAML | SAML Sub-Agent |
| `NewOIDC` | No auth wiring + user wants OIDC | OIDC Sub-Agent |

If the target protocol is ambiguous, the orchestrator asks: **"Should this app use SAML2 or OIDC for Entra ID authentication?"**

---

### Input Parameters

| Parameter | Allowed values | Default | Impact |
|---|---|---|---|
| `TargetFramework` | `net10` | Inferred from `.csproj` | Passed to sub-agent for library version selection |
| `MigrationScenario` | `WindowsAuthToSAML` \| `WindowsAuthToOIDC` \| `NewSAML` \| `NewOIDC` | Inferred from codebase | Controls sub-agent routing |
| `SessionStore` | `InProc` \| `Redis` \| `SqlSession` | `InProc` | Controls token cache and session wiring |

---

### Orchestrator Responsibilities

| # | Step | What the orchestrator does |
|---|---|---|
| 1 | Framework detection | Reads `.csproj` to confirm `net10.0`; rejects if `web.config` present (out of scope) |
| 2 | Scenario detection | Scans `Program.cs` for `AddNegotiate()`, `WindowsIdentity`, existing SAML/OIDC references |
| 3 | Pre-flight | Discovery, Impact Summary, Entra app registration checklist (`ENTRA-REGISTRATION.md`) |
| 4 | Config scaffolding | Creates all four environment config stubs (`appsettings.{Development\|Test\|UAT\|Production}.json`) with placeholder values |
| 5 | Sub-agent routing | Routes to SAML or OIDC sub-agent (or both in parallel for `NewSAML` + `NewOIDC`) |
| 6 | Output stitching | Consolidates sub-agent outputs into a single `summary.md` delivery artifact |

---

### Outputs Produced (Shared — Created by Orchestrator)

| Output | Description |
|---|---|
| `ENTRA-REGISTRATION.md` | Entra ID app registration checklist — produced once; sub-agents do not regenerate |
| `appsettings.Development.json` | Config stub with placeholder values |
| `appsettings.Test.json` | Config stub with placeholder values |
| `appsettings.UAT.json` | Config stub with placeholder values — listed in `.copilotignore` |
| `appsettings.Production.json` | Config stub with placeholder values — listed in `.copilotignore` |
| `summary.md` | Per-migration tracking file (scenario, files changed, role map, verification checklist, risks) |

Protocol-specific outputs (e.g., `ClaimsTransformation.cs`, updated `Program.cs`, test stubs) are produced by the sub-agents — see the SAML and OIDC agent guides.

---

### `.copilotignore` Requirements

The following entries must be present in `.copilotignore` before the migration PR is raised:
- `appsettings.UAT.json`
- `appsettings.Production.json`
- `ENTRA-REGISTRATION.md`

---

### Quality Gates (All Must Pass Before Merge)

| Gate | Requirement |
|---|---|
| SonarQube | Zero new critical or high-severity findings |
| Code coverage | ≥80% line coverage from xUnit tests |
| Secret scanning | No credentials or tokens committed |
| Human review | Second reviewer mandatory (HIGH RISK) |
| AI transparency | PR description must disclose AI assistance, name the second reviewer, confirm gates passed |
| `.copilotignore` | Three entries confirmed present |

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | **HIGH** |
| Human oversight required | Yes — at every stage |
| Second reviewer required | Yes — mandatory |
| Data protection | UK GDPR applies (PII in auth tokens and claims) |
| Defra SDS standards applied | C# Coding, Security, Git Branching, Logging, Credential Exposure |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Identity Migration Orchestrator agent guide.

---

## References

- [`.github/agents/identity-migration.agent.md`](./../agents/identity-migration.agent.md) — orchestrator agent definition
- [Identity Migration SAML Agent Guide](./identity-migration-saml-agent-guide.md)
- [Identity Migration OIDC Agent Guide](./identity-migration-oidc-agent-guide.md)
- [`.github/instructions/auth.instructions.md`](./../instructions/auth.instructions.md) — auth rules
- [`.github/instructions/auth-aspnetcore.instructions.md`](./../instructions/auth-aspnetcore.instructions.md) — ASP.NET Core security rules
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/identity-migration.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
