# Entra ID Authentication & Access Control — Process Flow

**System:** Histopathology System  
**Auth approach:** Entra ID (authentication) + Internal Users table (authorisation)  
**Last updated:** 2026-08-21

---

## Overview

The Histopathology System uses a two-gate access model:

| Gate | Responsibility | Technology |
|---|---|---|
| **Gate 1 — Authentication** | Verify identity (who you are) | Microsoft Entra ID (OIDC/SAML) |
| **Gate 2 — Authorisation** | Verify access rights (what you can do) | Application `tblUser` database table |

---

## Authentication Flow

```
User launches app URL
        │
        ▼
┌─────────────────────────┐
│   Entra ID Login Prompt  │
│   (SSO / MFA enforced)  │
└────────────┬────────────┘
             │
    ┌────────▼─────────┐
    │  Valid credentials?│
    └────────┬──────────┘
         No  │  Yes
             │
    ┌────────▼──────────────────────┐
    │  Entra ID issues access token  │
    │  (identity confirmed)          │
    └────────┬──────────────────────┘
             │
    ┌────────▼───────────────────────────┐
    │  App reads UPN from token claim     │
    │  e.g. durai.sila@apha.gov.uk        │
    └────────┬───────────────────────────┘
             │
    ┌────────▼────────────────────────────┐
    │  UserService.ResolveUserAsync()      │
    │  Looks up UPN in tblUser.NTLogin     │
    └────────┬────────────────────────────┘
             │
    ┌────────▼─────────────────────┐      ┌──────────────────────────┐
    │  Row found AND Active = 1?   │  No  │  Redirect to             │
    │                              ├─────►│  AccessDenied.cshtml     │
    └────────┬─────────────────────┘      └──────────────────────────┘
             │ Yes
    ┌────────▼──────────────────┐
    │  Session populated with   │
    │  Name / Group / Area      │
    └────────┬──────────────────┘
             │
    ┌────────▼──────────────────┐
    │  Application home page    │
    │  (role-based panels shown)│
    └───────────────────────────┘
```

---

## Enterprise Application Configuration

| Setting | Value | Reason |
|---|---|---|
| **Assignment required** | **No** | App database is the access gate; Entra ID handles identity only |
| **Visible to users (My Apps)** | No (recommended) | Prevents app appearing for users with no DB account |
| **Supported account types** | Single tenant (APHA) | Only APHA tenant users + invited guests |

### Why Assignment Required = No

- The application's `tblUser` table is the **single source of truth** for access control
- Setting to `Yes` would require maintaining two separate user lists (Entra ID assignments + DB rows), creating duplicate administration and risk of inconsistency
- Any authenticated user not in the DB is blocked at Gate 2 regardless of Entra ID assignment status

### Recommended approach

**Set "Assignment required" = No** — Entra ID handles authentication only; the application handles authorisation:

```
Entra ID (Authentication only)          Your App (Authorisation)
┌──────────────────────────┐            ┌─────────────────────────────┐
│  Any tenant user can     │  Token →   │  UserService.ResolveUser()  │
│  authenticate and get    │ ─────────► │  checks tblUser.Active = 1  │
│  an access token         │            │  else → AccessDenied.cshtml │
└──────────────────────────┘            └─────────────────────────────┘
```

**Why NOT "Assignment required = Yes":**

- You'd need to maintain two separate lists: Entra ID assignments AND the DB Users table
- Duplicate administration burden — adding a new user requires work in both Azure portal and the app
- If someone is removed from the DB but not from Entra ID assignments (or vice versa), access control becomes inconsistent
- For a closed internal system like a histopathology lab, DB-only gating is simpler and the single source of truth

### Implementation checklist — ADR-006 bridge → Phase B migration

| Step | Setting | Rationale |
|---|---|---|
| Enterprise App → Properties → Assignment required | **No** | Any authenticated tenant user can attempt login; the DB is the actual gate |
| Enterprise App → Properties → Visible to users | **No** (optional) | Prevents the app appearing in My Apps for users who have no DB row |
| `Login.cshtml` / future Entra ID OIDC handler | Check `UserService.ResolveUserAsync()` result | If null or `Active = false` → redirect to `AccessDenied.cshtml` |
| `tblUser.NTLogin` column | Populate with UPN (`user@domain.com`) during Phase B data migration | Entra ID sends UPN, not `DOMAIN\username` — see ISS-009 |

