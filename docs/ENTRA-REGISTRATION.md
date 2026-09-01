# Entra ID App Registration Checklist — Histopathology System

> **SECURITY:** This file is listed in `.copilotignore` — do not commit real tenant/client IDs or URLs
> to source control. Fill placeholders below after the Entra admin configures the Enterprise Application.
> See also: [Defra credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)

---

## 1. Enterprise Application Settings

| Setting | Required value | Notes |
|---|---|---|
| App type | Non-gallery application | SSO via SAML |
| Application name | `HistopathologySystem` | Matches the legacy app name |
| Assignment required | **No** | App `tblUser` table is the access gate; see `EntraID-Authentication-Process-Flow.md` |
| Visible to users (My Apps) | No (recommended) | Prevents app appearing for users with no DB row |
| Supported account types | Single tenant (APHA) | `accounts in this organizational directory only` |

---

## 2. SAML Single Sign-On Configuration

| Setting | Placeholder | Where to find / how to set |
|---|---|---|
| **Identifier (Entity ID)** | `__SP_ENTITY_ID_PLACEHOLDER__` | Set to the app's public URL, e.g. `https://histo.apha.gov.uk` |
| **Reply URL (ACS)** | `__ACS_URL_PLACEHOLDER__` | `https://{app-host}/Saml2/acs` |
| **Sign-on URL** | `__SIGN_ON_URL_PLACEHOLDER__` | `https://{app-host}/Saml2/login` |
| **Logout URL** | `__LOGOUT_URL_PLACEHOLDER__` | `https://{app-host}/Saml2/slo` |
| **NameID format** | `emailAddress` | Entra ID → SAML → NameIdentifier claim |
| **Signature algorithm** | `RSA-SHA256` | Set in Signing section |
| **Signing certificate** | Download `.cer` from Entra ID portal | Used to validate assertions in `appsettings.json` |

---

## 3. Attribute / Claim Mapping

Configure these in: Enterprise Application → Single sign-on → Attributes & Claims

| Claim name | Source attribute | Notes |
|---|---|---|
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | `user.mail` | Primary lookup key in `tblUser.Email` |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` | `user.displayname` | Display name |
| `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` | `user.userprincipalname` | NameID |

---

## 4. Required `appsettings.json` Keys

After registration, populate these values in each environment's `appsettings.{env}.json`:

| Key | Description |
|---|---|
| `Saml2:Issuer` | SP Entity ID — must match "Identifier" above |
| `Saml2:SingleSignOnDestination` | Entra ID SSO URL — from SAML SSO setup page |
| `Saml2:SingleLogoutDestination` | Entra ID SLO URL — from SAML SSO setup page |
| `Saml2:IdPSigningCertificate` | Base64-encoded Entra ID signing certificate (`.cer` → base64) |
| `Saml2:SPCertificateThumbprint` | SP signing cert thumbprint — from IIS/Key Vault cert store |

---

## 5. Tenant / Application IDs (fill after registration)

| Value | Placeholder |
|---|---|
| Tenant ID | `__TENANT_ID_PLACEHOLDER__` |
| Application (client) ID | `__APP_CLIENT_ID_PLACEHOLDER__` |
| Object ID | `__APP_OBJECT_ID_PLACEHOLDER__` |

---

## 6. Post-Registration Checklist

- [ ] Entra ID Enterprise Application created (SAML SSO type)
- [ ] Entity ID, ACS URL, and Sign-on URL configured
- [ ] Attribute/claim mapping set up (email, display name, NameID)
- [ ] Entra ID signing certificate downloaded and base64-encoded into `appsettings.{env}.json`
- [ ] `appsettings.Development.json` SSO/SLO URLs populated (dev tenant or test app)
- [ ] SP signing certificate provisioned for non-dev environments
- [ ] `ISS-009` data migration run: `tblUser.Email` values verified against Entra ID UPNs
- [ ] Dry-run reconciliation report produced and reviewed before go-live
- [x] `Login.cshtml` and `Login.cshtml.cs` decommissioned — deleted 2026-08-28 (see `docs/ADR/ADR-006-manual-login-page-bridge.md`)
