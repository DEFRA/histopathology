---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
applyTo:
  - "**/*auth*.*"
  - "**/*identity*.*"
  - "**/*claims*.*"
  - "**/*saml*.*"
  - "**/*entra*.*"
  - "**/*openid*.*"
  - "**/*owin*.*"
  - "**/*authentication*.*"
  - "**/web.config"
  - "**/*security*.*"
---

# Authentication & Identity Instructions (Scoped)

These instructions apply **only** when GitHub Copilot is generating or modifying
authentication, identity, claims, or security-related code in this repository.

They work in addition to `.github/copilot-instructions.md` and do **not** replace it.

---

## Target Architecture

> **Framework scope:** These instructions apply to **.NET Framework 4.8 / VB.NET** applications only.
> For **ASP.NET Core (.NET 8+)** applications see `auth-aspnetcore.instructions.md` — the constraints below do not apply to those projects.

- Platform: **ASP.NET (Full .NET Framework 4.8)**
- Language: **VB.NET**
- Identity Provider: **Azure Entra ID**
- Legacy baseline: **Windows Authentication**
- Auth style: Redirect-based authentication with ID tokens and claims mapping

Do **not** propose (for .NET Framework projects):
- ASP.NET Core–only patterns
- .NET 6/7/8 features
- Minimal hosting / Program.cs models
- Framework upgrades unless explicitly requested

---

## Mandatory Design Rules (Auth-Specific)

### 1. Centralisation
- All authentication setup must be **centralised** (single bootstrap/config area).
- All claims handling must flow through a **single claims mapping layer**.
- Do not scatter claim parsing logic across controllers, pages, or services.

### 2. Claims Handling
- Normalize all incoming SAML / Entra ID claims into **canonical internal claims**.
- Never assume optional claims exist.
- Always provide safe defaults or explicit failure paths.
- Preserve **existing authorization semantics** (roles/groups) unless explicitly instructed otherwise.

### 3. Token & Session Safety
- Never log:
  - tokens
  - raw claims payloads
  - secrets or keys
- Token lifetime enforcement is mandatory:
  - issuer
  - audience
  - lifetime
  - signature
- Session expiry must trigger a **re-auth challenge**, not silent failure.

### 4. Configuration Discipline
- All auth configuration must:
  - be externally configurable (e.g., web.config / environment)
  - use placeholders such as `__TENANT_ID__`, `__CLIENT_ID__`
  - be documented when introduced
- Never hard-code:
  - tenant IDs
  - client IDs
  - redirect URLs
  - logout URLs

---

## Testing & Validation (Auth Changes Must Pass)

When modifying files in scope, Copilot must ensure the following are addressed:

### Required flow validation
- Unauthenticated request → redirect → authenticated return
- Valid token accepted
- Expired/invalid token rejected
- Logout clears session and redirects correctly

### Claims validation
- Required claims present post-login
- Missing optional claims handled safely
- Roles/groups mapping produces same authorization result as pre-migration

### Local testing expectation
- Prefer supporting a **mock SAML / identity provider** for local validation
- Real Entra ID configuration must be switchable via config only

---

## Output Expectations (Strict)

---

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)

When Copilot proposes or implements auth-related changes, it must include:

### ✅ Summary
What changed and why (auth/identity focused)

### 📁 Files Changed
Exact list of files and nature of changes

### 🔑 Claims & Session Decisions
- Claims mapped (old → new)
- Session expiry / renew behavior

### ⚙️ Configuration Keys
List of required keys (placeholders only)

### 🧪 Validation
What flows were validated or must be validated by the developer

---

## Explicit Prohibitions

❌ Do not introduce framework upgrades  
❌ Do not weaken authorization rules  
❌ Do not silently change role/group meaning  
❌ Do not log sensitive identity data  
❌ Do not invent identity flows not compatible with .NET Framework 4.8  

---

## Guiding Principle

> **“Authentication code is security code.”**  
> Changes must be explicit, testable, reversible, and easy to reason about.