---

## User Types and Access Paths

### Type 1 — Internal APHA Users (same tenant)

```
durai.sila@apha.gov.uk
        │
        ▼
Authenticate with APHA Entra ID credentials
        │
        ▼
Token issued → App checks tblUser → Access granted or denied
```

### Type 2 — Partner / Cross-Domain Users (different tenant)

For users from other government organisations (e.g. Natural England, DEFRA) who are not in the APHA tenant:

**Solution: B2B Guest Invitation**

```
Admin invites external user in Azure Portal
        │
        ▼
Guest account created in APHA tenant
(user retains their own org credentials)
        │
        ▼
Admin adds guest UPN to tblUser in application DB
        │
        ▼
User launches app URL
        │
        ▼
Entra ID prompts → user signs in with their own org credentials
        │
        ▼
Guest token issued → App checks tblUser → Access granted or denied
```

**Supported external domains:**

| Domain | Organisation | Access method |
|---|---|---|
| `@apha.gov.uk` | APHA (host tenant) | Direct |
| `@defra.gov.uk` | DEFRA | B2B Guest Invitation |
| `@naturalengland.org.uk` | Natural England | B2B Guest Invitation |
| Other gov domains | As required | B2B Guest Invitation |

---

## User Onboarding Process

### Adding a new user (internal or guest)

```
1. New user exists in Entra ID (directly or as invited guest)
        │
2. Admin adds a row to tblUser:
   - NTLogin = user UPN (e.g. durai.sila@apha.gov.uk)
   - Name, Group, Area populated
   - Active = 1
        │
3. User launches app and authenticates via Entra ID
        │
4. App resolves UPN → DB row found → access granted
```

### Removing a user

```
1. Admin sets tblUser.Active = 0 for the user's row
        │
2. On next login attempt:
   - Entra ID authentication succeeds (identity still valid)
   - App DB check fails (Active = 0)
   - User sees Access Denied page
        │
3. No Entra ID action required
   (guest invitation can be removed separately if needed)
```

---

## Key Technical Prerequisite (ISS-009)

Before Entra ID integration goes live, the `tblUser.NTLogin` column must be migrated from Windows domain format to UPN format:

| Current format (legacy) | Required format (Entra ID) |
|---|---|
| `APHA\durai.sila` | `durai.sila@apha.gov.uk` |
| `DOMAIN\username` | `username@domain.com` |

**Action required:** One-time data migration of all existing `NTLogin` values to UPN format before Phase B go-live.

---

## Security Boundaries

```
┌─────────────────────────────────────────────────────────┐
│  APHA Azure AD Tenant                                    │
│                                                          │
│  ┌─────────────────┐    ┌────────────────────────────┐  │
│  │  Internal Users  │    │  B2B Guest Users           │  │
│  │  @apha.gov.uk    │    │  @defra.gov.uk             │  │
│  │                  │    │  @naturalengland.org.uk     │  │
│  └────────┬─────────┘    └────────────┬───────────────┘  │
│           │                           │                   │
│           └──────────┬────────────────┘                  │
│                      │                                    │
│              Entra ID Token                               │
└──────────────────────┼────────────────────────────────────┘
                       │
        ┌──────────────▼──────────────────┐
        │  Histopathology Application      │
        │                                  │
        │  UserService.ResolveUserAsync()  │
        │  ↓                               │
        │  tblUser (NTLogin / Active)      │
        │  ↓                               │
        │  Access Granted / Denied         │
        └──────────────────────────────────┘
```

---

## Related Documents

| Document | Location |
|---|---|
| Entra ID implementation plan | `docs/EntraID-Implementation-plan.md` |
| ADR-006 Manual login bridge | `docs/ADR/ADR-006-manual-login-page-bridge.md` |
| Migration run journal | `docs/migration-run-journal.md` |
| Risk register (R-001, ISS-009, ISS-011) | `docs/Risk-and-Governance.md` |
