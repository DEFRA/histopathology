# Security Code Skill — ITHC Scanner v2.1.0

> **Auto-loaded by:** `.github/agents/security-code.agent.md`
> **Defra AI Toolkit:** ✅ Compliant
> **Data Classification:** Official
> **Last Updated:** 2026-07-29

---

## 1. Purpose

This skill provides the detailed execution instructions for the **ITHC Security Findings
Scanner Agent**. It defines:

- How to discover and read files in the target codebase
- How to apply each analysis layer (SAST, Secrets, SCA, IaC, AI-assisted)
- How to score, deduplicate, and enrich findings
- How to generate ITHC-format reports with Defra compliance metadata
- How to handle PII, secrets, and data classification requirements

> **On every invocation**, the agent must call `get_file` to load this SKILL.md before
> beginning any analysis step.

---

## 2. Pre-flight Checklist

Before beginning any scan, verify:

```
[ ] project_path is accessible and readable
[ ] All required skill files are present (see agent Required Skill Files section)
[ ] pii_redaction = true  (default — must NOT be disabled without explicit approval)
[ ] send_to_external_ai = false  (cloud AI requires --enable-cloud-ai + security approval)
[ ] data_classification is set (default: Official)
[ ] Observability log is enabled (default: true)
[ ] Output destination matches the data classification level
```

If any required skill file is missing: **halt immediately**, log a structured error to
the observability trail, and instruct the operator to restore the file before re-running.

---

## 3. Scan Workflow

### Step 1 — Codebase Discovery

1. Recursively list all files under `project_path`.
2. Identify the primary language(s) by file extension frequency.
3. Detect project type: Web, API, CLI, Library, IaC, or Mixed.
4. Locate manifest files: `packages.config`, `*.csproj`, `*.vbproj`, `package.json`,
   `requirements.txt`, `pom.xml`, `build.gradle`, `go.mod`, `Gemfile`.
5. Locate configuration files: `web.config`, `appsettings*.json`, `*.env`, `.env*`,
   `app.config`, `*.yaml`, `*.yml`, `*.tf`, `*.bicep`.
6. Locate IaC files: `*.tf`, `*.bicep`, ARM `*.json`, `Dockerfile`,
   `docker-compose*.yml`, `*.k8s.yaml`.
7. Record the full file inventory in the observability log.

---

### Step 2 — SAST Analysis

Reference `references/cwe-owasp-mapping.md` for OWASP category assignments.

#### 2a. Injection

| Pattern | Languages | CWE | OWASP |
|---|---|---|---|
| String-concatenated SQL queries | All | CWE-89 | A03:2021 |
| Dynamic OS command execution | All | CWE-78 | A03:2021 |
| Unsanitised LDAP queries | All | CWE-90 | A03:2021 |
| Unsanitised XPath queries | All | CWE-643 | A03:2021 |
| `eval()` / `exec()` with user input | JS, Python, Ruby | CWE-95 | A03:2021 |
| XML external entity (XXE) parsers | All | CWE-611 | A05:2021 |
| Server-side template injection | All | CWE-94 | A03:2021 |
| NoSQL injection (`$where`, `$regex`) | JS, Python | CWE-943 | A03:2021 |

#### 2b. Cross-Site Scripting (XSS)

| Pattern | CWE | OWASP |
|---|---|---|
| Unencoded user input in HTML output | CWE-79 | A03:2021 |
| `innerHTML` / `document.write()` with user data | CWE-79 | A03:2021 |
| `Response.Write()` without encoding (.NET) | CWE-79 | A03:2021 |
| `@Html.Raw()` with user input (Razor) | CWE-79 | A03:2021 |

#### 2c. Authentication & Session

| Pattern | CWE | OWASP |
|---|---|---|
| Hardcoded credentials or default passwords | CWE-798 | A07:2021 |
| Missing `[Authorize]` on sensitive endpoints | CWE-862 | A01:2021 |
| Session tokens in URL parameters | CWE-598 | A07:2021 |
| Insufficiently random session token generation | CWE-330 | A07:2021 |
| Missing HTTPS enforcement / `requireSSL=false` | CWE-319 | A02:2021 |
| Never-expiring or excessively long session timeouts | CWE-613 | A07:2021 |
| Missing MFA on privileged paths | CWE-308 | A07:2021 |

#### 2d. CSRF

