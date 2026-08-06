# Skill: upgrade-report-generator
 
## Purpose
Generate a fully styled, self-contained HTML report documenting every change applied during the framework upgrade. The report is the primary deliverable for stakeholders and reviewers. It must render correctly offline, contain no external dependencies, and never include secret values.
 
## Trigger Conditions
- Always runs **last**, regardless of whether previous skills completed fully or partially.
- A partial-completion report is valid and expected — missing sections are marked as "Skipped".
 
---
 
## Step 1 — Collect Inputs from All Preceding Skills
 
| Input File | Source Skill |
|---|---|
| `upgrade-path-analysis-report.json` | `upgrade-path-analysis` |
| `framework-upgrade-report.json` | `framework-upgrade` |
| `nuget-upgrade-report.json` | `nuget-package-upgrade` |
| `build-validation-report.json` | `build-validation` |
 
If any input file is absent, mark the corresponding section as **"Skipped / Not Applied"**.
 
---
 
## Step 2 — Generate HTML Report
 
Produce a single, self-contained `.html` file with all CSS embedded in a `<style>` block. No external stylesheets, scripts, or CDN references.
 
### Required Sections
 
#### 1 — Executive Summary
 
- Application name (from `solutionFolder` parameter)
- Source framework → Target framework
- Report date
- Overall status badge: ✅ **Complete** / ⚠️ **Partial** / ❌ **Blocked**
- Upgrade path type: Classic→Classic / Classic→Modern / SDK→SDK
- Summary counters:
 
| Metric | Value |
|---|---|
| Project files upgraded | n |
| web.config files updated | n |
| NuGet packages upgraded | n |
| CVE-flagged packages resolved | n |
| Build status | ✅ Zero errors / ❌ n errors remaining |
| WebForms stubs created (Classic→Modern only) | n |
 
#### 2 — Upgrade Path Analysis
 
- Path type classification
- Project inventory table (file, format, old version)
- Risk matrix (Classic→Modern only):
 
| File | Risk Item | Risk Level | Recommended Action |
|---|---|---|---|
| `src/Pages/SamplePage.aspx` | WebForms page | High | Migrate to Razor Pages |
| `src/Services/SampleService.vb` | `System.Web.Mail` | Low | Replace with MailKit |
 
- Effort estimate
 
#### 3 — Framework Changes
 
- Table: Project File | Old Version | New Version | Status
- `web.config` changes per file: `httpRuntime` before/after diff, binding redirects added
 
#### 4 — NuGet Package Upgrades
 
- Table with CVE rows highlighted:
 
| Package | Old Version | New Version | CVE | Status |
|---|---|---|---|---|
| `Newtonsoft.Json` | 9.0.1 | 13.0.3 | — | ✅ Upgraded |
| `SamplePackage` | 2.1.0 | 3.5.0 | CVE-2023-1234 | ✅ Resolved |
 
- Packages with no compatible modern version (if Classic→Modern) listed as blocking items
 
#### 5 — Build Validation
 
- Build tool used (MSBuild / dotnet CLI)
- Build exit code
- Error count before and after iterative fixes
- Remaining errors list (if any) with file, line, error code
- Warning baseline count
- Assembly binding redirect changes
- Test results summary (if test projects present)
 
#### 6 — Classic → Modern Migration Notes (conditional)
 
Show only when `pathType = ClassicToModern`:
 
- WebForms stubs created: list of `.aspx` files and their corresponding stub `.cshtml` paths
- `Global.asax` → `Program.cs` migration summary
- `web.config` sections migrated to `appsettings.json`
- Manual migration items (items the agent cannot automate)
 
#### 7 — Skipped / Blocked Items
 
- Any skill that did not complete, with reason
- Manual steps required before go-live:
 
| Item | Reason | Owner |
|---|---|---|
| WebForms page logic migration | Cannot be automated | Development team |
| Key Vault secret population | Requires secure operator access | Platform team |
| SQL managed identity grant | T-SQL data-plane step | DBA |
 
#### 8 — Appendix — All Files Changed
 
Full flat list sorted by skill category.
 
---
 
## Step 3 — Apply CSS Styling
 
```css
body { font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; color: #2c3e50; margin: 0; padding: 20px; }
h1 { background: #1a3a5c; color: #fff; padding: 18px 24px; border-radius: 6px; }
h2 { color: #1a3a5c; border-bottom: 2px solid #1a3a5c; padding-bottom: 6px; margin-top: 32px; }
h3 { color: #2e6da4; }
table { border-collapse: collapse; width: 100%; margin: 16px 0; background: #fff; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
th { background: #1a3a5c; color: #fff; padding: 10px 14px; text-align: left; font-size: 0.92em; }
td { padding: 9px 14px; border-bottom: 1px solid #e8edf2; font-size: 0.91em; }
tr:last-child td { border-bottom: none; }
tr:hover td { background: #f0f5fb; }
.badge-ok   { background: #27ae60; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-warn { background: #e67e22; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-err  { background: #c0392b; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-skip { background: #95a5a6; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-high { background: #c0392b; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-med  { background: #e67e22; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-low  { background: #27ae60; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.cve        { background: #fdecea; }
pre, code   { background: #f4f6f9; border: 1px solid #dde3ea; border-radius: 4px; padding: 2px 6px; font-size: 0.88em; }
pre         { padding: 12px; overflow-x: auto; }
.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin: 20px 0; }
.summary-card { background: #fff; border-radius: 8px; padding: 18px 20px; box-shadow: 0 1px 4px rgba(0,0,0,0.08); text-align: center; }
.summary-card .number { font-size: 2.2em; font-weight: 700; color: #1a3a5c; }
.summary-card .label  { font-size: 0.85em; color: #7f8c8d; margin-top: 4px; }
.path-badge { display: inline-block; background: #2980b9; color: #fff; padding: 4px 14px; border-radius: 14px; font-size: 0.9em; font-weight: 600; }
```
 
---
 
## Step 4 — Write Output File
 
Write the report to:
 
```
<reportOutputFolder>\upgrade-report-<YYYY-MM-DD>.html
```
 
Create the `upgrade-reports\` directory if it does not exist.
 
Add `upgrade-reports/` to `.gitignore` if not already present — generated reports must not be committed to source control.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `upgrade-reports/upgrade-report-<date>.html` | Self-contained styled HTML report |
 
---
 
## Constraints
 
- The HTML file must be **fully self-contained** — no `<link>` to external CSS, no CDN `<script>` tags.
- Do **not** include any secret values (connection strings, instrumentation keys, passwords) even if they appear in diff context.
- The file must **not** be committed to source control.
- The report must render correctly in Microsoft Edge, Chrome, and Firefox with no external network requests.