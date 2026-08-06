# Severity SLA and Escalation Reference
## ITHC Security Scanner v2.1.0

> Standard: NCSC Cyber Essentials + Defra ITHC SLA framework
> Updated: 2026-07-24
> Usage: Referenced in SKILL.md Step 8 to populate column R (Remediation Deadline)

---

## Severity Bands, SLA and Pipeline Actions

| Severity | CVSSv3.1 Range | Remediation Deadline | Pipeline Gate | Defra Escalation |
|---|---|---|---|---|
| Critical | 9.0 - 10.0 | 24 hours | Block deployment immediately | Notify security lead within 1 hour |
| High | 7.0 - 8.9 | 7 days | Block deployment | Notify security team same day |
| Medium | 4.0 - 6.9 | 30 days | Warn only | Risk decision gate (may accept with sign-off) |
| Low | 0.1 - 3.9 | 90 days | Log only | Backlog; standard sprint prioritisation |
| Informational | 0.0 | Best effort | Log only | Not actionable; awareness only |

---

## Deadline Calculation Rules

1. Deadline is calculated from scan date (report metadata, not commit date).
2. Deadlines falling on UK weekends or Bank Holidays move to the next working day.
   (Bank Holiday reference: https://www.gov.uk/bank-holidays)
3. Secrets found in git history (not HEAD): deadline starts from today, not original commit date.
4. Risk Accepted findings must document in column U: approver, date of acceptance, review date.
   - Max review period: 90 days for Medium, 30 days for High

---

## Risk Acceptance Policy

| Severity | Risk Acceptance Allowed | Required Approver |
|---|---|---|
| Critical | No - must remediate | N/A |
| High | Exceptional cases only | Security Lead + Programme Director |
| Medium | Yes | Security Lead |
| Low | Yes | Team Lead |
| Informational | Yes | Engineer |

---

## CI/CD Pipeline Integration

Set fail_on_severity in the agent invocation to control the pipeline exit code:

| fail_on_severity Value | Pipeline exits non-zero when |
|---|---|
| Critical | Any Critical finding present |
| High | Any Critical or High finding present |
| Medium | Any Critical, High, or Medium present |
| Low | Any non-Informational finding present |
| (not set) | Always exits 0 - findings logged only |

Recommended gate for pre-release pipelines: fail_on_severity: High

---

## Escalation Contacts

| Scenario | Contact | Response SLA |
|---|---|---|
| Critical finding in production code | Security Lead (internal) | 1 hour |
| Leaked secret in git history | Security Lead - rotate credential immediately | Immediate |
| PII exposure (Official Sensitive data) | Data Protection Officer + AICapabilityAndEnablement@defra.gov.uk | 1 hour |
| AI bias or hallucination in findings | AICapabilityAndEnablement@defra.gov.uk | 24-48 hours |
| General security finding queries | Security team via repo issue | 7 days |

Issue Tracker: https://github.com/DEFRA/lap-civica-agents/issues
Incident Tag: [SECURITY-CODE-INCIDENT]

