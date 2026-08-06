# Auth Smoke Test Checklist (Entra ID, .NET Framework 4.8)

## Build
- Solution builds successfully using repo standard process

## Sign-in flow
- Unauthenticated request → challenge/redirect to identity provider
- Successful sign-in returns to the expected page

## Token validation
- Valid token accepted
- Expired token triggers re-auth
- Invalid token denied safely

## Claims mapping
- Required canonical claims present post-login
- Missing optional claims do not crash the app

## Authorization equivalence
- Role/group access gates behave the same as pre-migration (allow/deny parity)

## Logout
- App session cleared
- Logout redirect works
- Next request triggers re-auth