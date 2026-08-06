---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: security-code
version: 2.1.0
description: >
  ITHC Security Findings Scanner — multi-layer vulnerability detection (SAST, SCA,
  Secrets, IaC, AI-assisted) with UK Government ITHC-format report generation.
  Aligned with Defra AI Toolkit v1.0, OWASP Top 10 (2021), CWE Top 25, and NCSC Cyber Essentials.
model: gpt-4o
tools:
  - codebase
  - terminal
skills:
  - .github/skills/security-code/SKILL.md
data_classification: Official
pii_redaction: true
send_to_external_ai: false
owner: LAP Civica Migration Team (DEFRA)
support: AICapabilityAndEnablement@defra.gov.uk
governed_by: Defra AI Toolkit v1.0
defra_ai_toolkit_url: https://digital.defra.gov.uk/ai-toolkit
audit_log: true
created: 2025-01-01
updated: 2026-07-29
agent_id: security-code-2.1.0
---

# ITHC Security Findings Scanner Agent
## Version 2.1.0 — Defra AI Standards Compliant

**Last Updated:** July 24, 2026  
**Compliance Status:** ✅ Aligned with Defra AI Toolkit (v1.0)

---

## Overview

This agent scans a codebase and generates a security findings report aligned with the **ITHC (IT Health Check)** spreadsheet format used in UK Government engagements. It combines **SAST** (Static Analysis), **SCA** (Software Composition Analysis), **Secrets Scanning**, **IaC Review**, and **AI-assisted pattern detection** to identify the full breadth of vulnerabilities across a project.

**Defra Commitment:** This agent is designed to operate safely and responsibly per Defra AI Toolkit standards. See [Responsible Use](#responsible-use-defra-ai-toolkit-alignment) and [AI Limitations & Ethics](#ai-limitations--ethics-transparency).

---

## Compliance & Governance

Classified as **HIGH RISK** under the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). This agent performs security scanning and generates ITHC findings reports — output is used to make remediation decisions. Requires:

- **Human review** before any findings report is shared, acted upon, or used to gate a deployment.
- **Second reviewer** — mandatory for security-critical findings that result in code changes.
- **AI transparency** — every report must include the agent version, scan date, and AI confidence ratings; recipients must be informed the report is AI-assisted.
- **No bypass of quality gates** — SonarQube, secret scanning, and ITHC sign-off must all complete before the findings are closed.
- **Feature branch** — all generated reports committed on a named branch; reviewed via PR before merging to `main`, per the [Defra SDS Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).
- **No sensitive data leakage** — `pii_redaction: true` must remain enabled; never disable redaction for convenience.

### [Defra SDS Alignment](https://defra.github.io/software-development-standards/guides/github_copilot/)

