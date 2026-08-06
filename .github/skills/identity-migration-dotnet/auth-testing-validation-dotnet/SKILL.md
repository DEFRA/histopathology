---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: auth-testing-validation-dotnet
description: >-
  Provides a repeatable testing and validation checklist for Entra ID authentication migrations.
  Use when verifying sign-in, token/session behavior, claims mapping, logout, and local testing with mock SAML IdP.
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
compatibility: "ASP.NET Core .NET 10"
---

> **Framework scope:** This skill targets **.NET 10 (ASP.NET Core)**. Test stubs should use xUnit with `Microsoft.AspNetCore.Mvc.Testing` and Playwright for journey tests.

# Auth Testing & Validation (.NET 10)

## When to use this skill
Use when:
- You have implemented auth bootstrap, token/session lifecycle, or claims mapping
- You need a consistent smoke test + regression test checklist
- You want a lightweight “validation artifact” for review/audit

## Inputs
- Expected auth flows (sign-in, renew/re-auth, logout)
- Canonical claims contract and role/group mapping rules
- Any local testing constraints (e.g., ability to use mock IdP)

## Procedure (validation levels)

### Level 1: Build verification
1. Compile/build the solution using the repository’s standard process
2. Confirm configuration keys exist (placeholders are fine for build)

### Level 2: Runtime flow validation
Validate these flows end-to-end:
- Unauthenticated request → challenge/redirect → authenticated return
- Valid token accepted
- Expired/invalid token rejected safely
- Session expiry triggers re-auth
- Logout clears session and redirects correctly

### Level 3: Claims & authorization equivalence
- Required canonical claims appear after login
- Missing optional claims do not crash the app
- Role/group mapping yields the same allow/deny outcomes as pre-migration

### Level 4: Local testing support (recommended)
- Where feasible, support a mock SAML/identity provider approach for local claims testing, and document how to switch to real Entra settings.
  (This pattern is useful when tenant dependencies slow down dev testing.)

## What to generate (deliverables)
Create a lightweight `AUTH_VALIDATION.md` containing:
- Flows tested
- Claims mapping verified
- Authorization equivalence notes
- Files changed
- Known limitations / follow-ups

## Guardrails
- Do not include secrets or token values in the validation artifact
- Do not paste raw claims dumps; summarize and redact
- If a validation step can’t be completed, state what is missing (config, access, environment) without guessing

## References (load when needed)
- ./references/local-testing-mock-idp.md
- ./references/smoke-test-checklist.md

## Output format
### AUTH_VALIDATION.md draft
### Test scenarios checklist (pass/fail placeholders)
### Any new/updated test files (if created)

---

## Standards

This skill is loaded by identity migration agents. All auth test code is **HIGH RISK** — second reviewer mandatory. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/).