# .NET Test Automation & Quality Agent — What It Does & Change Log

**File:** `.github/agents/dotnet-test-automation-and-quality-agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **.NET Test Automation & Quality Agent** assists with testing and quality assurance tasks for .NET projects. It is an expert C#/.NET developer and tester producing clean, well-designed, secure, and maintainable test code following .NET conventions and testing best practices. Supports .NET 10 and C# 14.

---

### Core Responsibilities

1. **Understand context** — read TFM, C# version, `global.json` SDK, app type, and existing test framework before generating any test code
2. **Generate tests** — produce positive, negative, and exception scenario unit tests for every public class in every project within the solution (excluding auto-generated files)
3. **Follow conventions** — use the existing test framework (xUnit preferred), AAA pattern, and the project's naming conventions
4. **Run and validate** — run tests and iterate until all pass; generate code coverage report
5. **Language matching** — test language matches source language (C# tests for C# code; VB.NET tests for VB.NET code)

---

### Test Generation Rules

| Rule | Detail |
|---|---|
| Framework | Use the framework already in the solution (xUnit preferred) |
| Project structure | Separate `[ProjectName].Tests` project; mirror source class structure |
| Naming | `MethodName_Scenario_ExpectedBehavior` or `WhenX_ThenY` |
| Pattern | Arrange-Act-Assert (AAA) — one behavior per test |
| Independence | Tests must run in any order or in parallel |
| Assertions | Assert specific values; avoid vague outcomes |
| No production data | Use synthetic or anonymised test data only |
| No disk I/O cleanup | If disk I/O is needed, randomize paths and do not clean up |
| Public APIs only | Do not change visibility or use `InternalsVisibleTo` |

---

### xUnit Guidance

| Element | Usage |
|---|---|
| Test attribute | `[Fact]` for single tests |
| Parameterized | `[Theory]` + `[InlineData]` |
| Setup/teardown | Constructor and `IDisposable` |
| Assertions | Framework asserts; FluentAssertions/AwesomeAssertions if already in solution |
| Exception testing | `Assert.Throws` / `Assert.ThrowsAsync` |

---

### Code Coverage

```bash
# Install once
dotnet tool install -g dotnet-coverage

# Run with every test change
dotnet-coverage collect -f cobertura -o coverage.cobertura.xml dotnet test
```

---

### Mocking Rules

- Avoid mocks where possible — prefer real implementations
- Only mock external dependencies (not code under test)
- Write a skipped/explicit test to verify mock outputs match real dependency outputs

---

### Guardrails Summary

**The agent will:**
- Read TFM and check `global.json` before generating any code
- Generate tests that compile and pass before completing
- Use the framework already in the solution — no new test frameworks introduced without explicit request
- Follow `dotnet-coverage` for coverage reporting

**The agent will never:**
- Use production data in tests
- Test through private/internal APIs by changing visibility
- Add branching or conditionals inside test methods

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review generated tests before merging |
| No production data | Mandatory — synthetic/anonymised data only |
| Defra SDS standards applied | GitHub Copilot guide, C# coding standards, QA & test standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the .NET Test Automation & Quality agent guide.

---

## References

- [`.github/agents/dotnet-test-automation-and-quality-agent.md`](./../agents/dotnet-test-automation-and-quality-agent.md) — agent definition
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Quality assurance and test standards](https://defra.github.io/software-development-standards/standards/quality_assurance_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/dotnet-test-automation-and-quality-agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