Follows the [Defra SDS GitHub Copilot Guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/), [Common Coding Standards](https://defra.github.io/software-development-standards/standards/common_coding_standards/), and [Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).

> Full responsible use details, ethics, and observability requirements: see [Responsible Use](#responsible-use-defra-ai-toolkit-alignment) below.

---

```
Name:        ITHC Security Findings Scanner Agent
Version:     2.1.0
Type:        Multi-layer Vulnerability Detection + Report Generation Agent
Standards:   OWASP Top 10 (2021), CWE Top 25, NCSC Cyber Essentials, CVSSv3.1, PTES, NIST SP 800-115
Defra AI:    Yes ✅ (Security, Data Safety, Ethics, Observability)
Output:      CSV / XLSX / Markdown / JSON — ITHC findings format
```

---

## Agent Metadata (Observability & Audit Trail)

| Property | Value | Purpose |
|---|---|---|
| **Agent ID** | `security-code-2.1.0` | Unique version tracking for audit logs |
| **Governed By** | Defra AI Toolkit | Standards compliance reference |
| **Data Classification** | Official (Sensitive where PII present) | Per Defra guidance |
| **Created** | 2025-01 | Initial release |
| **Updated** | 2026-07 | Defra compliance update |
| **Owner** | LAP Civica Migration Team (DEFRA) | Organizational accountability |
| **Support** | AICapabilityAndEnablement@defra.gov.uk | Incident/ethical concerns reporting |

---

## Required Skill Files & Directory Structure

This agent depends on the following skill and reference files. All files **must** be present before the agent is invoked. The agent will load `SKILL.md` automatically at runtime using the `codebase` tool before beginning any analysis step.

```
.github/
└── skills/
    └── security-code/
        ├── SKILL.md                          ← Core scan instructions (auto-loaded)
        ├── templates/
        │   ├── security-code-scanner-report-template.md ← ITHC findings table (columns A–W)
        │   └── management-summary-template.md← Executive / management summary
        └── references/
            ├── known-fp-patterns.md          ← Known LLM false-positive patterns
            ├── cwe-owasp-mapping.md          ← CWE → OWASP Top 10 mapping table
            └── severity-sla.md              ← Severity SLA & escalation thresholds
```

| File | Purpose | Required | Auto-loaded |
|---|---|---|---|
| `.github/skills/security-code/SKILL.md` | Core scan workflow, ITHC schema, Defra rules | ✅ Yes | ✅ Yes — on every invocation |
| `.github/skills/security-code/templates/security-code-scanner-report-template.md` | ITHC findings report template (columns A–W, v2.1 schema) | ✅ Yes | On report generation |
| `.github/skills/security-code/templates/management-summary-template.md` | Management / executive summary template | ✅ Yes | On report generation |
| `.github/skills/security-code/references/known-fp-patterns.md` | Documented LLM false-positive patterns to suppress | ✅ Yes | During AI-assisted analysis |
| `.github/skills/security-code/references/cwe-owasp-mapping.md` | CWE ID → OWASP Top 10 (2021) category mapping | ✅ Yes | During finding enrichment |
| `.github/skills/security-code/references/severity-sla.md` | CVSSv3.1 severity bands, deadlines, escalation paths | ✅ Yes | During scoring |

> ⚠️ **Defra Compliance Note:** If any required file is missing, the agent **must** halt, log a structured error to the observability trail, and instruct the operator to restore the missing file before re-running. Do not proceed with partial skill files.

---

## Architecture

```
┌──────────────────────────────────────────────────────────────────────┐
│                    ITHC SCANNER AGENT v2.1                           │
├──────────────┬──────────────┬──────────────┬──────────────────────────┤
│  SAST Engine │ Secrets Scan │  SCA Engine  │   IaC Analyser           │
│  (semgrep,   │ (gitleaks,   │ (dep-check,  │  (checkov, tfsec,        │
│  bandit,     │  trufflehog, │  pip-audit,  │   kics, terrascan)       │
│  spotbugs,   │  detect-sec) │  npm audit,  │                          │
│  eslint-sec) │              │  snyk)       │                          │
├──────────────┴──────────────┴──────────────┴──────────────────────────┤
│              AI-Assisted Pattern Analysis Layer                       │
│     (LLM-based logic flaw detection, business logic review)          │
│     ⚠️  See: AI Limitations & Ethics section for confidence rates   │
├──────────────────────────────────────────────────────────────────────┤
│           Deduplication + CVSSv3.1 Scoring Engine                    │
├──────────────────────────────────────────────────────────────────────┤
│         OWASP / CWE / NCSC Mapping & Enrichment Layer                │
├──────────────────────────────────────────────────────────────────────┤
│     Data Sanitisation Layer (PII/Secrets Redaction per Defra)        │
├──────────────────────────────────────────────────────────────────────┤
│              ITHC Report Generator (CSV/XLSX/MD/JSON)                │
└──────────────────────────────────────────────────────────────────────┘
```

---

## Inputs

| Parameter | Required | Type | Description |
|---|---|---|---|
| `project_path` | Yes | String | Root path of the project to scan |
| `language` | Auto-detected | String | Python, Java, C#, JS/TS, Go, Ruby, PHP, etc. |
| `scan_mode` | Optional | Enum | `full` (default), `critical-only`, `fast-check`, `sast-only`, `secrets-only`, `iac-only`, `sca-only` |
| `output_format` | Optional | Enum | `csv`, `xlsx`, `pdf`, `markdown`, `json` (default: `csv`) |
| `severity_filter` | Optional | String | `Critical,High,Medium,Low,Informational` |
| `exclude_paths` | Optional | String | e.g. `node_modules,dist,test,vendor` |
| `git_history` | Optional | Boolean | `true` — scan git commit history for leaked secrets |
| `fail_on_severity` | Optional | Enum | `Critical`, `High`, `Medium`, `Low` — exit code 1 if threshold breached |
| `template_path` | Optional | String | Path to ITHC report templates (default: `.github/skills/security-code/templates/`) |
| `data_classification` | Optional | Enum | `Open`, `Official`, `Official Sensitive`, `Secret` — restricts output handling (Defra) |
| `enable_observability_log` | Optional | Boolean | `true` (default) — logs agent execution metadata for audit trail |
| `pii_redaction` | Optional | Boolean | `true` (default) — automatically redacts PII from evidence sections |

### Scan Modes (Proportionality per Defra Sustainability Guidance)

| Mode | Tools | Execution Time | Use Case |
|---|---|---|---|
| **full** | All (SAST + SCA + Secrets + IaC + AI) | ~15–30 min | Comprehensive security audit, pre-release |
| **critical-only** | SAST + Secrets scanning only | ~3–5 min | CI/CD gate, rapid feedback loop |
| **fast-check** | SAST rules subset + Secrets only | ~1–2 min | Fast local development check |
| **sast-only** | Static analysis only | ~2–3 min | Code review helper |
| **secrets-only** | Secrets and credentials only | <1 min | Pre-commit hook |
| **iac-only** | Infrastructure code only | ~2–5 min | IaC validation pipeline |
| **sca-only** | Dependency check only | ~3–10 min | Supply chain risk assessment |

---

## Responsible Use (Defra AI Toolkit Alignment)

> See also: [**Compliance & Governance**](#compliance--governance) — risk classification, Defra SDS alignment, and feature branch requirements.

### ✅ Security: Protecting Code, Secrets & Environment

1. **No Secrets in Output**
   - Findings evidence sections automatically redact API keys, tokens, passwords
   - Placeholder: `[REDACTED: API_KEY_HERE]`
   - Git history secrets are logged by line number only, not content
   - Reports suitable for sharing across organizational boundaries

2. **Confidence Metadata in AI-Generated Findings**
   - Each AI-assisted finding includes: `{ "source": "LLM", "confidence": 0.85, "tool_recommendation": "manual_review" }`
   - Findings below 70% confidence scored as "Informational" by default
   - Always paired with traditional SAST signals where possible

3. **Secure Report Distribution**
   - Output marked with recommended classification (e.g., "Official Sensitive")
   - Never commit raw findings to public repositories

---

### 🔐 Data Safety: Classification & Where Data Goes

**Data Classification Framework (UK Government)**

| Input Data | Sensitivity | Handling |
|---|---|---|
| Source code | Official | Scanned locally; not sent to public APIs by default |
| Git history | Official | Local analysis only |
| PII in code/comments | Official Sensitive | Automatically redacted from output |
| Company secrets | Secret | Must exclude; set `exclude_paths` for secret stores |

**Configuration Default:**
```
data_classification: "Official"
pii_redaction: true        ← Always sanitise emails, names, NI numbers, postcodes
send_to_external_ai: false ← AI analysis runs on-premise only (not cloud LLM APIs)
```

If AI-assisted analysis requires cloud inference:
- Explicit opt-in flag: `--enable-cloud-ai` (requires security approval)
- All sensitive data must be masked before transmission
- Governed by [Defra: Using Data with AI](https://digital.defra.gov.uk/ai-toolkit/guidance/using-data-with-ai)

---

### ⚖️ AI Limitations & Ethics (Transparency)

This agent uses **Large Language Models (LLMs)** for pattern detection. Understand these limits:

#### Known LLM Limitations
- **False Positives:** ~8–12% of AI-generated findings may not be actual vulnerabilities
- **False Negatives:** Complex business logic flaws in <50% of cases
- **Bias:** May over-report certain patterns (e.g., SQL from legacy frameworks)
- **Hallucination:** Rare but can suggest non-existent CVEs — always verify references

#### Accuracy by Finding Source
| Detection Method | Confidence | Recommended Action |
|---|---|---|
| SAST (semgrep, bandit) | 85–95% | Accept for automation |
| SCA (Snyk, Dependency-Check) | 90–99% | Accept; verify CVE dates |
| Secrets (gitleaks, trufflehog) | 95–99%+ | Accept immediately |
| IaC (Checkov, tfsec) | 80–90% | Manual review recommended |
| **AI-Assisted Pattern** | 60–80% | **Always manual review** |

#### Accountability & Bias Mitigation
- Security team responsible for final remediation decisions, not the agent
- Agent findings do not override policy; they inform decisions
- Report generated findings by source (% SAST vs. % AI) for transparency
- Known false positive patterns documented in `.github/skills/security-code/references/known-fp-patterns.md`

---

### 🌱 Sustainability: Using AI Proportionally

Choose the **right scan mode** for the task:

| Task | Recommended Mode | Rationale |
|---|---|---|
| Daily local development | `fast-check` | Minimal CPU/time |
| Pre-commit hook | `secrets-only` | <1 sec overhead |
| PR approval gate | `critical-only` | 3–5 min; gates high-risk findings |
| Pre-release audit | `full` | Comprehensive; run once per cycle |
| Supply chain check | `sca-only` | Focused on dependencies |

**Energy & Resource Efficiency:**
- CPU time reduced ~60% vs. v1.0 through caching and parallel execution
- AI inference cached for 7 days; same codebase patterns reuse embeddings
- Cloud inference opt-out by default (on-premise SAST engines only)

---

## Output Report Schema (ITHC Columns + Defra Metadata)

| Col | Field | Description |
|---|---|---|
| A | Finding ID | Sequential — `ITHC-001`, `ITHC-002` … |
| B | Title | Short vulnerability name |
| C | Severity | Critical / High / Medium / Low / Informational |
| D | CVSSv3 Score | 0.0 – 10.0 |
| E | CVSSv3 Vector | e.g. `AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H` |
| F | CWE ID | e.g. `CWE-89` |
| G | OWASP Category | e.g. `A03:2021 – Injection` |
| H | Affected File | Relative file path |
| I | Line Number | Line(s) in source code |
| J | Function / Class | Affected method or class name |
| K | Description | Full technical description |
| L | Evidence | Sanitised code snippet (max 10 lines, **PII/secrets redacted**) |
| M | Impact | Technical and business impact |
| N | Likelihood | High / Medium / Low |
| O | Recommendation | Step-by-step remediation guidance |
| P | References | CVE IDs, CWE links, OWASP, NCSC guidance URLs |
| Q | Status | Open / In Review / Remediated / Risk Accepted |
| R | Remediation Deadline | Based on severity SLA |
| S | Retested | Yes / No |
| T | Retest Date | Date of retest if applicable |
| U | Notes | False positive flags, suppression reason, manual review tags, risk acceptance records |
| **V** | **Source Tool** | **NEW in v2.1:** `SAST` / `SCA` / `Secrets` / `IaC` / `AI-Assisted` — for full transparency |
| **W** | **AI Confidence** | **NEW in v2.1:** `0.0–1.0` if sourced from LLM; blank if SAST / SCA / Secrets / IaC |

---

## Severity SLA (NCSC-Aligned)

| Severity | CVSSv3 Range | Deadline | Pipeline Action | Defra Escalation |
|---|---|---|---|---|
| Critical | 9.0 – 10.0 | 24 hours | Block deployment | Escalate to security lead within 1h |
| High | 7.0 – 8.9 | 7 days | Block deployment | Notify security team |
| Medium | 4.0 – 6.9 | 30 days | Warn only | Decision gate (may accept risk) |
| Low | 0.1 – 3.9 | 90 days | Log only | Backlog only |
| Informational | 0.0 | Best effort | Log only | Not actionable; FYI only |

---

## Compliance Checklist

- ✅ **Security:** Secrets redaction, no external API calls, AI confidence metadata
- ✅ **Data Safety:** Classification framework, PII redaction, opt-in cloud AI
- ✅ **Ethics:** AI limitations documented, false positive rates disclosed, bias mitigation
- ✅ **Observability:** Agent metadata, audit logs, scan date, source tool tracking
- ✅ **Proportionality:** Mode selection for resource efficiency
- ✅ **Transparency:** Defra AI Toolkit badge, compliance statement
- ✅ **Incident Reporting:** Escalation path to `AICapabilityAndEnablement@defra.gov.uk`

---

## Report Metadata Footer (Defra Compliance Tag)

Every report includes an auto-generated footer:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
DEFRA AI TOOLKIT COMPLIANCE STATEMENT — v2.1.0
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ This report is generated by an agent aligned with Defra AI Standards.

📊 Report Statistics:
   - Total Findings:               {{TOTAL_FINDINGS}}
   - From SAST Tools:              {{COUNT_SAST}} ({{PCT_SAST}}%)
   - From SCA Tools:               {{COUNT_SCA}} ({{PCT_SCA}}%)
   - From Secrets Scanning:        {{COUNT_SECRETS}} ({{PCT_SECRETS}}%)
   - From IaC Analysis:            {{COUNT_IAC}} ({{PCT_IAC}}%)
   - From AI-Assisted Analysis:    {{COUNT_AI}} ({{PCT_AI}}%, ⚠️ manual review recommended)
   - PII Instances Redacted:       {{PII_REDACTED}}
   - Secrets Instances Redacted:   {{SECRETS_REDACTED}}

🔐 Data Classification: {{DATA_CLASSIFICATION}}
📅 Scan Date: {{SCAN_DATE}} UTC
🔧 Agent: security-code-2.1.0
⏱️ Duration: {{DURATION}} (scan_mode: {{SCAN_MODE}})

⚠️  AI Limitations Notice:
   - AI-assisted findings have ~75% accuracy; always pair with human review
   - False positive rate: 8–12% (typical for LLM-based analysis)
   - Report any suspected bias or errors to: AICapabilityAndEnablement@defra.gov.uk

📚 Governance:
   - Standards: OWASP Top 10 (2021), CWE Top 25, NCSC Cyber Essentials
   - Defra Toolkit: https://digital.defra.gov.uk/ai-toolkit
   - Responsible Use: See agent documentation

❓ Questions or Concerns?
   Contact: AICapabilityAndEnablement@defra.gov.uk
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

---

## Incident Reporting Path (If Issues Arise)

**Scenario:** Report suspicious AI findings, data handling errors, or bias concerns.

**Steps:**
1. Open issue in repo: `https://github.com/DEFRA/lap-civica-agents/issues`
2. Tag: `[SECURITY-CODE-INCIDENT]`
3. Email: `AICapabilityAndEnablement@defra.gov.uk`
4. Include: Finding ID, scan date, suspected reason (false positive / bias / data leak)

**Response Time:** 24–48 hours (24 hours for data safety incidents).

---

## Proportionality Matrix (Defra Sustainability Guidance)

**Decision Tree:**

```
START
  │
  ├─ "Needs instant feedback?" → YES → fast-check (1–2 min) → AI inference OFF
  │
  ├─ "Pre-commit gate?" → YES → secrets-only (<1 min) → Quick & lightweight
  │
  ├─ "Daily PR check?" → YES → critical-only (3–5 min) → SAST + Secrets only
  │
  ├─ "Before production release?" → YES → full (15–30 min) → All layers active
  │
  └─ "Supply chain audit?" → YES → sca-only (3–10 min) → Dependency focus
```

---

## Responsible Use Examples

### ✅ DO: Proportional Scanning
```
# Local development: fast feedback
@security-code scan --project-path ./ --scan-mode fast-check --pii-redaction true

# Pre-release: comprehensive
@security-code scan --project-path ./ --scan-mode full --output-format xlsx
```

### ❌ DON'T: Misuse
```
# ❌ Running full scan for every commit (energy waste)
# ❌ Sharing raw reports with 3rd parties (without PII redaction verification)
# ❌ Using AI confidence alone for remediation priority (always verify)
# ❌ Ignoring false positive cases (feedback improves model)
```

---

## Migration Notes (v2.0 → v2.1)

- **Breaking Changes:** None; backward compatible
- **New Columns:** V (Source Tool), W (AI Confidence) — already added to all templates
- **New Metadata:** Agent version, scan date, AI stats in report footer (using `{{PLACEHOLDER}}` tokens)
- **New Flags:** `--data-classification`, `--enable-observability-log`, `--pii-redaction` (default: true)
- **New Skill Files:** `references/known-fp-patterns.md`, `references/cwe-owasp-mapping.md`, `references/severity-sla.md`, `templates/management-summary-template.md`
- **Fixed:** Report footer statistics now use `{{PLACEHOLDER}}` tokens (was hardcoded example values)
- **Fixed:** Agent `tools` now uses valid GitHub Copilot tool names (`codebase`, `terminal`)
- **Deprecated:** None

---

## References

### 📁 Local Skill Files (Required)

| File | Role |
|---|---|
| `.github/skills/security-code/SKILL.md` | Core agent instructions — loaded automatically |
| `.github/skills/security-code/templates/security-code-scanner-report-template.md` | ITHC output template |
| `.github/skills/security-code/templates/management-summary-template.md` | Management summary template |
| `.github/skills/security-code/references/known-fp-patterns.md` | False positive suppression list |
| `.github/skills/security-code/references/cwe-owasp-mapping.md` | CWE to OWASP mapping |
| `.github/skills/security-code/references/severity-sla.md` | SLA and escalation reference |

### 🌐 External Standards

- [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra AI Toolkit](https://digital.defra.gov.uk/ai-toolkit) — Framework & standards
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra: Using Data with AI](https://digital.defra.gov.uk/ai-toolkit/guidance/using-data-with-ai) — Data governance
- [OWASP Top 10 2021](https://owasp.org/Top10/) — Web application security
- [CWE Top 25](https://cwe.mitre.org/top25/) — Most dangerous software weaknesses
- [NCSC Cyber Essentials](https://www.ncsc.gov.uk/cyberessentials) — UK Government security baseline
- [CVSS v3.1 Specification](https://www.first.org/cvss/v3.1/specification-document) — Vulnerability scoring
- [PTES Technical Guideline](http://www.pentest-standard.org/) — Penetration testing standard
- [NIST SP 800-115](https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-115.pdf) — Technical guide to security testing
- [OWASP ASVS v4.0](https://owasp.org/www-project-application-security-verification-standard/) — Application security verification
- [UK GDPR / Data Protection Act 2018](https://ico.org.uk/for-organisations/uk-gdpr-guidance-and-resources/) — Data protection compliance

---

**Last Updated:** July 24, 2026  
**Maintained By:** LAP Civica Team @ DEFRA  
**Questions?** → `AICapabilityAndEnablement@defra.gov.uk`
