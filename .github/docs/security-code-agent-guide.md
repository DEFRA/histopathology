# Security Code Analysis Agent — What It Does & Change Log

**File:** `.github/agents/security-code.agent.md`  
**Last reviewed:** 2026-07-31  
**Owner:** LAP Civica Migration Team  
**Standards alignment:** Defra SDS · Defra AI Toolkit — Deliver with AI

---

## What This Agent Does

### Overview

The **Security Code Analysis Agent** (v2.1.0) scans a codebase and generates a security findings report aligned with the **ITHC (IT Health Check)** spreadsheet format used in UK Government engagements. It combines SAST, SCA, Secrets Scanning, IaC Review, and AI-assisted pattern detection to identify the full breadth of vulnerabilities.

> **Risk Classification: HIGH** — this agent performs security scanning and generates ITHC findings reports that are used to make remediation decisions. All outputs require mandatory human review before use.

---

### Skills

| Skill | File | Purpose |
|---|---|---|
| `security-code` | `.github/skills/security-code/SKILL.md` | Full security scan pipeline: SAST, SCA, Secrets, IaC, AI pattern detection |

---

### Scan Layers

| Layer | Type | What it detects |
|---|---|---|
| SAST | Static Application Security Testing | Injection, broken auth, insecure deserialization, XSS, CSRF |
| SCA | Software Composition Analysis | Vulnerable and outdated NuGet/npm dependencies (CVE/GHSA) |
| Secrets Scanning | Credential detection | Hardcoded API keys, connection strings, tokens, passwords |
| IaC Review | Infrastructure-as-Code | Misconfigured Bicep, ARM, Terraform, Docker, `web.config` |
| AI-assisted | Pattern detection | Logic flaws, business logic errors, OWASP Top 10 patterns not caught by SAST |

Standards applied: **OWASP Top 10 (2021)**, **CWE Top 25**, **NCSC Cyber Essentials**.

---

### Outputs Produced

| Output | Description |
|---|---|
| ITHC findings report (HTML) | UK Government ITHC-format report with all findings, severities, and confidence ratings |
| Per-finding entries | Risk rating, CWE/CVE references, affected file/line, recommended fix |
| Agent metadata footer | Version, scan date, AI confidence ratings — included in every report |
| `[MANUAL REVIEW]` flags | All AI-assisted findings are tagged for human review |

---

### Confidence Rating Model

All AI-assisted findings include a confidence rating (High / Medium / Low / Unconfirmed). Recipients of the report must be informed it is AI-assisted.

> **False positive rate (AI-assisted findings):** ~8–12%. All findings should be corroborated with SAST tooling before remediation.

---

### Quality Gates (All Must Pass Before Findings Are Closed)

| Gate | Requirement |
|---|---|
| Human review | **Mandatory** before any findings report is shared, acted upon, or used to gate deployment |
| Second reviewer | Required for security-critical findings resulting in code changes |
| AI transparency | Report must include agent version, scan date, and AI confidence ratings |
| SonarQube | Must complete before findings are closed |
| Secret scanning | Must confirm no live credentials in scanned code |
| ITHC sign-off | Required before production deployment |

---

### Guardrails Summary

**The agent will:**
- Include AI confidence ratings on all AI-assisted findings
- Redact PII — `pii_redaction: true` is always enabled
- Tag all AI-detected findings with `[MANUAL REVIEW]`
- Never send data to external AI services (`send_to_external_ai: false`)
- Maintain an audit log of all scan activities

**The agent will never:**
- Allow reports to be used to gate deployment without human ITHC sign-off
- Disable PII redaction
- Present AI findings as confirmed without SAST corroboration
- Bypass the second reviewer requirement for security-critical remediations

---

### Compliance Profile

| Dimension | Classification / Status |
|---|---|
| AI Toolkit risk level | **HIGH** |
| Human oversight required | Yes — mandatory before any report is shared or acted upon |
| Second reviewer required | Yes — for security-critical findings leading to code changes |
| Data classification | Official |
| PII redaction | Always enabled |
| Send to external AI | Never |
| Audit log | Always enabled |
| Defra SDS standards applied | GitHub Copilot guide, Security standards, Common coding standards, Git branching |

---

## Change Log

### v1.0 — 2026-07-31 — Initial Guide

Initial creation of the Security Code Analysis agent guide.

---

## References

- [`.github/agents/security-code.agent.md`](./../agents/security-code.agent.md) — agent definition (v2.1.0)
- [OWASP Top 10 (2021)](https://owasp.org/Top10/)
- [CWE Top 25](https://cwe.mitre.org/top25/)
- [NCSC Cyber Essentials](https://www.ncsc.gov.uk/cyberessentials/overview)
- [OWASP ASVS v4.0](https://owasp.org/www-project-application-security-verification-standard/)
- [UK GDPR / Data Protection Act 2018](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/)
- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — Security standards](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra SDS — Git branching strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security guidance](https://digital.defra.gov.uk/ai-toolkit/guidance/security)
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)

---

*This document is maintained alongside `.github/agents/security-code.agent.md`. Update the change log and "Last reviewed" date whenever the agent is modified.*
