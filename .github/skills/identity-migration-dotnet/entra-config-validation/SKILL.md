---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: entra-config-validation
description: >-
  Validates Azure Entra ID configuration and runtime token/issuer/audience alignment for .NET applications.
  Use when verifying Entra settings (redirect/logout URLs, authority/tenant, audience/client id) and enforcing safe failures.
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
compatibility: "ASP.NET Core .NET 10"
---

# Entra Configuration & Runtime Validation

## When to use this skill
Use when:
- Writing Entra configuration into web.config/app settings
- You need deterministic checks for tenant/authority, issuer, audience, redirect/logout URLs
- You want consistent error messages and safe failure behavior

## Inputs
- Required config keys (tenant/authority, client id, redirect URL, logout URL, token validation settings)
- Environments supported (local/dev/test/prod) and how configuration differs
- Where config is currently stored and read

## Procedure
1. **Define configuration keys (placeholders only)**
   - Add/confirm keys for tenant/authority, client id, redirect URL, logout URL
2. **Implement config validation at startup**
   - If required keys are missing/invalid, fail early with actionable developer guidance
3. **Implement runtime validation checks**
   - Ensure token issuer aligns with configured authority/tenant
   - Ensure audience matches configured client id (or expected audience rules)
4. **Add safe diagnostics**
   - Provide developer-friendly messages without leaking identity contents
   - Redact sensitive values
5. **Document configuration**
   - Provide a short config section describing each key and expected format

## Guardrails
- No real IDs/secrets committed
- Fail safe (deny/challenge) on invalid identity state
- Keep config logic centralized

## Output format
### Config keys added/updated (placeholders only)
### Validation rules enforced
### Files changed
### Expected failure modes & developer actions

---

## Standards

This skill is loaded by identity migration agents. Config validation is **HIGH RISK** — all validation failures must be reviewed before proceeding. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/).