# Copilot Global Instructions (C# + ASP.NET Core + .NET 10)

These instructions apply to all GitHub Copilot interactions in this repository, including custom agents under `.github/agents/`.
This file is the shared "brain" that keeps changes consistent, secure, and reviewable across the team.

---

## Repo Context (Assumed)
- Code is **C# on ASP.NET Core (.NET 10)** — the VB.NET / .NET Framework 4.8 migration is complete.
- All project files target `net10.0`; SDK-style `.csproj` format; `Program.cs` minimal hosting with no `Startup.cs` or `Global.asax`.
- Authentication has been migrated from **Windows Authentication** to **Azure Entra ID** (SAML 2.0 or OIDC depending on the application).
- Do **not** introduce .NET Framework patterns, OWIN middleware, `web.config` auth sections, or `System.ServiceProcess` references.

---

## Golden Rules (Security & Safety)
1. **Never output or commit secrets** (client secrets, certificates, tokens, connection strings). Use placeholders like `__CLIENT_ID__`, `__TENANT_ID__`, `__AUTHORITY__`.
2. **Never log tokens or full claims payloads**. If diagnostics are needed, log only non-sensitive opaque identifiers (e.g., `NameIdentifier` GUID) and redact all other values.
3. **Avoid behavioral drift**: do not change authorization outcomes unless the change is explicitly described and tested.
4. **Prefer minimal, incremental changes**: small PRs, clear diffs, easy rollback.
5. **No `WindowsIdentity` references**: the codebase is post-migration — `WindowsIdentity`, `AddNegotiate()`, and `NegotiateDefaults` must not appear in any new or modified code.

---

## How to Work in This Repo (Required Process)
When implementing changes (especially auth-related):

### 1) Start with discovery
- Confirm the project targets `net10.0` and uses ASP.NET Core minimal hosting.
- Identify where authentication and authorization are currently enforced (`[Authorize]` attributes, `IClaimsTransformation`, `ClaimsPrincipal` accessors).
- Identify all claim usage points and normalize them through the existing `ClaimsTransformation` mapping layer — do not add inline claim parsing.

### 2) Make changes in a consistent structure
- Centralize:
  - auth bootstrap/configuration in `Program.cs`
  - claims normalization/mapping in `IClaimsTransformation`
  - session/expiry/renew behaviors via cookie middleware configuration
- Avoid scattered inline claim parsing across Razor Pages, controllers, or services.
- Wire configuration via `appsettings.json` / `IOptions<T>` — never hard-coded values.

### 3) Always provide a "change ledger"
For every meaningful change-set, include a short `AUTH_CHANGELOG.md` (or append to `CHANGELOG.md`) with:
- files changed
- key methods touched
- summary of auth behavior changes
- validation performed

---

## Coding Conventions (C# / .NET 10)
- Follow [Defra SDS C# Coding Standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/): standard C# naming, `async`/`await` throughout, `IOptions<T>` for configuration.
- Do not rewrite unrelated code — scope changes to what is explicitly requested.
- Prefer explicit error handling for auth/claims issues:
  - clear failure paths (challenge / deny / config error)
  - no silent catches that swallow auth exceptions
- Use **structured logging via Serilog** — log only opaque identifiers; never log PII, claim values, or token contents.
- Use `dotnet build` and `dotnet test` (cross-platform) — never `msbuild` or `nuget.exe` for `.NET 10` projects.

---

## Testing & Validation (Non-Negotiable for Auth Changes)
Any authentication / claims changes must include:

1) **Build verification**
- `dotnet restore` then `dotnet build --configuration Release` — zero errors required.

2) **Runtime flow verification**
- Sign-in redirect round-trip (SAML ACS or OIDC callback)
- Token / assertion validation works (issuer / audience / lifetime)
- Session expiry triggers re-auth challenge
- Logout clears session and redirects correctly

3) **Claims mapping verification**
- Roles/groups mapping preserved — same allow/deny outcomes as pre-migration
- Missing optional claims handled safely (no null reference exceptions)
- Authorization results remain equivalent to pre-migration

4) **Coverage threshold**
- Authentication code must achieve >= 80% line coverage from xUnit tests before the PR is raised.

5) **Local testing approach**
- Where feasible, use a **mock SAML / OIDC identity provider** for local validation of claims mapping, then document how to switch to real Entra ID settings.

---

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Logging standards](https://defra.github.io/software-development-standards/standards/logging/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

## Output Expectations (How Copilot should respond)
When you propose or implement changes, always output:

### Summary
- What changed and why

### Files Changed
- A bullet list of file paths + what changed

### Key Decisions
- Claims mapping rules
- Session/renew strategy
- Validation approach

### Configuration Keys
- List of required keys (placeholders only, no secrets)

### Verification
- What was built/tested and what outcome is expected