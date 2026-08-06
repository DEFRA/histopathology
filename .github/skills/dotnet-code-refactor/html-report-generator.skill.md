# Skill: html-report-generator
 
## Purpose
Generate a fully styled, self-contained HTML conversion report that clearly documents every change made during the refactoring, framework upgrade, NuGet upgrade, exception handling improvement, and logging enhancement process. The report is the primary artefact delivered to the engineering team and reviewers after a refactoring run.
 
## Trigger Conditions
- All other skills have completed (or partially completed with documented gaps).
- Called as the **final step** in the agent's execution order.
 
---
 
## Step 1 — Collect Inputs from All Preceding Skills
 
Aggregate the JSON output reports from each completed skill:
 
| Input File | Source Skill |
|---|---|
| `framework-upgrade-report.json` | `framework-upgrade` |
| `nuget-upgrade-report.json` | `nuget-package-upgrade` |
| `build-validation-report.json` | `build-validation` |
| `code-cleanup-report.json` | `code-cleanup-refactor` |
| `exception-handling-report.json` | `global-exception-handling` |
| `logging-enhancement-report.json` | `appinsights-logging` |
 
If any input file is absent (skill did not run or did not produce output), mark the corresponding section as **"Skipped / Not Applied"** in the report — do not fail.
 
---
 
## Step 2 — Generate HTML Report
 
Produce a single, self-contained `.html` file with embedded CSS. Do not reference external stylesheets or scripts — the file must render correctly offline and in any email client.
 
### Required Sections
 
#### 1. Executive Summary
- Application name and solution file
- Report generation date
- Overall status badge: ✅ **Complete** / ⚠️ **Partial** / ❌ **Blocked**
- Total files changed count
- Total NuGet packages upgraded count
- Build status (zero errors / errors remaining)
 
#### 2. Framework Upgrade
- Table: File | Old Version | New Version | Status
- **Classic→Modern** changes (when `pathType` = Classic→Modern):
  - Project file conversion: `.vbproj` classic format → SDK-style with `<TargetFramework>net10.0</TargetFramework>`
  - `web.config` `<appSettings>` keys migrated to `appsettings.json` — list of keys moved (values masked if sensitive)
  - `Program.cs` skeleton created — list of `TODO` placeholders for manual wiring
  - `Global.asax` flagged as dead code — lifecycle methods mapped to their ASP.NET Core equivalents
  - Stub Razor Pages created: table of `.aspx` source → `.cshtml`/`.cshtml.cs` stub target
 
#### 3. NuGet Package Upgrades
- Table: Package | Old Version | New Version | CVE Flag | Status
- Highlight rows where CVE was flagged in **red**
 
#### 4. Code Cleanup
- Count of dead code removals with file references
- Count of naming convention fixes
- List of methods split (file, original name, resulting method names)
- TODO/FIXME/HACK comment catalogue: Comment Text | File | Line | Recommended Action
 
#### 5. Exception Handling
- Count of `On Error Resume Next` patterns removed
- **Domain Exception Hierarchy** diagram (ASCII or structured list)
- Table: File | Old Pattern | New Pattern | Error Type
- Empty catch blocks removed: File | Line | Added Logging Statement
- Global exception handler: `UseExceptionHandler` middleware wired in `Program.cs` — confirm present or flag as missing
 
#### 6. Logging Enhancement
- NuGet packages added: `Microsoft.ApplicationInsights.AspNetCore` version
- `TelemetryHelper` DI registration summary: registered as scoped service in `Program.cs`; `IHttpContextAccessor` injected for contextual properties
- Table: File | Old Logging Method | New Logging Call | Log Level
- Log level mapping applied:

| Legacy Pattern | New Application Insights Call | Level |
|---|---|---|
| `EventLog.WriteEntry(..., Error)` | `_telemetryHelper.TrackException(ex, ...)` | Error |
| `Debug.Print` | `_telemetryHelper.TrackTrace(..., SeverityLevel.Verbose)` | Verbose |
| `Console.WriteLine` (info) | `_telemetryHelper.TrackTrace(..., SeverityLevel.Information)` | Information |
| `Response.Write` (debug) | `_telemetryHelper.TrackTrace(..., SeverityLevel.Warning)` | Warning |
#### 7. Build Validation
- Build exit code and status
- Error count and warning count (before and after)
- Remaining warnings list with file and line references
- Assembly binding redirect changes
 
#### 8. Skipped Items / Follow-on Actions
- Any item the agent could not complete automatically, with reason and recommended manual action
 
#### 9. Appendix — Files Changed
- Full flat list of all files modified, sorted by skill category
 
---
 
## Step 3 — Apply CSS Styling
 
The HTML must use the following embedded CSS palette for professional presentation:
 
```css
/* Embedded in <style> block in <head> */
body { font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; color: #2c3e50; margin: 0; padding: 20px; }
h1 { background: #1a3a5c; color: #fff; padding: 18px 24px; border-radius: 6px; }
h2 { color: #1a3a5c; border-bottom: 2px solid #1a3a5c; padding-bottom: 6px; margin-top: 32px; }
h3 { color: #2e6da4; }
table { border-collapse: collapse; width: 100%; margin: 16px 0; background: #fff; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 4px rgba(0,0,0,0.08); }
th { background: #1a3a5c; color: #fff; padding: 10px 14px; text-align: left; font-size: 0.92em; }
td { padding: 9px 14px; border-bottom: 1px solid #e8edf2; font-size: 0.91em; }
tr:last-child td { border-bottom: none; }
tr:hover td { background: #f0f5fb; }
.badge-ok { background: #27ae60; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-warn { background: #e67e22; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-err { background: #c0392b; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.badge-skip { background: #95a5a6; color: #fff; padding: 2px 10px; border-radius: 12px; font-size: 0.85em; }
.cve { background: #fdecea; }
pre, code { background: #f4f6f9; border: 1px solid #dde3ea; border-radius: 4px; padding: 2px 6px; font-size: 0.88em; }
pre { padding: 12px; overflow-x: auto; }
.summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin: 20px 0; }
.summary-card { background: #fff; border-radius: 8px; padding: 18px 20px; box-shadow: 0 1px 4px rgba(0,0,0,0.08); text-align: center; }
.summary-card .number { font-size: 2.2em; font-weight: 700; color: #1a3a5c; }
.summary-card .label { font-size: 0.85em; color: #7f8c8d; margin-top: 4px; }
```
 
---
 
## Step 4 — Write Output File
 
Write the generated HTML to:
 
```
bse\docs\code-refactor\conversion-report-<YYYY-MM-DD>.html
```

If the `docs\code-refactor\` directory does not exist, create it.
## Outputs
 
| Output | Description |
|---|---|
| `docs/code-refactor/conversion-report-<date>.html` | Full styled HTML report |
 
---
 
## Constraints
 
- The HTML file must be **self-contained** — no `<link>` to external CSS, no `<script src="...">` to CDNs.
- Do **not** include any secret values (connection strings, keys) in the report even if they appear in diff context.
- The report file must **not** be committed to source control (add `docs/code-refactor/` to `.gitignore` if not already present).