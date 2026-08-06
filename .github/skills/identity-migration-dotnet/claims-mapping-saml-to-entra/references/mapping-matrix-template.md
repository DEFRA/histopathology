# Claims Mapping Matrix (Template)

Use this to document mapping from incoming SAML/Entra claims to canonical claims.

| Incoming claim type | Incoming source (SAML/Entra) | Canonical claim | Required? | Notes |
|---|---|---|---|---|
| __INCOMING_TYPE__ | __SOURCE__ | canonical.user_id | Yes | stable identifier |
| __INCOMING_TYPE__ | __SOURCE__ | canonical.display_name | Yes | |
| __INCOMING_TYPE__ | __SOURCE__ | canonical.email_or_upn | Optional/Yes | depends on app needs |
| __INCOMING_TYPE__ | __SOURCE__ | canonical.roles | Yes | preserve authz semantics |
| __INCOMING_TYPE__ | __SOURCE__ | canonical.groups | Optional | only if app uses groups |

## Authorization equivalence checklist
- Each role/group-based gate yields the same allow/deny outcome pre/post migration.
- Any intentional difference must be explicitly documented and tested.