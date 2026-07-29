# ADR-003: Per-Page Role Check via String Comparison

**Date:** Discovered 2026-07-27  
**Status:** Implicit — no centralised authorisation design documented  
**Source:** All ASPX `.vb` code-behind files; `Home.aspx.vb::EnableControls`, `ReceiveBatch.aspx.vb::CheckPermissions`, and 58 other pages

---

## Context

The application serves three distinct user groups with different access rights:
- `"Customer"` — submission entry and viewing only
- `"Histopathology User"` — full lab workflow
- `"Maintenance"` — admin functions

A mechanism was needed to enforce which pages each group can access, and to conditionally show/hide UI controls per group.

## Decision

Each ASPX page implements its own `CheckPermissions()` method (or equivalent logic in `Page_Load`) that:
1. Calls `VLAHeader1.GetUserDetails()` to ensure `Session(SV_HeaderGroupName)` is populated
2. Reads the group name string from `Session`
3. Compares against the three known string literals
4. Redirects to `Home.aspx` if the current group is not permitted

UI control visibility is also conditionally set per-group in methods like `EnableCustomerLinks()`, `EnableHistologyUserLinks()`, `EnableHistologyMaintenanceLinks()` in `Home.aspx.vb`.

## Consequences

**Positive:**
- Simple to understand and trace — the access rule is visible in every page file
- No framework configuration; works with any ASP.NET version

**Negative:**
- Three string literals (`"Customer"`, `"Histopathology User"`, `"Maintenance"`) are scattered across 60+ files — a rename or new group requires editing every page
- No centralised audit of which pages are accessible to which group — requires manual inspection of all pages
- No compile-time safety — a typo in the group-name string produces a silent grant (the `Else` branch redirects, but only if the exact string matches none of the known groups)
- `CheckPermissions()` can be omitted from a new page inadvertently — no framework enforcement prevents a page from being served without a check
- Incompatible with middleware-level or attribute-level authorisation patterns required by modern ASP.NET Core
