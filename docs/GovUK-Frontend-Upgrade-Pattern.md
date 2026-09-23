# GOV.UK Frontend — Upgrade Pattern and Approach

**Document type:** Engineering Reference — Frontend Asset Management  
**Date:** 2026-08-04  
**Application:** Histo.Web (ASP.NET Core Razor Pages, .NET 10)  
**Hosting:** Azure App Service (Windows), deployed via Azure DevOps  
**Current version:** 6.4.0

---

## 1. Context

The Histo.Web application uses [GOV.UK Frontend](https://frontend.design-system.service.gov.uk/) for all UI styling and JavaScript components. Because the front end is built with **Razor Pages** (not Nunjucks), only the **pre-built distribution files** are required — no Sass compilation, no Nunjucks macros, no build toolchain beyond an npm install to retrieve the correct package.

The three files consumed at runtime are:

| File | Purpose | Location in project |
|---|---|---|
| `govuk-frontend.min.css` | All GOV.UK Design System styles | `wwwroot/govuk/` |
| `govuk-frontend.min.js` | Component JavaScript (accordion, skip link, etc.) | `wwwroot/govuk/` |
| `assets/` | GDS Transport fonts + GOV.UK icons/favicon | `wwwroot/govuk/assets/` |

These are referenced in [src/Histo.Web/Pages/Shared/_Layout.cshtml](../src/Histo.Web/Pages/Shared/_Layout.cshtml) as static files:

```html
<link rel="stylesheet" href="~/govuk/govuk-frontend.min.css" asp-append-version="true" />
<script type="module" src="~/govuk/govuk-frontend.min.js" asp-append-version="true"></script>
```

The `asp-append-version="true"` attribute handles cache-busting automatically — no changes to `_Layout.cshtml` are needed when upgrading govuk-frontend.

---

## 2. Why Not Commit the Files to Git

The `wwwroot/govuk/` folder contains pre-built binary/text assets totalling ~1 MB uncompressed. Committing them directly:

- Bloats git history on every upgrade (binary diffs for CSS/JS/fonts/images)
- Has no version audit trail — only the files themselves record what version is in use
- Requires manual file downloads to upgrade (error-prone, no verification)
- Makes pull request diffs noisy with irrelevant asset changes

The `wwwroot/govuk/` folder is therefore **excluded from git** and **generated at build time** in the Azure DevOps pipeline.

---

## 3. Version-of-Record: `package.json`

The declared govuk-frontend version lives in a single file:

**[src/Histo.Web/package.json](../src/Histo.Web/package.json)**

```json
{
  "name": "histo-web",
  "version": "1.0.0",
  "private": true,
  "dependencies": {
    "govuk-frontend": "6.4.0"
  }
}
```

This is the **only file that changes** when upgrading. The version must be pinned exactly (no `^` or `~` range specifiers) so the pipeline is deterministic and every environment gets the same files.

---

## 4. `.gitignore` Entries

The following entries must be present in the root [.gitignore](../.gitignore) to exclude generated assets and npm internals from source control:

```gitignore
# GOV.UK Frontend — generated at build time from package.json, not committed
src/Histo.Web/node_modules/
src/Histo.Web/wwwroot/govuk/
```

> **Note for local development:** The `wwwroot/govuk/` files may exist on disk locally (placed there during a previous manual update or local `npm install`). Git ignores them but they still work at runtime. See Section 7 for local dev instructions.

---

## 5. Azure DevOps Pipeline — Steps

Add the following three steps **before** the `dotnet publish` step in the Azure DevOps pipeline YAML. These steps are idempotent — they produce the same output on every run.

```yaml
# ─────────────────────────────────────────────────────────────
# Step 1: Ensure Node.js is available on the agent
# ─────────────────────────────────────────────────────────────
- task: NodeTool@0
  inputs:
    versionSpec: '20.x'
  displayName: 'Install Node.js 20'

# ─────────────────────────────────────────────────────────────
# Step 2: Install govuk-frontend at the declared version
# npm ci uses package.json for the exact version — never drifts
# ─────────────────────────────────────────────────────────────
- script: npm ci
  workingDirectory: src/Histo.Web
  displayName: 'npm ci — restore govuk-frontend $(cat src/Histo.Web/package.json | grep govuk-frontend)'

# ─────────────────────────────────────────────────────────────
# Step 3: Copy pre-built dist files into wwwroot/govuk/
# Only the four pre-built files + assets folder are needed.
# node_modules/ itself is NOT deployed — only the output files.
# ─────────────────────────────────────────────────────────────
- powershell: |
    $src  = "src/Histo.Web/node_modules/govuk-frontend/dist/govuk"
    $dest = "src/Histo.Web/wwwroot/govuk"

    New-Item -ItemType Directory -Force -Path $dest         | Out-Null
    New-Item -ItemType Directory -Force -Path "$dest/assets" | Out-Null

    Copy-Item "$src/govuk-frontend.min.css"     $dest -Force
    Copy-Item "$src/govuk-frontend.min.css.map" $dest -Force
    Copy-Item "$src/govuk-frontend.min.js"      $dest -Force
    Copy-Item "$src/govuk-frontend.min.js.map"  $dest -Force
    Copy-Item "$src/assets"  "$dest/assets" -Recurse -Force

    Write-Host "govuk-frontend assets staged:"
    Get-ChildItem $dest -Recurse -File | Select-Object Name, @{N="KB";E={[math]::Round($_.Length/1KB,1)}}
  displayName: 'Stage govuk-frontend dist → wwwroot/govuk'
```

### Pipeline position

```
checkout
  └─ NodeTool (install Node.js)
  └─ npm ci  (restore govuk-frontend)
  └─ Stage govuk-frontend dist → wwwroot/govuk    ← these three steps
  └─ dotnet restore
  └─ dotnet build
  └─ dotnet publish                               ← wwwroot/govuk is included here
  └─ Deploy to Azure App Service
```

`dotnet publish` copies the entire `wwwroot/` tree into the publish output, so the staged assets are automatically included in the deployment package without any additional configuration.

---

## 6. Upgrade Procedure

### To upgrade govuk-frontend to a new version

1. Check the [GOV.UK Frontend changelog](https://github.com/alphagov/govuk-frontend/blob/main/CHANGELOG.md) for breaking changes.
2. Edit `src/Histo.Web/package.json` — change the version number:

   ```diff
   -  "govuk-frontend": "6.4.0"
   +  "govuk-frontend": "6.5.0"
   ```

3. Commit with message: `chore: upgrade govuk-frontend to 6.5.0`
4. The pipeline handles the rest — no manual file downloads, no binary changes in git.

### Breaking vs feature releases

| Release type | Example | Action needed in Razor Pages |
|---|---|---|
| **Patch** `6.x.x` | Bug/accessibility fix | Update version — no code changes |
| **Feature** `6.x.0` | New components, new options | Update version — review changelog for recommended changes; Razor HTML unchanged unless using new components |
| **Major** `7.0.0` | Breaking changes | Read migration guide — HTML structure or CSS class names may change; audit all `.cshtml` pages |

For **Razor Pages** specifically, breaking changes typically affect:
- HTML element structure (e.g., the `<header>`/`<footer>` restructure in v6.0.0)
- Removed CSS class names (e.g., `govuk-body-xs` removed in v6.0.0)
- Renamed component classes (e.g., `govuk-tag--pink` → `govuk-tag--magenta` in v6.0.0)

None of the JavaScript or Nunjucks macro changes affect Razor Pages because macros are not used.

---

## 7. Local Development

Node.js is **not required** to run the application locally if the `wwwroot/govuk/` files already exist on disk from a previous install. Git ignores them but they remain functional.

If setting up a fresh clone or updating locally:

1. Install [Node.js LTS](https://nodejs.org/) (v20.x or later)
2. Run:

   ```powershell
   cd src/Histo.Web
   npm install
   ```

3. Run the PowerShell copy block from Section 5 locally, or simply use `dotnet run` — in development the app serves the existing `wwwroot/govuk/` files directly.

> On machines without Node.js (e.g., this workstation), the govuk-frontend files can be updated manually by downloading the tarball from `https://registry.npmjs.org/govuk-frontend/-/govuk-frontend-{version}.tgz`, extracting, and copying `dist/govuk/govuk-frontend.min.*` and `dist/govuk/assets/` into `wwwroot/govuk/`.

---

## 8. Version History

| Date | Version | Notes |
|---|---|---|
| 2026-07-31 | 6.2.0 | Initial version after GDS compliance sweep (Runs #45–#48) |
| 2026-08-04 | 6.4.0 | Upgraded manually; Interruption panel + Date input shorthand options added |

---

## 9. Alternative Approaches Considered and Rejected

| Approach | Reason rejected |
|---|---|
| **Commit static files to git** | Binary git bloat, no version audit, manual error-prone upgrades |
| **LibMan** | Awkward path mapping for govuk-frontend's `dist/govuk/` structure; niche tooling with poor IDE support in .NET 10 |
| **CDN reference** (`cdn.jsdelivr.net`) | GDS guidance explicitly requires self-hosting assets — user data must not be exposed to third-party CDN providers |
| **Sass compilation in pipeline** | Unnecessary — Razor Pages has no Sass source files; pre-built CSS is identical output with zero additional benefit |
| **MSBuild `Exec` target** | Requires Node.js installed on every developer machine and the build agent; breaks offline/restricted builds; couples asset management to the .NET build |

---

## 10. References

- [GOV.UK Design System — Staying up to date](https://frontend.design-system.service.gov.uk/staying-up-to-date/)
- [GOV.UK Frontend changelog](https://github.com/alphagov/govuk-frontend/blob/main/CHANGELOG.md)
- [GOV.UK Frontend npm package](https://www.npmjs.com/package/govuk-frontend)
- [Azure Pipelines NodeTool task](https://learn.microsoft.com/en-us/azure/devops/pipelines/tasks/reference/node-tool-v0)
- [ASP.NET Core static files](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/static-files)
