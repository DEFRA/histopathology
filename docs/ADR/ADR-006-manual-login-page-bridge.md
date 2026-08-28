# ADR-006 — Manual NTLogin Bridge Page (Pre-Entra ID)

**Date:** 2026-08-07
**Status:** Superseded by Entra ID SAML 2.0 authentication — decommissioned 2026-08-28
**Deciders:** LAP Civica Migration Team

---

## Context

`Environment.UserDomainName` / `Environment.UserName` is unreliable on
non-domain-joined developer machines — it returns the local machine name
(`WORKGROUP\sd000106`) rather than the corporate AD identity.  
`HttpContext.User.Identity.Name` is always empty under Kestrel because Windows
Authentication is an IIS-only feature; it is not wired in the migrated .NET 10
app at this stage.

As a result, the auto-NT-detection logic in `HistoPageModel` cannot resolve any
real user from the database, and every page redirects to `/AccessDenied`.

Entra ID SAML 2.0 integration (Phase B of
[`docs/EntraID-Implementation-plan.md`](../EntraID-Implementation-plan.md))
is the correct long-term solution but is not yet implemented.

---

## Decision

Introduce `Login.cshtml` / `Login.cshtml.cs` as a **temporary bridge**:

- `HistoPageModel.OnPageHandlerExecutionAsync` is simplified to a pure **session
  gate** — if `Session.GroupName` is empty, redirect to `/Login`.
- `Login.cshtml` renders a GDS-compliant NTLogin entry form.
- `Login.cshtml.cs` validates the entered login against the database via
  `UserService.ResolveUserAsync`, populates the session via
  `Session.PopulateFromUser`, and redirects to `/Index` on success.
- `Login.cshtml.cs` does **not** inherit `HistoPageModel` — it is outside the
  session gate to prevent a redirect loop.

---

## Files Introduced

| File | Role |
|---|---|
| `src/Histo.Web/Pages/Login.cshtml` | GDS NTLogin entry form |
| `src/Histo.Web/Pages/Login.cshtml.cs` | DB validation + session population |

## Files Changed

| File | Change |
|---|---|
| `src/Histo.Web/Pages/HistoPageModel.cs` | Removed auto-NT-detection; pure session gate redirecting to `/Login` |

---

## Decommission Trigger

**When:** Phase B of the Entra ID plan is complete — specifically when:
1. `ITfoxtec.Identity.Saml2.MvcCore` is registered in `Program.cs` (`AddSaml2`)
2. The SAML ACS endpoint validates the assertion and calls `Session.PopulateFromUser(user)`

**Actions at decommission:**

1. **Delete** `src/Histo.Web/Pages/Login.cshtml`
2. **Delete** `src/Histo.Web/Pages/Login.cshtml.cs`
3. **Update** `HistoPageModel.cs` — change the redirect from `/Login` to a SAML
   authentication challenge (or remove the redirect entirely if Entra ID Easy Auth
   handles the challenge at the platform layer before the request reaches the page model)
4. **Update this ADR** — set Status to `Superseded by ADR-00X (Entra ID SAML)`

---

## Searchable Marker

All three affected source files carry the comment `// BRIDGE (ADR-006):` to
make them discoverable:

```
grep -r "BRIDGE (ADR-006)" src/
```

---

## Consequences

- **Positive:** Unblocked local development and end-to-end page testing before
  Entra ID was wired.
- **Positive:** `HistoPageModel` became simpler — a single 4-line session gate (at the time).
- **Negative (accepted):** Users had to type their NTLogin manually on the login
  page; this was not a production-grade experience and has since been replaced.
- **Negative (accepted):** The login page had no brute-force protection — accepted
  for an internal dev/test bridge that has now been removed.

---

## Decommissioned — 2026-08-28

Both decommission-trigger conditions were confirmed met:

1. `ITfoxtec.Identity.Saml2.MvcCore` is registered in `Program.cs` (`AddSaml2`, `app.UseSaml2()`).
2. `AuthController` implements the SAML ACS endpoint (`POST /Saml2/Acs`), and `HistoPageModel.OnPageHandlerExecutionAsync`
   issues `new ChallengeResult("saml2")` for unauthenticated requests instead of redirecting to `/Login`.

**Actions taken:**

1. **Deleted** `src/Histo.Web/Pages/Login.cshtml`
2. **Deleted** `src/Histo.Web/Pages/Login.cshtml.cs`
3. `HistoPageModel.cs` already issues a SAML `ChallengeResult` (Gate 1) and a `RedirectToPageResult("/AccessDenied")`
   (Gate 2) — no further change needed; the bridge's session-gate redirect to `/Login` no longer exists in the codebase.
4. **This ADR updated** — Status set to `Superseded by Entra ID SAML 2.0 authentication`.

No other files referenced `Login.cshtml`/`LoginModel` at decommission time (confirmed via repo-wide search).
