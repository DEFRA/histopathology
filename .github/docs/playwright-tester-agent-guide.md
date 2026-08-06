# Playwright Tester Agent — What It Does & Change Log

**File:** `.github/agents/playwright-tester-agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Playwright Tester Agent** generates, improves, and executes Playwright end-to-end tests using TypeScript. It uses the Playwright MCP to navigate websites, capture page snapshots, and write reliable, maintainable test suites. It can also run tests and iterate on failures until tests pass consistently.

> **Note:** This agent writes TypeScript Playwright tests. For C# .NET Playwright tests, use the [E2E Baseline Orchestrator](./readme-e2e-baseline-agent-framework.md).

---

### Core Responsibilities

| # | Responsibility | Detail |
|---|---|---|
| 1 | **Website exploration** | Navigate to the website using Playwright MCP, take page snapshots, identify key user flows — no code is generated until exploration is complete |
| 2 | **Test improvements** | When improving existing tests, navigate to the URL first and use the snapshot to identify correct locators |
| 3 | **Test generation** | Write well-structured TypeScript Playwright tests based on explored user flows |
| 4 | **Test execution & refinement** | Run tests, diagnose failures, iterate until all tests pass reliably |
| 5 | **Documentation** | Provide clear summaries of functionalities tested and test structure |

---

### Test Generation Principles

- **Explore first, code second** — the agent always navigates and analyses the site before writing any test code
- **Role-based locators** — prefer `getByRole`, `getByLabel`, `getByText` over CSS selectors or XPath
- **Auto-retrying assertions** — use Playwright's built-in web-first assertions (`expect(locator).toHaveText()`, etc.)
- **No hard-coded waits** — rely on Playwright's auto-waiting; never use `page.waitForTimeout()`
- **Descriptive test titles** — test names clearly state the intent and expected outcome

---

### Toolset

The agent uses the `playwright` MCP tool alongside standard file editing and search tools. It can:
- Navigate to URLs and capture accessibility snapshots
- Identify locators from live page state
- Run `playwright test` and analyse failures
- Update test files based on run results

---

### Outputs Produced

| Output | Description |
|---|---|
| Playwright test files (`.spec.ts`) | Well-structured TypeScript test suites |
| Test run summary | Pass/fail summary with failure diagnostics |

---

### Guardrails Summary

**The agent will:**
- Explore the website before writing any test code
- Use accessibility-first locators for resilience
- Run and verify tests pass before completing

**The agent will never:**
- Use `page.waitForTimeout()` or hard-coded waits
- Generate test code without first exploring the page
- Leave failing tests without attempting to resolve them

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review generated tests before committing |
| No production data | Use test environments only |
| Defra SDS standards applied | GitHub Copilot guide, QA & test standards, Security standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Playwright Tester agent guide.

---

## References

- [`.github/agents/playwright-tester-agent.md`](./../agents/playwright-tester-agent.md) — agent definition
- [`.github/instructions/playwright-dotnet.instructions.md`](./../instructions/playwright-dotnet.instructions.md) — Playwright .NET test generation conventions
- [Playwright documentation](https://playwright.dev/docs/intro)
- [Playwright TypeScript documentation](https://playwright.dev/docs/test-typescript)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Quality assurance and test standards](https://defra.github.io/software-development-standards/standards/quality_assurance_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/playwright-tester-agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
