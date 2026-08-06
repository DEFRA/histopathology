# Auth Bootstrap Checklist (Entra ID, .NET Framework 4.8)

## Goal
Introduce Entra ID authentication in a centralized way while keeping the application’s behavior stable.

## Discovery checklist
- Identify where authentication is configured:
  - `web.config` authentication/authorization sections
  - Application startup entry points (e.g., Global.asax, modules, startup classes)
- Identify current Windows Authentication dependencies:
  - `HttpContext.Current.User`
  - `WindowsIdentity`
  - `IsInRole(...)`
- Identify existing redirect behavior:
  - login landing pages
  - post-auth return URL behavior
  - any custom “sign out” link behavior

## Bootstrap decisions (record explicitly)
- Where authentication initializes (single place)
- How unauthenticated requests are challenged
- How failures are handled (config missing, token invalid, claims missing)
- Which configuration keys are required (placeholders only)

## Output artifact
- `AUTH_CHANGELOG.md` entry listing:
  - Files changed
  - Summary of behavior change (should be “no authz drift”)
  - How to validate locally