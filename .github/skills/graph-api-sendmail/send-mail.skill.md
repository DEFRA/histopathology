---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: graph-api-sendmail
description: >-
  Provides a single callable skill to send email via Microsoft Graph API using OAuth 2.0 client credentials.
  Use when an agent needs to send notifications, alerts, or reports via Microsoft 365 from a service account.
license: OGL-UK-3.0
metadata:
  author: defra-digital
  version: "1.0"
---

> **Last reviewed: 2026-07-29** — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes

# Graph API Send Mail Skill

Purpose: provide a single callable skill to send email via Microsoft Graph API.

Signature
---------

- `send_mail(tenant_id, client_id, client_secret, sender, to, subject, body, content_type='HTML', cc=None, bcc=None, attachments=None, save_to_sent=True)`

Parameters
----------

- `tenant_id` (str): Azure AD tenant id
- `client_id` (str): App (client) id
- `client_secret` (str): App client secret (sensitive)
- `sender` (str): userPrincipalName or id to send mail as (e.g. `no-reply@example.com`)
- `to` (List[str]): recipient email addresses
- `subject` (str): subject line
- `body` (str): message body
- `content_type` (str): `HTML` or `Text` (default `HTML`)
- `cc` (List[str] | None): CC recipients
- `bcc` (List[str] | None): BCC recipients
- `attachments` (List[dict] | None): attachments where each dict contains keys: `name` (str), `bytes_base64` (str base64-encoded)
- `save_to_sent` (bool): save the message to Sent Items (default True)

Return
------

Dictionary with keys:

- `success` (bool)
- `status_code` (int) — HTTP return code from Graph
- `response` (dict|string) — parsed JSON response if available
- `error` (str|null) — human-friendly error message

Error handling
--------------

The implementation raises no uncaught exceptions. All expected failure modes are returned in the `error` field and `success` is set to `False`. Errors covered include:

- Authentication errors from MSAL
- Network errors (requests exceptions)
- Graph API 4xx/5xx responses (returned body is included when available)
- Attachment encoding/size validation errors

CLI
---

The skill ships with a small CLI wrapper implemented in `graph_api_send_mail.py` for manual testing. Use the requirements file to install dependencies.

---

## Standards

This skill is loaded by agents that send email via Microsoft Graph API. All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Credentials (`client_secret`, `tenant_id`, `client_id`) must never be committed — use placeholders only. Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/), and [Defra SDS — Credential Exposure Process](https://defra.github.io/software-development-standards/processes/credential_exposure/).
