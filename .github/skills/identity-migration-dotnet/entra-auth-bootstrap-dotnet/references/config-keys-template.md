# Entra Configuration Keys (Template)

> Use placeholders only. Do not commit secrets.

## Required keys (placeholders)
- `AUTH_TENANT_ID = __TENANT_ID__`
- `AUTH_AUTHORITY = __AUTHORITY__`
- `AUTH_CLIENT_ID = __CLIENT_ID__`
- `AUTH_REDIRECT_URL = __REDIRECT_URL__`
- `AUTH_LOGOUT_URL = __LOGOUT_URL__`

## Token validation policy (placeholders)
- `AUTH_VALIDATE_ISSUER = true`
- `AUTH_VALIDATE_AUDIENCE = true`
- `AUTH_VALIDATE_LIFETIME = true`

## Notes
- Store values in the repo’s standard configuration mechanism (commonly `web.config` for .NET Framework apps).
- Document where these keys live and how local developers set them.