# Identity Migration OIDC Agent — What It Does & Change Log

**File:** `.github/agents/identity-migration-oidc.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Identity Migration OIDC Sub-Agent** handles the complete OIDC Authorization Code + PKCE flow for four Civica applications (BSE, Histo, D2R2, PTLIMS) migrating to ASP.NET Core on .NET 10. It is invoked by the `identity-migration` orchestrator after shared discovery and scaffolding are complete.

The agent replaces Windows Authentication (Kerberos/NTLM) or absent authentication with a standards-compliant OIDC integration against Microsoft Entra ID, producing all wiring, configuration, token lifecycle management, claims normalisation, tests, and documentation needed to reach production readiness.

---

### Scenarios Handled

| Scenario | Starting point | What the agent does |
|---|---|---|
| `WindowsAuthToOIDC` | App uses `AddNegotiate()` / Windows Auth | Removes Windows Auth wiring, replaces all `WindowsIdentity` usages with `ClaimsPrincipal`, wires OIDC middleware |
| `NewOIDC` | App has no authentication wiring | Wires OIDC from scratch — no removal work needed |

---

### Inputs Received from Orchestrator

| Input | Description |
|---|---|
| `TargetFramework` | `net10` |
| `MigrationScenario` | `WindowsAuthToOIDC` or `NewOIDC` |
| `SessionStore` | `InProc`, `Redis`, or `SqlSession` |
| Impact Summary | Windows Auth signals, authorization model, files to touch |
| `ENTRA-REGISTRATION.md` | Path to the shared Entra app registration checklist — agent does not regenerate this |
| Four-environment config stubs | `appsettings.Development.json`, `appsettings.Test.json`, `appsettings.UAT.json`, `appsettings.Production.json` — created by orchestrator; agent only adds missing `AzureAd` sections |

---

### OIDC Library Selection

| Approach | NuGet package | When to use |
|---|---|---|
| **Microsoft.Identity.Web (preferred)** | `Microsoft.Identity.Web` | New integrations; no conflicting auth setup |
| Bare AddOpenIdConnect | Built into ASP.NET Core | Explicit request; integration constraints |
| Microsoft.Identity.Web.MicrosoftGraph | `Microsoft.Identity.Web.MicrosoftGraph` | Only when Graph API calls are explicitly required — never added speculatively |

> **Rule:** Only add a NuGet package when at least one concrete usage is wired in the same migration. Exact version must be pinned in `.csproj` — no floating ranges. Pin version and reason must be recorded in the PR description.

---

### Primary Functions (Responsibilities)

| # | Responsibility | Description |
|---|---|---|
| 1 | OIDC Middleware Wiring | Configures `AddAuthentication` + `AddMicrosoftIdentityWebApp` (or `AddOpenIdConnect` + `AddCookie`) in `Program.cs`; PKCE enabled by default |
| 2 | `appsettings.json` AzureAd Section | Adds `AzureAd` config block with all keys as placeholders (`TenantId`, `ClientId`, `ClientSecret`, `CallbackPath`, `SignedOutCallbackPath`) |
| 3 | PKCE Flow | Confirms `UsePkce = true`; documents `code_challenge_method = S256`; ensures no code overrides disable PKCE |
| 4 | Silent Renew Handler | Uses `ITokenAcquisition.AcquireTokenSilent`; on `MsalUiRequiredException` falls back to interactive re-challenge; MSAL token cache wired to `SessionStore` |
| 5 | Token Storage | `id_token` claims populate `ClaimsPrincipal` via cookie; `access_token` and `refresh_token` stored in MSAL distributed token cache — never in logs or unencrypted cookies |
| 6 | Local Sign-in / Sign-out Pages | Thin `Account/Login` and `Account/Logout` Razor Page wrappers calling `ChallengeAsync` / `SignOutAsync` — no token logic in pages |
| 7 | Windows Auth Removal | (`WindowsAuthToOIDC` only) Inventories all `WindowsIdentity` call sites, removes `AddNegotiate`, replaces all usages in a single pass |
| 8 | Claims Normalisation | Implements `IClaimsTransformation`; confirms whether Entra emits App Role values, group GUIDs, or display names before writing `RoleMap`; preserves all existing role gates |
| 9 | Negative-Path Handlers | Explicit handlers for: expired `id_token`, failed `nonce`/`state`, expired `access_token`, expired/revoked `refresh_token`, audience mismatch, consent not granted |
| 10 | `nonce` / `state` Validation | Relies on ASP.NET Core OpenIdConnect middleware auto-validation; never disables nonce validation; never overrides `OnMessageReceived`/`OnTokenValidated` to bypass checks |

---

### Key URLs

| URL | Path | Notes |
|---|---|---|
| **Callback (redirect URI)** | `/signin-oidc` | Entra ID posts the authorization code here after login |
| **Post-logout redirect** | `/signout-callback-oidc` | Entra ID redirects here after logout |
| **Sign-out trigger** | Application-defined (e.g. `/Account/Logout`) | Calls `SignOutAsync` for both OIDC and cookie schemes |

These paths must be registered as **Redirect URIs** and **Front-channel logout URLs** in the Entra ID app registration per environment.

---

### Outputs Produced

| Output | Location | Notes |
|---|---|---|
| Updated `Program.cs` | Source root | OIDC + cookie middleware wired; MSAL token cache registered |
| Updated `.csproj` | Project root | `Microsoft.Identity.Web` package added with pinned version |
| Updated `appsettings*.json` | Project root | `AzureAd` section added to each environment file missing it |
| `ClaimsTransformation.cs` | Application services layer | `IClaimsTransformation` implementation |
| `Account/Login.cshtml` + `Login.cshtml.cs` | Pages/Account (if UX requires) | Thin challenge wrapper |
| `Account/Logout.cshtml` + `Logout.cshtml.cs` | Pages/Account (if UX requires) | Thin sign-out wrapper |
| xUnit test stubs | Test project | Auth Code + PKCE, silent renew, negative-path handlers, logout, role equivalence; parameterised per environment |
| `appsettings.json` key reference | Documentation | Each `AzureAd` key explained with where to find the value in Entra ID |
| Entra ID registration checklist | Documentation | App registration type, redirect URIs, client secret/certificate, token configuration |
| MSAL token cache configuration docs | Documentation | Per `SessionStore` (`InProc`, `Redis`, `SqlSession`) |
| Troubleshooting table | Documentation | AADSTS error codes, causes, fixes |
| IIS / hosting change notes | Documentation | (`WindowsAuthToOIDC` only) Windows Auth disabled, Anonymous Auth enabled |

---

### Quality Gates (All Must Pass Before Merge)

| Gate | Requirement |
|---|---|
| SonarQube | Zero new critical or high-severity findings |
| Code coverage | ≥80% line coverage from xUnit tests (verified by CI pipeline) |
| Secret scanning | No credentials or tokens in committed files (`ClientSecret` must reference Key Vault in production) |
| Human review | Second human reviewer mandatory (HIGH RISK — Defra AI Toolkit) |
| AI transparency | PR description must disclose AI assistance, name the second reviewer, and confirm SonarQube and coverage gates passed |
| `.copilotignore` | `appsettings.UAT.json`, `appsettings.Production.json`, `ENTRA-REGISTRATION.md` must be listed |

---

### Guardrails Summary

**The agent will:**
- Wire all auth through `Program.cs` — single configuration entry point
- Store all OIDC/Entra config in `appsettings.json` under `AzureAd` — no values in code
- Prefer `Microsoft.Identity.Web` — it handles Entra ID token flows and MSAL correctly
- Implement claims normalisation via `IClaimsTransformation` registered in DI
- Confirm PKCE is enabled — never disabled
- Preserve every existing role gate — no silent access changes
- Create a feature branch for all changes; never commit to `main`
- Disclose AI assistance in the PR description

**The agent will never:**
- Use SAML libraries or generate ACS/SLO endpoints — OIDC only
- Apply SAML assertion validation to OIDC token responses
- Store `access_token` or `refresh_token` in client-accessible storage (unencrypted cookies, `ViewState`, `localStorage`)
- Add `Microsoft.Identity.Web.MicrosoftGraph` unless Graph API calls are explicitly implemented
- Use OWIN middleware
- Disable `nonce` or `state` validation
- Add a NuGet package without wiring at least one concrete usage
- Overwrite `appsettings.{Environment}.json` files owned by the orchestrator
- Rename a public method without grepping all call sites first
- Bypass SonarQube, coverage, or peer-review gates
- Feed token payloads or personal data to an AI model without a confirmed Defra data handling agreement

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | **HIGH** |
| Human oversight required | Yes — at every stage |
| Second reviewer required | Yes — mandatory |
| Data protection | UK GDPR applies (PII in OIDC tokens and claims) |
| Defra SDS standards applied | C# Coding, Security, Git Branching, Logging, Credential Exposure |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Identity Migration OIDC Sub-Agent guide covering:

- OIDC Authorization Code + PKCE middleware wiring for `WindowsAuthToOIDC` and `NewOIDC` scenarios
- `Microsoft.Identity.Web` library preference and fallback to bare `AddOpenIdConnect`
- PKCE, silent renew via `ITokenAcquisition`, and MSAL distributed token cache per `SessionStore`
- Token storage rules: `id_token` claims in cookie, `access_token`/`refresh_token` in MSAL cache only
- `id_token` and `access_token` negative-path handlers (expired, audience mismatch, consent denied)
- `nonce`/`state` validation enforcement
- Local sign-in/sign-out page scaffolding (thin wrappers only)
- Claims normalisation via `IClaimsTransformation`
- Windows Auth removal inventory and single-pass call-site replacement
- xUnit test stubs parameterised per environment
- Documentation requirements including AADSTS troubleshooting table and MSAL cache config docs
- SonarQube gate, ≥80% coverage threshold, and second reviewer requirement
- References to `auth-aspnetcore.instructions.md`, Defra SDS C#/Security standards, and Defra AI Toolkit

---

## References

- [`.github/agents/identity-migration-oidc.agent.md`](./../agents/identity-migration-oidc.agent.md) — agent definition this guide documents
- [`.github/instructions/auth-aspnetcore.instructions.md`](./../instructions/auth-aspnetcore.instructions.md) — security, logging, and credential rules
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra SDS — Logging standards](https://defra.github.io/software-development-standards/standards/logging/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)
- [Microsoft.Identity.Web documentation](https://learn.microsoft.com/en-us/azure/active-directory/develop/microsoft-identity-web)

---

*This document is maintained alongside `.github/agents/identity-migration-oidc.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
