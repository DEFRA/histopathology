# Identity Migration SAML Agent — What It Does & Change Log

**File:** `.github/agents/identity-migration-saml.agent.md`  
**Last reviewed:** 2026-07-29  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Identity Migration SAML Sub-Agent** handles the complete SAML 2.0 assertion lifecycle for four Civica applications (BSE, Histo, D2R2, PTLIMS) migrating to ASP.NET Core on .NET 10. It is invoked by the `identity-migration` orchestrator after shared discovery and scaffolding are complete.

The agent replaces Windows Authentication (Kerberos/NTLM) or absent authentication with a standards-compliant SAML 2.0 integration against Microsoft Entra ID, producing all wiring, configuration, claims normalisation, tests, and documentation needed to reach production readiness.

---

### Scenarios Handled

| Scenario | Starting point | What the agent does |
|---|---|---|
| `WindowsAuthToSAML` | App uses `AddNegotiate()` / Windows Auth | Removes Windows Auth wiring, replaces all `WindowsIdentity` usages with `ClaimsPrincipal`, wires SAML2 middleware |
| `NewSAML` | App has no authentication wiring | Wires SAML2 from scratch — no removal work needed |

---

### Inputs Received from Orchestrator

| Input | Description |
|---|---|
| `TargetFramework` | `net10` |
| `MigrationScenario` | `WindowsAuthToSAML` or `NewSAML` |
| `SessionStore` | `InProc`, `Redis`, or `SqlSession` |
| Impact Summary | Windows Auth signals, authorization model, files to touch |
| `ENTRA-REGISTRATION.md` | Path to the shared Entra app registration checklist — agent does not regenerate this |
| Four-environment config stubs | `appsettings.Development.json`, `appsettings.Test.json`, `appsettings.UAT.json`, `appsettings.Production.json` — created by orchestrator; agent only adds missing `Saml2` sections |

---

### Primary Functions (Responsibilities)

| # | Responsibility | Description |
|---|---|---|
| 1 | SAML2 Middleware Wiring | Configures `AddAuthentication` + `AddCookie` + `AddSaml2` in `Program.cs`; wires SP certificate placeholder |
| 2 | `appsettings.json` SAML Section | Adds `Saml2` config block with all keys as placeholders (`SPEntityId`, `IdPMetadataUrl`, `AcsPath`, `SloPath`, `SPCertificateThumbprint`) |
| 3 | AuthnRequest / Response Handling | Validates SAML assertions (issuer, audience, signature, lifetime); enforces structured logging with no PII; handles encrypted assertions |
| 4 | ACS Handler | Verifies the ACS path in config matches the Entra app registration; handles `ReturnUrl` redirect after successful assertion processing |
| 5 | Single Logout (SLO) | SP-initiated: `SignOutAsync` for both schemes + Entra redirect. IdP-initiated: inbound `<LogoutRequest>` cleared, `<LogoutResponse>` returned |
| 6 | Session Expiry | Detects expiry (`SessionNotOnOrAfter`) and triggers `ChallengeAsync` — no silent renew |
| 7 | Windows Auth Removal | (`WindowsAuthToSAML` only) Inventories all `WindowsIdentity` call sites, removes `AddNegotiate`, replaces all usages in a single pass |
| 8 | Claims Normalisation | Implements `IClaimsTransformation`; confirms whether Entra emits group GUIDs, display names, or App Role values before writing `RoleMap`; preserves all existing role gates |

---

### Outputs Produced

| Output | Location | Notes |
|---|---|---|
| Updated `Program.cs` | Source root | SAML2 + cookie middleware wired |
| Updated `.csproj` | Project root | SAML library package added with pinned version |
| Updated `appsettings*.json` | Project root | `Saml2` section added to each environment file missing it |
| `ClaimsTransformation.cs` | Application services layer | `IClaimsTransformation` implementation |
| xUnit test stubs | Test project | Happy path, SLO, claims, session expiry, role equivalence; parameterised per environment |
| `appsettings.json` key reference | Documentation | Each placeholder key explained |
| Entra ID registration checklist | Documentation | Enterprise app, SAML SSO, attribute/claim mapping steps |
| Troubleshooting table | Documentation | Common SAML errors, causes, and fixes |
| IIS / hosting change notes | Documentation | Windows Auth disabled, Anonymous Auth enabled |

---

### Quality Gates (All Must Pass Before Merge)

| Gate | Requirement |
|---|---|
| SonarQube | Zero new critical or high-severity findings |
| Code coverage | ≥80% line coverage from xUnit tests (verified by CI pipeline) |
| Secret scanning | No credentials or tokens in committed files |
| Human review | Second human reviewer mandatory (HIGH RISK — Defra AI Toolkit) |
| AI transparency | PR description must disclose AI assistance and name the second reviewer |
| `.copilotignore` | `appsettings.UAT.json`, `appsettings.Production.json`, `ENTRA-REGISTRATION.md` must be present |

---

### Guardrails Summary

**The agent will:**
- Wire all auth through `Program.cs` (single entry point)
- Store all SAML config in `appsettings.json` using placeholders only
- Implement claims normalisation via `IClaimsTransformation` registered in DI
- Preserve every existing role gate — no silent access changes
- Create a feature branch for all changes; never commit to `main`
- Disclose AI assistance in the PR description

