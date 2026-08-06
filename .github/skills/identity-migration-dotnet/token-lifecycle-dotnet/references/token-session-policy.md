# Token & Session Policy (Entra ID, .NET Framework 4.0)

## Objectives
- Enforce token validation (issuer, audience, lifetime).
- Ensure deterministic session behavior (expiry → re-auth; invalid token → deny/challenge).
- Avoid storing or logging sensitive token material.

## Session lifecycle rules (write what the app will do)
- When session is valid:
  - Requests proceed using the canonical identity/claims principal.
- When token is expired:
  - Trigger re-authentication (challenge/redirect).
- When token is invalid:
  - Deny access or challenge (no partial authentication state).
- When logout occurs:
  - Clear any server session markers and redirect to configured logout URL.

## Security constraints
- Never log raw tokens or claims payloads.
- Never persist raw tokens in session unless explicitly required.
- Prefer storing minimal identifiers (e.g., stable user id, correlation id).