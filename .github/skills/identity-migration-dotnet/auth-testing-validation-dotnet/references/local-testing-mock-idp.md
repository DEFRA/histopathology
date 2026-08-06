# Local Testing Guidance (Mock Identity Provider Pattern)

## Purpose
Support local development testing of claims mapping and auth flows without depending on external tenant readiness.

## Approach (high-level)
- Provide a local/developer-only configuration mode that can simulate identity assertions for testing claims mapping.
- Keep it isolated to development settings and ensure it is easy to switch back to real Entra configuration.

## Why this exists
Internal engineering discussions explicitly noted using a mock SAML identity provider for local testing and later switching to real configuration. [1](URL of configuration Placeholder)

## Constraints
- Do not commit real tenant IDs or secrets.
- Do not log raw tokens/claims payloads.
- Document the switch mechanism clearly (dev-only vs real Entra settings).