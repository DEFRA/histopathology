# Canonical Claims Contract (App-Internal)

## Purpose
Define the minimal set of canonical claims the application uses everywhere.

## Required canonical claims (example placeholders)
- `canonical.user_id` (stable unique identifier)
- `canonical.display_name`
- `canonical.email_or_upn` (if the app requires it)
- `canonical.roles` (role list)
- `canonical.groups` (group list, if used)

## Optional canonical claims
- `canonical.department`
- `canonical.job_title`
- Other profile claims only if the app uses them

## Rules
- Required claims missing → deterministic failure path (deny or challenge)
- Optional claims missing → do not crash; treat as absent
- No direct use of raw incoming claim types outside the mapping layer