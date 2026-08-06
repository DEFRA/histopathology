---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: claims-mapping-saml-to-entra
description: >-
  Creates a centralized claims normalization/mapping layer to translate incoming SAML/Entra claims into canonical app claims,
  preserving existing authorization semantics (roles/groups). Use when the app relies on Windows identity or SAML claims.
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
compatibility: "ASP.NET Core .NET 10"
---

# Claims Mapping: SAML/Entra → Canonical App Claims

## When to use this skill
Use when:
- The application reads claims in multiple places (controllers/pages/services)
- Roles/groups mapping is inconsistent or implicit
- You need a single “source of truth” for identity/authorization claims

## Inputs
- List of claims the app currently uses (username, email/UPN, roles, groups, unique id)
- All `IsInRole`, role checks, and group-based authorization behaviors in code
- Any existing SAML claim type mappings currently embedded in the app

## Procedure
1. **Inventory claim usage**
   - Find all claim reads and role checks
   - Categorize them: identity, authorization, profile/display
2. **Define canonical claim contract**
   - Decide a minimal canonical set used everywhere in the app
   - Document required vs optional claims
3. **Implement a centralized mapping module**
   - Map incoming claim types to canonical claim names/types
   - Handle missing claims safely (explicit error or fallback)
4. **Update call sites**
   - Replace direct claim parsing with canonical helper accessors
   - Keep changes minimal and mechanical where possible
5. **Authorization equivalence check**
   - Ensure every pre-migration role/group decision yields the same allow/deny result post-migration
   - Document any intentional differences

## Guardrails
- Do not widen access accidentally (least privilege)
- Never silently change meaning of roles/groups
- Never log full claims payloads
- Keep mapping deterministic and testable



## Output format
### Canonical claims contract (required vs optional)
### Mapping rules (incoming → canonical)
### Files changed
### Authorization equivalence notes
### Test plan for claims scenarios
### Accountability sign-off
Role mapping decisions must be reviewed and signed off by a named accountable person before delivery.
Record here: **Reviewer:** [name] | **Date:** [date]

---

## Standards

This skill is loaded by identity migration agents. Claims mapping decisions are **HIGH RISK** — named accountable reviewer required. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/).