**The agent will never:**
- Scaffold local sign-in/sign-out UI pages (Entra handles the redirect cycle)
- Generate refresh-token code (SAML2 has no refresh tokens)
- Introduce OIDC concepts (`nonce`, `access_token`, PKCE, MSAL refresh)
- Use OWIN middleware
- Add a NuGet package without wiring at least one concrete usage
- Overwrite `appsettings.{Environment}.json` files owned by the orchestrator
- Rename a public method without grepping all call sites first
- Bypass SonarQube, coverage, or peer-review gates
- Feed SAML assertion payloads or personal data to an AI model without a confirmed Defra data handling agreement

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | **HIGH** |
| Human oversight required | Yes — at every stage |
| Second reviewer required | Yes — mandatory |
| Data protection | UK GDPR applies (PII in SAML assertions) |
| Defra SDS standards applied | C# Coding, Security, Git Branching, Logging, Credential Exposure |

---

## Change Log

### v1.1 — 2026-07-29 — Defra SDS & AI Toolkit Alignment

**Reason for change:** Align the agent with Defra Software Development Standards — GitHub Copilot guide (https://defra.github.io/software-development-standards/guides/github_copilot/) and the Defra AI Toolkit Deliver with AI guidelines (https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai).

| # | Area | Change made | Standard / Requirement |
|---|---|---|---|
| 1 | Governance | Added **Compliance & Governance** section covering risk classification, SDS alignment table, UK GDPR data protection rules, and `.copilotignore` requirements | Defra AI Toolkit — Deliver with AI |
| 2 | Governance | Classified agent work as **HIGH RISK** under the AI Toolkit; documented all four high-risk requirements (human review, AI transparency, second reviewer, no quality gate bypass) | Defra AI Toolkit — Deliver with AI |
| 3 | Governance | Added **Defra SDS Alignment table** mapping C# Coding Standards, Security Standards, Git Branching Strategy, Credential Exposure Process, and Logging Standards to how this agent meets each | Defra SDS |
| 4 | Data Protection | Added **UK GDPR rules** for SAML assertion data: no raw XML logging, no claim storage beyond session lifetime, no forwarding to third parties without documented basis | UK GDPR / Defra data handling policies |
| 5 | Security | Added **`.copilotignore` requirements** block listing `appsettings.UAT.json`, `appsettings.Production.json`, and `ENTRA-REGISTRATION.md` | auth-aspnetcore.instructions.md / SDS Security |
| 6 | Security | Added `ENTRA-REGISTRATION.md` and `**/ENTRA-REGISTRATION.md` entries to the repository `.copilotignore` file | auth-aspnetcore.instructions.md |
| 7 | Logging | Added structured logging (Serilog) requirement to Responsibility 3 (AuthnRequest / Response Handling): log only opaque `NameIdentifier` GUIDs, never email / display name / group values | Defra SDS Logging Standards |
| 8 | Testing | Added **≥80% line coverage threshold** to Tests section B; referenced CI pipeline (Azure DevOps) enforcement | Defra SDS Testing Standards |
| 9 | Documentation | Strengthened Documentation section C: SonarQube must have zero new critical/high findings; added `.copilotignore` confirmation step; added Defra credential exposure process reference | Defra AI Toolkit / SDS Security |
| 10 | Guardrails (Do) | Added three new guardrail rules: `.copilotignore` confirmation, AI transparency disclosure in PR description, Defra credential exposure process | Defra AI Toolkit / SDS |
| 11 | Guardrails (Don't) | Added two new rules: no bypass of quality gates using AI as justification; no feeding of personal SAML data to AI without a Defra data handling agreement | Defra AI Toolkit / UK GDPR |
| 12 | Working Approach | Added **Step 1: Create feature branch** following Defra Git Branching Strategy (`feature/identity-migration-saml-<appname>`); renumbered subsequent steps | Defra SDS Git Branching Strategy |
| 13 | Working Approach | Added **Step 12: AI disclosure in PR description** as final mandatory step; steps 9 and 10 updated to reference coverage gate and pinned NuGet version | Defra AI Toolkit — Deliver with AI |
| 14 | NuGet | Added **version pinning guidance** to SAML Library Selection section: exact version required in `.csproj`, no floating ranges, version recorded in PR description | Defra SDS Dependency Management |
| 15 | References | Added five new references: Git Branching Strategy, Logging Standards, Credential Exposure Process, Deliver with AI, Keeping Data Safe; standardised "Defra" casing (was "DEFRA") | Defra SDS / AI Toolkit |
| 16 | Front matter | Updated "Last reviewed" comment to 2026-07-29; added trigger condition for Defra AI Toolkit guidance changes | Maintenance |

---

### v1.0 — 2026-07-23 — Initial Version

Initial creation of the Identity Migration SAML Sub-Agent covering:

- SAML 2.0 middleware wiring for `WindowsAuthToSAML` and `NewSAML` scenarios
- Sustainsys.Saml2 and ITfoxtec library selection guidance
- ACS handler, SLO bindings, session expiry, Windows Auth removal
- Claims normalisation via `IClaimsTransformation`
- xUnit test stubs parameterised per environment
- Documentation requirements including troubleshooting table
- SonarQube gate and second reviewer requirement for security-critical path
- References to `auth-aspnetcore.instructions.md`, Defra SDS C#/Security standards, and Defra AI Config Examples

---

*This document is maintained alongside `.github/agents/identity-migration-saml.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
