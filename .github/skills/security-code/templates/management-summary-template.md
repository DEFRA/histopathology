# Management Summary - Security Assessment
## [PROJECT NAME]

> Classification: [DATA_CLASSIFICATION]
> Date: [SCAN_DATE]
> Prepared by: ITHC Security Findings Scanner v2.1.0 (Defra AI Toolkit Compliant)
> Audience: Senior Leadership, Product Owner, Programme Manager

---

## 1. Overview

A security assessment was conducted against [PROJECT NAME] covering static code analysis
(SAST), dependency scanning (SCA), secrets detection, and infrastructure-as-code review.
This summary provides a non-technical overview of findings and required actions.

---

## 2. Risk Summary

| Risk Level | Findings | Immediate Action Required |
|---|---|---|
| Critical | [COUNT_CRITICAL] | Yes - within 24 hours |
| High | [COUNT_HIGH] | Yes - within 7 days |
| Medium | [COUNT_MEDIUM] | Within 30 days |
| Low | [COUNT_LOW] | Within 90 days |
| Informational | [COUNT_INFO] | Best effort |

Overall Risk Rating: [RISK_RATING]

---

## 3. Key Findings (Plain English)

### Finding 1: [TITLE] ([SEVERITY])

What it means: [BUSINESS_IMPACT_PLAIN_ENGLISH]
Risk to the organisation: [BUSINESS_RISK]
What needs to happen: [PLAIN_ENGLISH_RECOMMENDATION]
Deadline: [DEADLINE]

---

### Finding 2: [TITLE] ([SEVERITY])

What it means: [BUSINESS_IMPACT_PLAIN_ENGLISH]
Risk to the organisation: [BUSINESS_RISK]
What needs to happen: [PLAIN_ENGLISH_RECOMMENDATION]
Deadline: [DEADLINE]

---

## 4. Dependency and Third-Party Risk

- Total third-party packages scanned: [TOTAL_PACKAGES]
- Packages with known vulnerabilities: [VULNERABLE_PACKAGES]
- Critical/High CVEs in dependencies: [CRITICAL_HIGH_CVE_COUNT]

Outdated or vulnerable third-party components represent a supply chain risk and
should be upgraded as a priority alongside code-level fixes.

---

## 5. Immediate Actions Required

The following items must be actioned before the next deployment to production:

| # | Action | Owner | Deadline |
|---|---|---|---|
| 1 | [ACTION_1] | [OWNER_1] | [DEADLINE_1] |
| 2 | [ACTION_2] | [OWNER_2] | [DEADLINE_2] |
| 3 | [ACTION_3] | [OWNER_3] | [DEADLINE_3] |

---

## 6. Compliance Statement

This assessment was conducted using tooling aligned with:
- UK Government NCSC Cyber Essentials baseline
- OWASP Top 10 (2021)
- Defra AI Toolkit v1.0 (responsible AI use, data safety, PII redaction)

All sensitive data and credentials identified during the scan have been redacted from
this report. The full technical findings are available in the detailed ITHC report.

Questions: AICapabilityAndEnablement@defra.gov.uk

