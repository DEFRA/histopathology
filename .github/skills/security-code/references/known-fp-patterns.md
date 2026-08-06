# Known False Positive Patterns
## ITHC Security Scanner - AI-Assisted Analysis Reference

> **Version:** 2.1.0
> **Updated:** 2026-07-24
> **Purpose:** Suppress confirmed false positives from AI-assisted findings (Step 6 of SKILL.md).

---

## How to Use

When an AI-assisted finding matches a pattern here:
1. Suppress the finding - do not include in output
2. Log in column U: Suppressed: FP-ID - reason
3. Increment false_positive_suppressed counter in the observability log

To dispute: https://github.com/DEFRA/lap-civica-agents/issues [tag: SECURITY-CODE-FP-REVIEW]

---

## FP-001 - SQL Inside ORM / LINQ Expression (Not Raw SQL)

**Why FP:** LINQ and EF produce parameterised queries. SQL keywords appear in expression trees, not raw strings.
**Suppress if:** Code uses .Where(), .Select(), DbSet query syntax
**Do NOT suppress if:** ExecuteSqlRaw() or FromSqlRaw() with string concatenation is used

---

## FP-002 - MD5 for Non-Security Checksums

**Why FP:** MD5 is acceptable for cache keys, ETags, file deduplication.
**Suppress if:** MD5 output used for cache busting, ETag, file dedup
**Do NOT suppress if:** MD5 used for password hashing, HMAC, digital signatures

---

## FP-003 - Test / Seed Data Credentials

**Why FP:** Fake credentials in dedicated test projects are expected.
**Suppress if:** File path contains: Test, Spec, Fixture, Seed, Mock, Fake, Sample, Example
**Do NOT suppress if:** Value matches real format (AKIA*, ghp_*, valid JWT, Azure SAS)

---

## FP-004 - System.Random for Non-Security Use

**Why FP:** System.Random is appropriate for UI shuffling, test data, jitter, seeds.
**Suppress if:** Random used for non-security purposes
**Do NOT suppress if:** Random used for tokens, session IDs, CSRF nonces, encryption keys

---

## FP-005 - Commented-Out Code With Old Credentials

**Why FP:** Placeholder credential patterns in comments are not active.
**Suppress if:** Match is entirely within a comment AND does not match a real credential format
**Do NOT suppress if:** Value matches AKIA*, ghp_*, eyJ* - treat as Secrets finding

---

## FP-006 - SCA CVE Version Substring Mismatch

**Why FP:** Version comparison bugs in SCA tools can incorrectly match patch versions.
**Suppress if:** Actual version is outside CVE affected range per NVD advisory
**Evidence required:** Include NVD URL and affected version range in column U

---

## FP-007 - Razor @ Syntax Flagged as Template Injection

**Why FP:** ASP.NET Razor HTML-encodes all @ expressions by default.
**Suppress if:** Expression does not use @Html.Raw(), @MvcHtmlString, or raw-output helpers
**Do NOT suppress if:** @Html.Raw(userInput) is detected - genuine XSS (CWE-79)

---

## FP-008 - SQL in EF Migrations or Flyway Scripts

**Why FP:** Migration scripts contain intentional raw SQL run by infrastructure, not user input.
**Suppress if:** File is in /Migrations/, /db/migrations/, or /flyway/ directory
**Do NOT suppress if:** Migration uses string interpolation from variables