| Pattern | CWE | OWASP |
|---|---|---|
| State-changing POST without anti-CSRF token | CWE-352 | A01:2021 |
| `ValidateAntiForgeryToken` absent on forms (.NET) | CWE-352 | A01:2021 |
| `SameSite=None` cookie without `Secure` flag | CWE-352 | A01:2021 |

#### 2e. Insecure Deserialization

| Pattern | CWE | OWASP |
|---|---|---|
| `BinaryFormatter` / `NetDataContractSerializer` (.NET) | CWE-502 | A08:2021 |
| `ObjectInputStream` without type filter (Java) | CWE-502 | A08:2021 |
| `pickle.loads()` with untrusted input (Python) | CWE-502 | A08:2021 |

#### 2f. Path Traversal

| Pattern | CWE | OWASP |
|---|---|---|
| User-controlled file paths without canonicalization | CWE-22 | A01:2021 |
| `..` sequences not stripped from input | CWE-22 | A01:2021 |
| `Server.MapPath()` with unsanitised input (.NET) | CWE-22 | A01:2021 |

#### 2g. SSRF

| Pattern | CWE | OWASP |
|---|---|---|
| User-controlled URL in outbound HTTP request | CWE-918 | A10:2021 |
| No SSRF allowlist / blocklist on HTTP client | CWE-918 | A10:2021 |

#### 2h. Cryptographic Weaknesses

| Pattern | CWE | OWASP |
|---|---|---|
| MD5 / SHA-1 for password hashing | CWE-327 | A02:2021 |
| DES / 3DES / RC4 algorithms | CWE-327 | A02:2021 |
| AES-ECB mode | CWE-327 | A02:2021 |
| Hardcoded IV or static salt | CWE-329 | A02:2021 |
| `System.Random` used for security tokens | CWE-338 | A02:2021 |
| TLS version < 1.2 configured | CWE-326 | A02:2021 |

#### 2i. Sensitive Data Exposure

| Pattern | CWE | OWASP |
|---|---|---|
| PII logged or returned in API responses | CWE-200 | A02:2021 |
| Stack traces returned to clients | CWE-209 | A05:2021 |
| `customErrors mode="Off"` in web.config | CWE-209 | A05:2021 |
| Sensitive fields not masked in logs | CWE-532 | A09:2021 |

#### 2j. Security Logging & Auditing Gaps

| Pattern | CWE | OWASP |
|---|---|---|
| No logging on authentication failures | CWE-778 | A09:2021 |
| No logging on authorization failures | CWE-778 | A09:2021 |
| No audit trail for sensitive data access | CWE-778 | A09:2021 |
| Log injection (user input written directly to log) | CWE-117 | A09:2021 |

---

### Step 3 — Secrets Scanning

Scan every file. **Never output secret values** — replace with `[REDACTED: <TYPE>]`.

| Secret Type | Pattern (indicative) | CWE |
|---|---|---|
| Generic API keys | `(?i)(api[_-]?key\|apikey)\s*[:=]\s*['"]?[A-Za-z0-9+/]{20,}` | CWE-798 |
| AWS credentials | `AKIA[0-9A-Z]{16}` | CWE-798 |
| Azure connection strings | `DefaultEndpointsProtocol=https;AccountName=` | CWE-798 |
| Azure SAS tokens | `sv=20[0-9]{2}` | CWE-798 |
| Azure AD client secrets | `(?i)clientsecret\s*[:=]\s*['"]?[A-Za-z0-9._~-]{34,}` | CWE-798 |
| Passwords in config | `(?i)(password\|passwd\|pwd)\s*[:=]\s*['"]?[^\s'"]{6,}` | CWE-798 |
| DB connection strings | `(?i)data source.*password\s*=\s*[^;"']+` | CWE-798 |
| PEM private keys | `-----BEGIN (RSA \|EC \|OPENSSH )?PRIVATE KEY-----` | CWE-321 |
| JWT tokens | `eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}` | CWE-798 |
| GitHub PATs | `ghp_[A-Za-z0-9]{36}` | CWE-798 |

If `git_history=true` is passed: scan all commits. Any secret found in git history
(even if rotated at HEAD) = **Critical** — must be treated as leaked.

---

### Step 4 — SCA (Dependency Analysis)

