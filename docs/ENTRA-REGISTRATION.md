# Entra ID App Registration Checklist — Histopathology System

> **SECURITY:** This file is listed in `.copilotignore` — do not commit real tenant/client IDs or URLs
> to source control. Fill placeholders below after the Entra admin configures the Enterprise Application.
> See also: [Defra credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)

---

## 1. App Registration Settings

| Setting | Required value | Notes |
|---|---|---|
| App type | App registration (OpenID Connect) | Replaces the SAML Enterprise Application |
| Application name | `HistopathologySystem` | Matches the legacy app name |
| Assignment required | **No** | App `tblUser` table is the access gate; see `EntraID-Authentication-Process-Flow.md` |
| Visible to users (My Apps) | No (recommended) | Prevents app appearing for users with no DB row |
| Supported account types | Single tenant (APHA) | `accounts in this organizational directory only` |

---

## 2. Authentication (Web platform)

| Setting | Value |
|---|---|
| **Redirect URI** | `https://{app-host}/signin-oidc` (one per environment; add `https://localhost:{port}/signin-oidc` to dev only) |
| **Front-channel logout URL** | `https://{app-host}/signout-oidc` |
| **Implicit grant (access / ID tokens)** | Both **off** — the app uses authorization code + PKCE |
| **Client credential** | Client secret stored in Key Vault; App Service setting `AzureAd__ClientSecret` = Key Vault reference |
| **API permissions** | `Microsoft Graph → User.Read` (delegated, default) plus `openid`, `profile`, `email` |

---

## 3. Token Configuration (optional claims, ID token)

| Claim | Notes |
|---|---|
| `email` | Primary lookup key in `tblUser.Email` |
| `preferred_username` | Emitted by default; UPN fallback when `email` is absent (guest accounts) |
| `sid` | Lets `/signout-oidc` match the front-channel logout to the signed-in session |

No group or role claims are needed — groups and areas come from `tblUser`.

---

## 4. Required Configuration Keys

Set per environment as App Service settings (`AzureAd__X`):

| Key | Description |
|---|---|
| `AzureAd:TenantId` | Directory (tenant) ID |
| `AzureAd:ClientId` | Application (client) ID |
| `AzureAd:ClientSecret` | Key Vault reference only — never committed |
| `AzureAd:PublicOrigin` | Public https origin (e.g. `https://dev-cde.azure.defra.cloud`) used for `redirect_uri` behind the edge proxy |
| `AzureAd:ForceLogin` | `true` (default) sends `prompt=login`, so re-sign-in after a timeout asks for credentials |
| `Session:IdleTimeoutMinutes` | Idle timeout for the auth cookie and session (default `20`, minimum `1`) |
| `DataProtection:KeysPath` | Shared key ring — **required when running more than one instance**, or the OIDC `state` cannot be decrypted on the callback |

---

## 5. Tenant / Application IDs (fill after registration)

| Value | Placeholder |
|---|---|
| Tenant ID | `__TENANT_ID_PLACEHOLDER__` |
| Application (client) ID | `__APP_CLIENT_ID_PLACEHOLDER__` |
| Object ID | `__APP_OBJECT_ID_PLACEHOLDER__` |

---

## 6. Post-Registration Checklist

- [ ] App registration created per environment (Web platform, redirect + front-channel logout URLs)
- [ ] Optional ID token claims `email` and `sid` added
- [ ] Client secret created and stored in Key Vault; App Service has `get` access to it
- [ ] Pipeline variables `oidcClientId` / `oidcClientSecretUri` set for the stage
- [ ] SAML Enterprise Application SSO and signing certificate retired after OIDC sign-off
- [ ] `ISS-009` data migration run: `tblUser.Email` values verified against Entra ID UPNs
- [ ] Dry-run reconciliation report produced and reviewed before go-live
- [x] `Login.cshtml` and `Login.cshtml.cs` decommissioned — deleted 2026-08-28 (see `docs/ADR/ADR-006-manual-login-page-bridge.md`)
