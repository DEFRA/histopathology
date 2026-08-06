# Service Modernisation (WCF/ASMX → REST) Agent — What It Does & Change Log

**File:** `.github/agents/service-modernisation-wcf-asmx-to-rest.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Service Modernisation Agent** converts WCF and ASMX SOAP service contracts to REST APIs and direct in-process calls using a complexity-tiered migration strategy. It is fully generic — it detects the BO layer (CSLA, custom wrapper, direct ADO.NET, etc.) during analysis and adapts accordingly.

---

### Complexity Tiers

| Complexity | Approach |
|---|---|
| **Low** | Mechanical contract → controller conversion; data-parity integration test |
| **Medium** | Type remapping, `FaultContract` validation extraction, BO layer adapter |
| **High** | Structured review brief + manual sign-off gate — **never auto-converted** |

---

### Entry Points (Unified Modes)

| Mode | Purpose |
|---|---|
| `--mode analyze` | Scans legacy WCF/ASMX source; produces `service-analysis.json`, migration plan, and OpenAPI draft spec |
| `--mode validate` | Compares the new REST implementation against original contracts; produces diff report and sign-off checklist |

---

### `--mode analyze` — Steps

| Step | Name | What happens |
|---|---|---|
| 1 | **[Scan]** | Extracts operation signatures, DataContract types, FaultContracts, BO layer pattern; classifies services Low/Medium/High; produces `service-analysis.json` |
| 2 | **[Design]** | Derives OpenAPI 3.0 draft (`openapi-draft.yaml`) and `service-modernisation-plan.json`; maps HTTP methods from operation semantics |
| 3 | **[Generate scaffold]** | Generates SDK-style `.csproj` targeting `net10.0`; controller shells; DTO shells; `IServiceName` interface shells; `Program.cs` with DI wiring; folder structure |

All generated source files are **C# regardless of source language** — VB.NET types are mapped to C# equivalents.

### `--mode validate` — Steps

| Step | Name | What happens |
|---|---|---|
| 1 | **[Validate]** | Diffs original contracts against REST implementation across: operation coverage, request field coverage, response field coverage, route/HTTP method alignment |
| 2 | **[Test]** | Parses test output (`.trx` or xUnit XML) or runs `dotnet test`; appends per-operation pass/fail summary |
| 3 | **[Sign-off]** | Produces `sign-off-checklist.md` enumerating all Critical findings with resolution confirmation |

---

### Outputs Produced

| Output | Description |
|---|---|
| `service-analysis.json` | Classified service inventory with risk flags |
| `service-modernisation-plan.json` | Sequenced migration roadmap with effort estimates |
| `openapi-draft.yaml` | OpenAPI 3.0 draft specification |
| Project scaffold | `net10.0` C# project with controller, DTO, interface, and test shells |
| `contract-diff-report.md` | Human-readable contract comparison with Critical/Warning/Info findings |
| `contract-diff.json` | Machine-readable diff for CI integration |
| `sign-off-checklist.md` | Production readiness checklist requiring owner sign-off on all Critical findings |

---

### Finding Classifications (Validate mode)

| Classification | Meaning |
|---|---|
| **Critical** | Potential data loss — field present in original absent from REST |
| **Warning** | Behavioural change — route, method, or semantic difference |
| **Info** | Cosmetic or additive difference |

---

### Guardrails Summary

**The agent will:**
- Never auto-convert High-complexity services — these always produce a review brief with a manual sign-off gate
- Generate only C# output regardless of source language
- Confirm that `openapi-draft.yaml` is reviewed and adjusted before `generate-low-medium` or `generate-inprocess` runs
- Pin exact NuGet package versions in generated `.csproj` files

**The agent will never:**
- Hardcode secrets in any generated file
- Delete or overwrite original WCF/ASMX source files
- Produce incomplete scaffolds — all shells are generated before the mode completes

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — OpenAPI draft review required before implementation; sign-off checklist for Critical findings |
| Defra SDS standards applied | GitHub Copilot guide, C# coding standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Service Modernisation WCF/ASMX → REST agent guide.

---

## References

- [`.github/agents/service-modernisation-wcf-asmx-to-rest.agent.md`](./../agents/service-modernisation-wcf-asmx-to-rest.agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/service-modernisation-wcf-asmx-to-rest.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
