---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: token-lifecycle-dotnet
description: >-
  Implements token validation, expiry handling, session lifecycle rules, and renew/re-auth triggers for Entra ID authentication.
  Use when you need consistent token/session behavior (expiry, re-auth, renew).
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
compatibility: "ASP.NET Core .NET 10"
---

> **Framework scope:** This skill targets **.NET 10 (ASP.NET Core)**. Token validation and session lifecycle are managed by ASP.NET Core authentication middleware — wire via `AddAuthentication()` in `Program.cs` and use `ITokenAcquisition` (OIDC) or SAML session expiry re-challenge patterns.

# Token Lifecycle & Session Management (.NET 10)

## When to use this skill
Use when implementing or refactoring:
- ID token validation rules (issuer/audience/lifetime/signature enforcement)
- Session expiration behavior and re-auth triggers
- Renew/refresh patterns compatible with the application flow

## Inputs
- Where the app currently stores session state (in-proc, SQL session, cookies, etc.)
- Existing login/session timeout behaviors (config + code)
- Any existing “remember me” or long-lived session expectations

## Procedure
1. **Locate current session + identity storage**
   - Identify if identity is derived from server session, cookies, or per-request principal only
2. **Define and implement expiry rules**
   - Specify what “expired” means (token lifetime, session lifetime, both)
   - Ensure expiry produces a deterministic outcome (re-auth challenge or logout)
3. **Implement token validation boilerplate**
   - Centralize token validation (no scattered validation checks)
   - Ensure validation failures are handled consistently
4. **Implement renew/re-auth logic**
   - Prefer deterministic re-auth triggers on expiry
   - Avoid “silent failures” that leave the app in a half-authenticated state
5. **Harden logging and error handling**
   - Redact or avoid any sensitive token/claims logs
   - Provide safe, diagnosable errors for developers (without leaking identity data)

## Guardrails
- Never store raw tokens in server session unless explicitly required
- Never log tokens or claims payloads
- Keep changes incremental and limited to lifecycle/session behavior

## Output format
### Summary
### Files changed
### Token/session rules implemented
### Expiry + re-auth/renew scenarios covered
### Verification checklist
- Valid token accepted
- Expired token triggers re-auth
- Invalid token denied safely
- Logout clears session cleanly

---

## Standards

This skill is loaded by identity migration agents. Token lifecycle code is **HIGH RISK** — second reviewer mandatory. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/).