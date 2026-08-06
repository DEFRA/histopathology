# CWE to OWASP Top 10 (2021) Mapping
## ITHC Security Scanner Reference v2.1.0

> Source: https://owasp.org/Top10/ | Updated: 2026-07-24
> Usage: Referenced in SKILL.md Step 8 to populate column G (OWASP Category)

---

## OWASP Top 10 (2021) Quick Reference

| Code | Category |
|---|---|
| A01:2021 | Broken Access Control |
| A02:2021 | Cryptographic Failures |
| A03:2021 | Injection |
| A04:2021 | Insecure Design |
| A05:2021 | Security Misconfiguration |
| A06:2021 | Vulnerable and Outdated Components |
| A07:2021 | Identification and Authentication Failures |
| A08:2021 | Software and Data Integrity Failures |
| A09:2021 | Security Logging and Monitoring Failures |
| A10:2021 | Server-Side Request Forgery (SSRF) |

---

## CWE to OWASP Mapping

| CWE ID | CWE Name | OWASP Category | Default Severity |
|---|---|---|---|
| CWE-22 | Path Traversal | A01:2021 Broken Access Control | High |
| CWE-23 | Relative Path Traversal | A01:2021 Broken Access Control | High |
| CWE-59 | Link Following | A01:2021 Broken Access Control | Medium |
| CWE-73 | External Control of File Name | A01:2021 Broken Access Control | High |
| CWE-78 | OS Command Injection | A03:2021 Injection | Critical |
| CWE-79 | Cross-Site Scripting | A03:2021 Injection | High |
| CWE-89 | SQL Injection | A03:2021 Injection | Critical |
| CWE-90 | LDAP Injection | A03:2021 Injection | High |
| CWE-94 | Code Injection | A03:2021 Injection | Critical |
| CWE-95 | Eval Injection | A03:2021 Injection | Critical |
| CWE-116 | Improper Encoding | A03:2021 Injection | Medium |
| CWE-117 | Log Injection | A09:2021 Security Logging Failures | Medium |
| CWE-200 | Sensitive Information Exposure | A02:2021 Cryptographic Failures | High |
| CWE-209 | Error Message With Sensitive Info | A05:2021 Security Misconfiguration | Medium |
| CWE-215 | Info Exposure via Debug Info | A05:2021 Security Misconfiguration | Low |
| CWE-269 | Improper Privilege Management | A01:2021 Broken Access Control | High |
| CWE-284 | Improper Access Control | A01:2021 Broken Access Control | High |
| CWE-285 | Improper Authorization | A01:2021 Broken Access Control | High |
| CWE-287 | Improper Authentication | A07:2021 Identification and Auth Failures | Critical |
| CWE-295 | Improper Certificate Validation | A02:2021 Cryptographic Failures | High |
| CWE-306 | Missing Auth for Critical Function | A07:2021 Identification and Auth Failures | Critical |
| CWE-307 | Brute Force Protection Missing | A07:2021 Identification and Auth Failures | High |
| CWE-308 | Single-Factor Auth on Critical Function | A07:2021 Identification and Auth Failures | High |
| CWE-311 | Missing Encryption of Sensitive Data | A02:2021 Cryptographic Failures | High |
| CWE-312 | Cleartext Storage of Sensitive Info | A02:2021 Cryptographic Failures | High |
| CWE-319 | Cleartext Transmission | A02:2021 Cryptographic Failures | High |
| CWE-321 | Hard-coded Cryptographic Key | A02:2021 Cryptographic Failures | Critical |
| CWE-326 | Inadequate Encryption Strength | A02:2021 Cryptographic Failures | High |
| CWE-327 | Broken Cryptographic Algorithm | A02:2021 Cryptographic Failures | High |
| CWE-328 | Reversible One-Way Hash | A02:2021 Cryptographic Failures | High |
| CWE-329 | Non-random IV with CBC Mode | A02:2021 Cryptographic Failures | High |
| CWE-330 | Insufficient Randomness | A07:2021 Identification and Auth Failures | High |
| CWE-338 | Weak PRNG for Security | A02:2021 Cryptographic Failures | High |
| CWE-345 | Insufficient Data Authentication | A08:2021 Software and Data Integrity | High |
| CWE-346 | Origin Validation Error | A01:2021 Broken Access Control | High |
| CWE-352 | CSRF | A01:2021 Broken Access Control | High |
| CWE-400 | Uncontrolled Resource Consumption | A05:2021 Security Misconfiguration | Medium |
| CWE-434 | Unrestricted File Upload | A03:2021 Injection | High |
| CWE-494 | Download Without Integrity Check | A08:2021 Software and Data Integrity | High |
| CWE-502 | Deserialization of Untrusted Data | A08:2021 Software and Data Integrity | Critical |
| CWE-521 | Weak Password Requirements | A07:2021 Identification and Auth Failures | Medium |
| CWE-522 | Insufficiently Protected Credentials | A07:2021 Identification and Auth Failures | High |
| CWE-532 | Sensitive Info in Log Files | A09:2021 Security Logging Failures | Medium |
| CWE-598 | Sensitive Info in Query String | A07:2021 Identification and Auth Failures | Medium |
| CWE-601 | Open Redirect | A01:2021 Broken Access Control | Medium |
| CWE-611 | XML External Entity (XXE) | A05:2021 Security Misconfiguration | High |
| CWE-613 | Insufficient Session Expiration | A07:2021 Identification and Auth Failures | Medium |
| CWE-614 | Sensitive Cookie Missing Secure Flag | A07:2021 Identification and Auth Failures | Medium |
| CWE-643 | XPath Injection | A03:2021 Injection | High |
| CWE-732 | Incorrect Permission Assignment | A01:2021 Broken Access Control | High |
| CWE-770 | Allocation Without Limits | A05:2021 Security Misconfiguration | Medium |
| CWE-778 | Insufficient Logging | A09:2021 Security Logging Failures | Medium |
| CWE-798 | Hardcoded Credentials | A07:2021 Identification and Auth Failures | Critical |
| CWE-829 | Inclusion from Untrusted Source | A08:2021 Software and Data Integrity | High |
| CWE-862 | Missing Authorization | A01:2021 Broken Access Control | High |
| CWE-863 | Incorrect Authorization | A01:2021 Broken Access Control | High |
| CWE-918 | Server-Side Request Forgery | A10:2021 SSRF | High |
| CWE-943 | NoSQL Injection | A03:2021 Injection | High |
| CWE-1021 | Clickjacking | A05:2021 Security Misconfiguration | Medium |
| CWE-1275 | Sensitive Cookie Without SameSite | A07:2021 Identification and Auth Failures | Medium |

