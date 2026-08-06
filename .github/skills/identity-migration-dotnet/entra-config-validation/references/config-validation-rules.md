# Entra Configuration Validation Rules

## Startup validation (fail fast)
- Required keys must exist and be non-empty:
  - tenant/authority
  - client id
  - redirect URL
  - logout URL
- If any required key is missing:
  - fail deterministically with a developer-actionable message
  - do not proceed in a partially configured state

## Runtime validation (fail safe)
- Issuer mismatch → deny/challenge
- Audience mismatch → deny/challenge
- Token lifetime invalid → deny/challenge

## Diagnostics guidance
- Do not output tokens or full claim payloads.
- Use redacted logging and high-level failure codes/messages for troubleshooting.