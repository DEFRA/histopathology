---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: entra-auth-bootstrap-dotnet
description: >-
  Bootstraps Azure Entra ID authentication for ASP.NET Core .NET 10 applications.
  Use when migrating from Windows Authentication to Entra ID.
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
compatibility: "ASP.NET Core .NET 10"
---

> **Framework scope:** This skill targets **.NET 10 (ASP.NET Core)**. Authentication is wired via `builder.Services.AddAuthentication()` in `Program.cs`. Use `appsettings.json` for configuration — `web.config`/`Global.asax` are legacy source artefacts to be analysed, not output targets.

# Entra Auth Bootstrap (.NET 10)

## When to use this skill
Use this skill when you need to:
- Introduce Entra ID sign-in to an ASP.NET (full framework) app
- Centralize authentication startup/configuration
- Replace or decouple Windows Authentication dependencies in a controlled way

## Inputs you should gather (from the repo)
- Current auth mode and entry points (e.g., web.config auth settings, Global.asax, any startup/auth modules)
- Existing usage of identity/roles/groups in VB.NET code
- Required redirect/logout URLs and expected token/claims shape (from migration requirements)

## Procedure (do in order)
1. **Discover current auth wiring**
   - Locate where auth is configured (config + initialization code)
   - List all files involved (e.g., config, startup, modules, global filters)
2. **Design a minimal bootstrap**
   - Choose a single “bootstrap” location for auth initialization (centralized)
   - Ensure it can be enabled/disabled via configuration (if repo pattern allows)
3. **Add configuration keys with placeholders**
   - Add keys for tenant/authority, client/app id, redirect URL, logout URL, and token validation expectations
   - Use placeholders only (no secrets, no real tenant IDs committed)
4. **Wire authentication challenge/redirect**
   - Ensure unauthenticated requests are challenged consistently
   - Ensure post-auth return path is deterministic
5. **Add safe failure paths**
   - Invalid config → fail fast with a clear message for developers
   - Token/claims issues → deny access or re-auth challenge (no silent failures)

## Guardrails
- Do not downgrade from .NET 10 or introduce .NET Framework dependencies in generated code
- Do not change authorization semantics (roles/groups) in this skill — only bootstrap
- Never log tokens or raw claims payloads
- Never commit secrets


## References (load when needed)
- ./references/bootstrap-checklist.md
- ./references/config-keys-template.md


## Output format (required)
### Summary
### Files changed (exact paths)
### Configuration keys added (placeholders only)
### Startup/auth flow decisions
### Manual verification steps (high-level)

---

## Standards

This skill is loaded by identity migration agents. Auth bootstrap code is **HIGH RISK** — second reviewer mandatory. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Credential Exposure Process](https://defra.github.io/software-development-standards/processes/credential_exposure/).
