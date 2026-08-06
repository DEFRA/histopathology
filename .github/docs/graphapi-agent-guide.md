# GraphAPI Send Mail Agent — What It Does & Change Log

**File:** `.github/agents/graphapi.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **GraphAPI Send Mail Agent** sends email via Microsoft Graph API using OAuth 2.0 client credentials flow. It is specific to the four Civica applications: **BSE, Histo, D2R2, and PTLIMS**. Designed for automation scenarios where an agent needs to deliver notifications, reports, or alerts from a service account.

---

### Skills

| Skill | File | Purpose |
|---|---|---|
| `graph-api-sendmail` | `.github/skills/graph-api-sendmail/send-mail.skill.md` | Wraps MSAL token acquisition and the Graph `sendMail` endpoint |

---

### Required App Permissions (Entra ID)

| Permission | Type | Use case |
|---|---|---|
| `Mail.Send` | Application | Send as any user via application credentials — requires admin consent |
| `Mail.Send` | Delegated | Send using delegated user tokens |

> Application permissions to send as arbitrary users require admin consent and careful security review before use.

---

### Inputs

| Parameter | Type | Required | Description |
|---|---|---|---|
| `tenant_id` | string | ✅ | Azure AD tenant ID |
| `client_id` | string | ✅ | App (client) ID |
| `client_secret` | string | ✅ | App client secret — never commit; use Key Vault |
| `sender` | string | ✅ | `userPrincipalName` or object ID of sending account |
| `to` | list[string] | ✅ | Recipient email addresses |
| `subject` | string | ✅ | Message subject |
| `body` | string | ✅ | Message body (HTML or plain text) |
| `content_type` | string | No | `HTML` or `Text` (default: `HTML`) |
| `cc` | list[string] | No | CC recipients |
| `bcc` | list[string] | No | BCC recipients |
| `attachments` | list[object] | No | Each object: `name` (string), `bytes_base64` (base64-encoded) |
| `save_to_sent` | bool | No | Save to Sent Items (default: `true`) |

---

### Outputs

| Field | Type | Description |
|---|---|---|
| `success` | bool | Whether the send succeeded |
| `status_code` | int | HTTP status code from Graph API |
| `response` | object\|string | Parsed JSON response or error text |
| `error` | string\|null | Human-friendly error message; null on success |

---

### Error Handling

All expected failure modes are returned in the `error` field — no uncaught exceptions are raised. Failures covered include:

- MSAL authentication errors
- Network errors (request exceptions)
- Graph API 4xx/5xx responses (response body included where available)
- Attachment encoding and size validation errors

---

### Security Rules

- `client_secret` must **never** be committed to source control — reference Key Vault in all environments
- `client_id` and `tenant_id` for UAT and production must be in `.copilotignore`-protected config files
- Application `Mail.Send` permission requires explicit admin consent — document the approval in the Entra registration checklist

---

### Guardrails Summary

**The agent will:**
- Use MSAL for token acquisition — no manual token construction
- Return structured error responses for all failure modes
- Validate attachment encoding before sending

**The agent will never:**
- Commit credentials to any file
- Send to recipients not explicitly provided in inputs
- Log `client_secret` or raw token values

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | MEDIUM |
| Human oversight required | Yes — review `Mail.Send` permission scope before deployment |
| Data protection | Email body may contain personal data — handle per UK GDPR |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Git branching, Credential exposure process |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the GraphAPI Send Mail agent guide.

---

## References

- [`.github/agents/graphapi.agent.md`](./../agents/graphapi.agent.md) — agent definition
- [Microsoft Graph sendMail API](https://learn.microsoft.com/en-us/graph/api/user-sendmail)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra SDS — Credential exposure process](https://defra.github.io/software-development-standards/processes/credential_exposure/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Keeping data safe](https://digital.defra.gov.uk/ai-toolkit/guidance/keeping-data-safe)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/graphapi.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