| Ecosystem | Manifest | Key Vulnerability Databases |
|---|---|---|
| NuGet (.NET Framework) | `packages.config` | NVD, OSV.dev |
| NuGet (SDK-style) | `*.csproj`, `*.vbproj` | NVD, OSV.dev |
| npm | `package.json`, `yarn.lock` | npm Advisory DB |
| Python | `requirements.txt`, `Pipfile` | PyPI Advisory DB |
| Java | `pom.xml`, `build.gradle` | NVD |
| Ruby | `Gemfile.lock` | RubyGems Advisory DB |
| Go | `go.mod` | Go Vulnerability DB |

Flag: (a) Critical/High CVEs, (b) end-of-life packages, (c) unpinned versions
(`*` or `latest`), (d) packages with no published security policy.

---

### Step 5 — IaC Review

| Check | CWE | Standard |
|---|---|---|
| Public storage blobs / containers | CWE-284 | A01:2021 |
| Missing encryption at rest | CWE-311 | A02:2021 |
| HTTP allowed (no TLS enforcement) | CWE-319 | A02:2021 |
| Overly permissive IAM / RBAC roles | CWE-269 | A01:2021 |
| NSG / firewall open to `0.0.0.0/0` | CWE-284 | A01:2021 |
| Hardcoded secrets in IaC parameters | CWE-798 | A02:2021 |
| Missing audit logging on Azure resources | CWE-778 | A09:2021 |
| TLS version < 1.2 configured | CWE-326 | A02:2021 |
| Debug / diagnostic settings left enabled | CWE-215 | A05:2021 |

---

### Step 6 — AI-Assisted Pattern Analysis

> ⚠️ All findings from this step **must** carry an AI confidence score (column W) and
> the tag `[MANUAL REVIEW REQUIRED]`. Do not assign severity above **Medium** from AI
> analysis alone without a corroborating SAST signal.

1. Review business logic for privilege escalation paths.
2. Identify missing authorization checks in multi-step workflows.
3. Identify data flows where user input reaches sensitive sinks not covered by SAST rules.
4. Cross-reference `references/known-fp-patterns.md` — suppress any confirmed FP patterns.
5. Assign confidence score `0.0–1.0` to each finding.
6. Auto-downgrade findings with confidence < 0.70 to **Informational**.

---

### Step 7 — Deduplication

Group by `(file_path, line_number, cwe_id)`. Keep the finding with the highest severity.
Record suppression reason in column U.

---

### Step 8 — Scoring & Enrichment

For every unique finding:

1. Assign CVSSv3.1 Base Score.
2. Map CWE → OWASP category using `references/cwe-owasp-mapping.md`.
3. Apply severity band and remediation deadline using `references/severity-sla.md`.
4. Set **column V** (`Source Tool`): `SAST` / `SCA` / `Secrets` / `IaC` / `AI-Assisted`.
5. Set **column W** (`AI Confidence`): populate if source = `AI-Assisted`; leave blank otherwise.

---

### Step 9 — PII & Secrets Redaction

Before writing any output, replace in all evidence fields:

| PII Type | Replacement Token |
|---|---|
| Email addresses | `[REDACTED: EMAIL]` |
| UK National Insurance numbers | `[REDACTED: NI_NUMBER]` |
| UK postcodes | `[REDACTED: POSTCODE]` |
| Dates of birth | `[REDACTED: DOB]` |
| Phone numbers | `[REDACTED: PHONE]` |
| Any secret / credential value | `[REDACTED: <SECRET_TYPE>]` |

Log the total redaction count in the report footer.

---

### Step 10 — Report Generation

1. Load `templates/security-code-scanner-report-template.md`
2. Load `templates/management-summary-template.md`
3. Populate all findings (columns A–W per the ITHC v2.1 schema)
4. Generate the executive summary
5. Generate the management summary
6. Append the Defra compliance footer
7. Emit in the requested format: `csv` / `xlsx` / `markdown` / `json`

---

## 4. ITHC Finding Schema — v2.1 (Columns A–W)

