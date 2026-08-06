---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
applyTo:
  - "**/*.cs"
  - "**/Program.cs"
  - "**/appsettings*.json"
  - "**/*.csproj"
---

# Authentication & Identity Instructions — ASP.NET Core (.NET 8+)

These instructions apply when GitHub Copilot is generating or modifying
authentication, identity, claims, or security-related code in **ASP.NET Core
projects targeting .NET 8, .NET 10, or newer**.

They work in addition to `.github/copilot-instructions.md`. Where they
conflict with `auth.instructions.md`, these instructions take precedence for
.NET 8+ projects.

---

## Target Architecture

- Platform: **ASP.NET Core (.NET 8, .NET 10, and newer)**
- Hosting model: **`Program.cs` minimal hosting** — no `Startup.cs`, no OWIN, no `web.config`
- Identity Provider: **Azure Entra ID**
- Auth protocols: **SAML 2.0** (Sustainsys or ITfoxtec) or **OIDC + PKCE** (Microsoft.Identity.Web)

Do **not** propose:
- `Global.asax`, `Application_Start`, `HttpModule` patterns
- OWIN middleware (`app.Use`, `IAppBuilder`)
- `web.config` `<system.web>` authentication configuration
- .NET Framework `WindowsIdentity` or Windows Integrated Auth patterns

---

## Mandatory Design Rules

### 1. Centralisation
- All authentication setup must be in `Program.cs` via `builder.Services.AddAuthentication()`.
- All claims handling must flow through a single `IClaimsTransformation` implementation registered in DI.
- Do not scatter claim parsing across controllers, Razor Pages, or services.

### 2. Claims Handling
- Normalise all incoming Entra ID claims into canonical internal claim names via `IClaimsTransformation`.
- Never assume optional claims exist — handle missing claims with safe defaults or explicit failure.
- Preserve existing authorisation semantics (roles/groups) unless explicitly instructed otherwise.
- Before writing any `ClaimsMapper`, confirm whether Entra ID emits group Object IDs, display names, or App Role values — an incorrect `RoleMap` silently denies all role-based access.

### 3. Token and Session Safety
- Never log tokens, raw claims payloads, secrets, or keys.
- Token lifetime enforcement is mandatory: issuer, audience, lifetime, signature.
- Session expiry must trigger a **re-auth challenge** (`ChallengeAsync`), not silent failure.
- Do not set `SaveTokens = false` or disable nonce validation on OIDC middleware.
- Never store raw tokens in unencrypted cookies, `ViewState`, or `localStorage`.

### 4. Configuration Discipline
- All auth configuration must be in `appsettings.json` under `AzureAd` (OIDC) or `Saml2` (SAML) sections.
- Use placeholder values only — never hardcode tenant IDs, client IDs, redirect URLs, or entity IDs.
- Client secrets must never be committed to source control — reference Key Vault in production.
- Document every configuration key when introduced.

### 5. Human Approval Gate
- Before writing any files during a migration, present the complete list of planned changes to the user and wait for explicit approval.
- AI-generated authentication code must not be committed without human review. A second reviewer is required for security-critical auth changes.

### 6. SonarQube Gate
- All AI-generated code must pass SonarQube static analysis before merge.
- Reference: DEFRA AI Security guidance.

### 7. Secret Scanning
- Add `appsettings.UAT.json`, `appsettings.Production.json`, and `ENTRA-REGISTRATION.md` to `.copilotignore` to prevent Copilot indexing production configuration.
- If real credentials are accidentally committed, follow the DEFRA credential exposure process: https://defra.github.io/software-development-standards/processes/credential_exposure/

---

## NuGet Package Rules

- Only add a package when at least one concrete usage is wired in the same change. No speculative packages.
- OIDC (preferred): `Microsoft.Identity.Web`
- OIDC (Graph calls only, explicit requirement): `Microsoft.Identity.Web.MicrosoftGraph`
- SAML (default): `Sustainsys.Saml2.AspNetCore2`
- SAML (when ITfoxtec already referenced): `ITfoxtec.Identity.Saml2`

---

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Logging standards](https://defra.github.io/software-development-standards/standards/logging/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)

## Testing Requirements

When modifying authentication code, ensure the following are addressed:

- Unauthenticated request → challenge/redirect → authenticated return
- Valid token accepted; expired/invalid token rejected
- Session expiry triggers re-auth challenge, not silent failure
- Logout clears session and redirects correctly
- Required claims present post-login; missing optional claims handled safely
- Roles/groups mapping produces same authorisation result as pre-migration

---

## Output Expectations

When proposing or implementing auth-related changes, include:

### Summary
What changed and why (auth/identity focused)

### Files Changed
Exact list of files and nature of changes

### Claims and Session Decisions
- Claims mapped (old → new)
- Session expiry / renew behaviour

### Configuration Keys
List of required `appsettings.json` keys (placeholders only)

### Validation
What flows were validated or must be validated by the developer before merge
