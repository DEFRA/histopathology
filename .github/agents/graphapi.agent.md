---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: GraphAPI Send Mail Agent
description: |
  This agent is specific to 4 civica applications (BSE, Histo, D2R2 and PTLIMS). Sends email via Microsoft Graph API. Supports application credential (client credentials) flows
  and will attempt to send mail as a specified user when the application has the correct Graph permissions.
  The agent wraps token acquisition (MSAL) and the Graph `sendMail` endpoint, with robust error handling
  and helpful diagnostics suitable for automation or use by other agents.
skills:
  - .github/skills/graph-api-sendmail/send-mail.skill.md
tags:
  - graph
  - email
  - msal
  - azure
  - oauth2

---

## Purpose

Send email messages using Microsoft Graph API. Designed for automation scenarios where an agent
needs to deliver notifications, reports, or alerts. The agent supports attachments (file size limits apply),
CC/BCC, and both HTML and plain-text bodies. It uses the OAuth2 client credentials flow via MSAL to obtain
an access token scoped to `https://graph.microsoft.com/.default` and then posts to the Graph `sendMail` API.

## Required Application Permissions

- `Mail.Send` (Application) — to send mail as any user with application permissions, or
- `Mail.Send` (Delegated) — when user-delegated tokens are available.

Note: Application permissions to send as arbitrary users require admin consent and careful security review.

## Inputs

- `tenant_id` (string) — Azure AD tenant id
- `client_id` (string) — App (client) id
- `client_secret` (string) — App client secret (sensitive)
- `sender` (string) — userPrincipalName or id to send mail as (e.g., `no-reply@example.com`)
- `to` (list[string]) — list of recipient email addresses
- `subject` (string) — message subject
- `body` (string) — message body (HTML or plain)
- `content_type` (string) — `HTML` or `Text` (default: `HTML`)
- `cc` (list[string]) — optional CC recipients
- `bcc` (list[string]) — optional BCC recipients
- `attachments` (list[object]) — optional attachments with fields: `name`, `bytes_base64`
- `save_to_sent` (bool) — whether to save to Sent Items (default: true)

## Outputs

- `success` (bool)
- `status_code` (int) — HTTP status code returned by Graph
- `response` (object|string) — parsed JSON response or error text
- `error` (string) — error message, null when success

## Examples

Send a simple email using CLI in the skill folder (see skill docs):

```powershell
python .github/skills/graph-api-sendmail/graph_api_send_mail.py \
  --tenant-id <TENANT_ID> --client-id <CLIENT_ID> --client-secret <CLIENT_SECRET> \
  --sender no-reply@example.com --to user1@example.com user2@example.com \
  --subject "Alert" --body "<p>Build finished</p>"
```

## Error handling

The skill provides detailed errors and exit codes for common failure modes:
- Authentication failures (invalid client credentials, tenant misconfiguration)
- Network/transport errors
- Graph API HTTP errors (4xx/5xx) with Graph error body returned when available
- Attachment-limit and encoding checks

Use the returned `error` field for programmatic handling; this agent never prints secrets to logs.

---

## Compliance & Governance

Classified as **MEDIUM RISK** under the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Requires:

- **Human review** before any AI-generated output is used, merged, or deployed.
- **AI transparency** — PR descriptions must disclose AI assistance and name the reviewer.
- **Feature branch** — all changes on a named branch; reviewed via PR before merging to `main`, per the [Defra SDS Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).
- **No hardcoded secrets** — `client_secret` and credentials must never be committed; use Key Vault or environment variable references.
- **SonarQube** — all AI-generated code must pass static analysis before merge.

### [Defra SDS Alignment](https://defra.github.io/software-development-standards/guides/github_copilot/)

Follows the [Defra SDS GitHub Copilot Guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/), and [Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).

## References

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)