| Col | Field | Type | Notes |
|---|---|---|---|
| A | Finding ID | String | `ITHC-001` sequential |
| B | Title | String | Short, clear vulnerability name |
| C | Severity | Enum | `Critical` / `High` / `Medium` / `Low` / `Informational` |
| D | CVSSv3 Score | Float | `0.0 – 10.0` |
| E | CVSSv3 Vector | String | e.g. `AV:N/AC:L/PR:N/UI:N/S:U/C:H/I:H/A:H` |
| F | CWE ID | String | e.g. `CWE-89` |
| G | OWASP Category | String | e.g. `A03:2021 – Injection` |
| H | Affected File | String | Relative path from project root |
| I | Line Number | String | e.g. `42` or `42–47` |
| J | Function / Class | String | Affected method or class name |
| K | Description | String | Full technical description |
| L | Evidence | String | Sanitised code snippet (max 10 lines; PII/secrets **redacted**) |
| M | Impact | String | Technical and business impact |
| N | Likelihood | Enum | `High` / `Medium` / `Low` |
| O | Recommendation | String | Step-by-step remediation guidance |
| P | References | String | CVE IDs, CWE links, OWASP URLs |
| Q | Status | Enum | `Open` / `In Review` / `Remediated` / `Risk Accepted` |
| R | Remediation Deadline | Date | Per SLA in `references/severity-sla.md` |
| S | Retested | Enum | `Yes` / `No` |
| T | Retest Date | Date | Date of retest |
| U | Notes | String | FP flags, suppression reason, manual review tags, risk acceptance records |
| V | Source Tool | Enum | `SAST` / `SCA` / `Secrets` / `IaC` / `AI-Assisted` ← **v2.1 NEW** |
| W | AI Confidence | Float | `0.0–1.0` if AI-Assisted; blank otherwise ← **v2.1 NEW** |

---

## 5. Defra Compliance Rules (Non-Negotiable)

The following rules are enforced on every run and **cannot** be overridden by user parameters.

| Rule | Enforcement |
|---|---|
| `pii_redaction = true` | Hard-coded default; block if disabled |
| `send_to_external_ai = false` unless `--enable-cloud-ai` explicitly passed | Block external calls by default |
| Secrets never appear in output | Redact before any write operation |
| AI findings must carry confidence score in column W | Refuse to omit |
| AI findings < 0.70 confidence → auto-downgrade to Informational | Enforced in Step 8 |
| Observability log must record: agent version, scan date, mode, file count, finding counts by source | Append on every run completion |
| Report footer must include Defra compliance statement | Auto-appended; cannot be suppressed |
| Missing required skill files → halt with structured error | Do not proceed with partial files |

---

## 6. Observability Log Entry Format

```json
{
  "agent_id": "security-code-2.1.0",
  "scan_date": "<ISO-8601-UTC>",
  "scan_mode": "<mode>",
  "project_path": "<path>",
  "file_count": 0,
  "findings": {
    "total": 0,
    "critical": 0, "high": 0, "medium": 0, "low": 0, "informational": 0
  },
  "findings_by_source": {
    "sast": 0, "sca": 0, "secrets": 0, "iac": 0, "ai_assisted": 0
  },
  "pii_redacted": 0,
  "secrets_redacted": 0,
  "data_classification": "Official",
  "operator": "<user-or-system>",
  "duration_seconds": 0
}
```

---

## 7. Escalation Triggers

| Trigger | Required Action |
|---|---|
| Any **Critical** finding | 🚨 Notify security lead within 1 hour. Block deployment. |
| Secrets found in git history | 🚨 Revoke and rotate immediately. Do not wait for fix cycle. |
| Authentication bypass finding | 🚨 Production may be compromised. Notify security team. |
| PII in code + `Official Sensitive` classification | 🚨 Notify DPO and `AICapabilityAndEnablement@defra.gov.uk` |

---

## 8. Responsible Use (Defra AI Toolkit)

| Pillar | Requirement |
|---|---|
| **Security** | No secrets in output; AI confidence metadata mandatory; secure report distribution |
| **Data Safety** | Local analysis by default; PII redaction always on; cloud AI opt-in only |
| **Ethics** | False positive rate disclosed (~8–12% for AI-assisted); bias mitigated via SAST corroboration |
| **Sustainability** | Use the appropriate scan mode; avoid full AI inference when SAST is sufficient |
| **Transparency** | All findings tagged by source; AI findings clearly marked for human review |
| **Incident Reporting** | `AICapabilityAndEnablement@defra.gov.uk` \| `https://github.com/DEFRA/lap-civica-agents/issues` |

---

## Standards

This skill is loaded by the [security-code agent](./../../agents/security-code.agent.md). This skill is classified as **HIGH RISK** — all findings must be human-reviewed before remediation is applied. All outputs are subject to AI transparency disclosure, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/).
