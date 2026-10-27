# Auth Changelog

## 2026-10-22 — SAML 2.0 → OpenID Connect (branch `feature/OIDC`)

**Behaviour change:** sign-in uses Entra ID OIDC (authorization code + PKCE, `prompt=login`) via
Microsoft.Identity.Web instead of ITfoxtec SAML. Authorisation is unchanged — group/area still come from `tblUser`.

| File | Change |
|---|---|
| `src/Histo.Web/Histo.Web.csproj` | ITfoxtec packages removed; `Microsoft.Identity.Web` added |
| `src/Histo.Web/Program.cs` | SAML config/metadata/middleware removed; `AddMicrosoftIdentityWebApp`; OIDC events (`OnRedirectToIdentityProvider[ForSignOut]`, `OnTokenValidated`, `OnRemoteFailure`); cookie scheme `Cookies` keeps name, idle timeout, single-session check and `/SessionError` redirect; `LoginPath` `/auth/login` |
| `src/Histo.Web/Controllers/AuthController.cs` | `/Saml2/*` replaced by `GET /auth/login` and `POST /auth/signout` |
| `src/Histo.Web/Auth/OidcSignInHandler.cs` | New — former ACS logic: `tblUser` lookup, app claims, session stamp |
| `src/Histo.Web/Auth/OidcRedirects.cs` | New — `prompt=login`, `PublicOrigin`-based `redirect_uri` / `post_logout_redirect_uri` |
| `src/Histo.Web/Auth/EntraIdClaims.cs` | New — email/UPN and object-ID lookup shared by sign-in and claims transformation |
| `src/Histo.Web/Auth/HistopathologyClaimsTransformation.cs` | Logs object ID instead of NameIdentifier; `ApplyAppClaims` shared |
| `src/Histo.Web/Pages/HistoPageModel.cs`, `Auth/MaintenanceOnlyPageFilter.cs` | Challenge `Cookies` instead of `saml2` |
| `src/Histo.Web/appsettings*.json` | `Saml2` section → `AzureAd` (placeholders) |
| `.azure-devops/pipeline.yaml` | DEV stage: `Saml2__*` → `AzureAd__*`; new `oidcClientId` / `oidcClientSecretUri` variables (placeholders). TST/PRE still SAML settings |
| `tests/Histo.Tests/Unit/*` | SAML SLO tests removed; controller, sign-in handler, redirect and claims-transformation tests added |

**Claims:** `email` → `preferred_username` fallback for the `tblUser.Email` lookup; `oid` is the only identity value logged.

**Validation:** `dotnet build` clean; 982 tests pass; auth classes 100 % line coverage. Locally verified: protected page → 302
`/auth/login?ReturnUrl=…` → 302 Entra `/oauth2/v2.0/authorize` with `response_type=code`, `code_challenge_method=S256`,
`response_mode=form_post`, `prompt=login`, `redirect_uri=https://dev-cde.azure.defra.cloud/signin-oidc`.
Full sign-in, timeout, single-session and sign-out flows still to be verified in dev.

## 2026-10-27 — Harden live Azure OIDC configuration guard

**Behaviour change:** startup now rejects placeholder or missing Entra values before the app starts, preventing broken live deployments from silently accepting invalid `AzureAd__*` settings.

| File | Change |
|---|---|
| `src/Histo.Web/Auth/AzureAdConfigurationValidator.cs` | New validator for `TenantId`, `ClientId`, `ClientSecret`, and placeholder `PublicOrigin` detection |
| `src/Histo.Web/Program.cs` | Replaced ad hoc startup checks with the shared validator |
| `tests/Histo.Tests/Unit/AzureAdConfigurationValidatorTests.cs` | Added regression tests for placeholder rejection and valid configuration acceptance |

**Validation:** `dotnet test tests/Histo.Tests/Histo.Tests.csproj --filter "AzureAdConfigurationValidatorTests" --nologo` plus the existing OIDC/session tests. This catches the exact live-environment issue where placeholder values can slip through and break the Entra redirect flow.
