---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: "fix-gds-layout"
description: "Reusable GDS layout skill — audit and fix GOV.UK Design System assets, header, footer, and body structure for any web framework. Auto-detects the shared layout file (ASP.NET Core MVC, Razor Pages, or any custom path). Invoke as part of a full GDS compliance pass or standalone for a specific layout issue. Use when: layout is not styled; header looks wrong; footer missing or unstyled; GOV.UK crown logo not showing; service name or navigation in wrong place; fonts not loading; govuk-frontend CSS or JS assets missing; page template does not match GDS structure."
tools: [read, edit, search, fetch]
argument-hint: "Optional: describe the specific layout issue (e.g. 'header not showing', 'fonts missing'). If omitted, runs the full audit and fix pipeline."
---
 
# Fix GOV.UK Layout — Header, Footer & Body
 
Reusable skill for auditing and fixing the GOV.UK Design System page template. Targets **govuk-frontend v6.x**. Auto-detects the host framework and layout file location — runs identically whether invoked by the `gds-ui` agent as part of a full compliance pass or called directly for a specific layout issue.
 
## Step 0 — Detect Framework & Layout File
 
Use the `search` tool with the glob pattern `**/Shared/_Layout.cshtml` to locate the shared layout file.
 
| Framework | Expected path |
|---|---|
| ASP.NET Core Razor Pages | `Pages/Shared/_Layout.cshtml` |
| Custom | any path returned by the search |
 
Record the discovered path as `layoutFile`. If multiple layout files are found, apply Steps 2 and 3 to each. If no layout file is found, skip Steps 2 and 3 and report the absence to the caller.
 
## Step 1 — Audit Assets
 
### 1a — Check existence with the search tool
 
Use the `search` tool with each glob below. Mark each file as **present**, **missing**, or **stub** (see 1b).
 
| Glob | Min real size | Purpose |
|---|---|---|
| `wwwroot/govuk/govuk-frontend.min.css` | >100 KB | All GDS styles |
| `wwwroot/govuk/govuk-frontend.min.js` | >40 KB | Component JS |
| `wwwroot/assets/fonts/light-*.woff2` | >30 KB | GDS Transport font |
| `wwwroot/assets/fonts/bold-*.woff2` | >28 KB | GDS Transport font bold |
| `wwwroot/assets/images/favicon.svg` | >1 KB | GOV.UK favicon (SVG) |
| `wwwroot/assets/images/govuk-crest.svg` | >30 KB | **Crown copyright logo** (CSS background-image) |
| `wwwroot/assets/images/govuk-icon-mask.svg` | >1 KB | Safari pinned-tab mask |
 
> **Critical:** `govuk-crest.svg` is loaded via CSS `background-image` — the browser shows no error when it is missing, the crown logo simply disappears silently.
 
### 1b — Detect stub files with the read tool
 
For `govuk-frontend.min.css` and `govuk-frontend.min.js`, use the `read` tool to read the first 3 lines.
 
- **Real file**: first line starts with minified CSS (`.govuk-`) or minified JS (`/*! govuk-frontend`).
- **Stub / error file**: first line is empty, contains an HTTP error message, or is fewer than 20 characters. Treat as missing.
 
### 1c — Fetch missing text assets with the fetch + edit tools
 
For each **missing or stub** text asset, use the `fetch` tool to retrieve the content from the unpkg CDN, then use the `edit` tool to write it to the target path.
 
| Asset | Fetch URL | Write to |
|---|---|---|
| CSS | `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/govuk-frontend.min.css` | `wwwroot/govuk/govuk-frontend.min.css` |
| JS | `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/govuk-frontend.min.js` | `wwwroot/govuk/govuk-frontend.min.js` |
| favicon.svg | `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/favicon.svg` | `wwwroot/assets/images/favicon.svg` |
| govuk-crest.svg | `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/govuk-crest.svg` | `wwwroot/assets/images/govuk-crest.svg` |
| govuk-icon-mask.svg | `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/govuk-icon-mask.svg` | `wwwroot/assets/images/govuk-icon-mask.svg` |
 
> **Binary assets** (`.woff2`, `.woff`, `.png`, `.ico`): these cannot be written by the `edit` tool. If they are missing, report them to the user with the exact unpkg URLs below and ask them to download manually or run `npm install govuk-frontend@6.2.0` and copy from `node_modules/govuk-frontend/dist/govuk/assets/`.
>
> Binary asset URLs (govuk-frontend v6.2.0):
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/fonts/bold-affa96571d-v2.woff`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/fonts/bold-b542beb274-v2.woff2`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/fonts/light-94a07e06a1-v2.woff2`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/fonts/light-f591b13f7d-v2.woff`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/favicon.ico`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/govuk-icon-180.png`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/govuk-icon-192.png`
> - `https://unpkg.com/govuk-frontend@6.2.0/dist/govuk/assets/images/govuk-icon-512.png`
 
## Step 2 — Audit Layout File
 
Read the layout file identified in Step 0 (`$layoutFile`) and check for these issues:
 
| Check | Wrong (old) | Correct (v6) |
|---|---|---|
| Body JS detection | Missing | `<script>document.body.className += ' js-enabled' + (...)</script>` immediately after `<body>` |
| Skip link | No `data-module` | `<a class="govuk-skip-link" data-module="govuk-skip-link">` |
| Header wrapper | `<header class="govuk-header">` | `<header class="govuk-template__header">` |
| Crown logo | Text `GOV.UK` or old SVG | Tudor Crown SVG (inline, `viewBox="0 0 324 60"`) |
| Service name | Inside `govuk-header__content` | Inside `govuk-service-navigation__service-name` |
| Navigation | `govuk-header__navigation-list` | `govuk-service-navigation__list` with `data-module="govuk-service-navigation"` |
| Footer wrapper | `<footer class="govuk-footer">` | `<footer class="govuk-template__footer"><div class="govuk-footer">` |
| Footer Crown SVG (inline) | Missing | Tudor Crown SVG (crown only, `viewBox="0 0 64 60"`) at top of `govuk-footer` |
| Footer Crown copyright image | `govuk-crest.svg` not in `wwwroot/assets/images/` | Download `govuk-crest.svg` — the CSS `govuk-footer__copyright-logo::before` uses it as `background-image`; it is **silent when missing** |
| Footer support links | No `govuk-visually-hidden` heading | `<h2 class="govuk-visually-hidden">Support links</h2>` before `ul` |
| JS initialisation | Missing | `<script type="module">import { initAll } from '/govuk/govuk-frontend.min.js'; initAll()</script>` |
| Viewport meta | `width=device-width, initial-scale=1.0` | `width=device-width, initial-scale=1, viewport-fit=cover` |
| Theme colour | Missing | `<meta name="theme-color" content="#1d70b8">` |

## Step 2a — Approval Checkpoint (Required Before Any Write)

Do not proceed to Step 3 until this checkpoint is complete. Agents that write to a system
must keep a human approval step before doing so — this skill must not overwrite `$layoutFile`
or fetch/write asset files without an explicit human go-ahead.

1. **Present the audit findings** from Step 1 (asset status) and Step 2 (layout issues) as a
   single summary — do not fetch, write, or edit anything yet.
2. **List every planned write action** before performing it, for example:
   - `Fetch and write: wwwroot/govuk/govuk-frontend.min.css (stub detected, 80 bytes)`
   - `Fetch and write: wwwroot/assets/images/govuk-crest.svg (missing)`
   - `Rewrite: Pages/Shared/_Layout.cshtml — header, footer, and body structure (Step 3)`
3. **Ask the user to confirm** before any of the above actions run. Use a direct yes/no
   question such as: *"I found N issues and plan to make the changes listed above. Should I
   proceed?"*
4. **If the user declines or requests changes to scope**, do not write any files. Revise the
   plan and re-confirm, or stop and report the audit findings only.
5. **If the user confirms**, proceed to Step 1c (asset fetch/write) and Step 3 (layout rewrite)
   exactly as approved. Do not silently expand scope beyond what was confirmed — if additional
   issues are discovered mid-fix, pause and get separate approval before addressing them.

> **Exception:** If this skill is invoked with an explicit standing instruction from the user
> in the same conversation turn (e.g. "fix all GDS layout issues without asking") the checkpoint
> may be skipped for that invocation only. Do not treat approval from a previous, unrelated
> conversation as standing consent.

## Step 3 — Apply the Correct v6 Layout Structure

Only after the Step 2a approval checkpoint has been confirmed: update `$layoutFile` with the
correct govuk-frontend v6 structure. The Razor directives (`@RenderBody()`, `@RenderSection()`,
`@ViewData`) are identical for both MVC and Razor Pages:

### `<head>` section
```html
<meta charset="utf-8" />
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover" />
<meta name="theme-color" content="#1d70b8" />
<title><!-- page title --></title>
<link rel="stylesheet" href="~/govuk/govuk-frontend.min.css" asp-append-version="true" />
```
 
### `<body>` opening
```html
<body class="govuk-template__body">
<script>document.body.className += ' js-enabled' + ('noModule' in HTMLScriptElement.prototype ? ' govuk-frontend-supported' : '');</script>
<a href="#main-content" class="govuk-skip-link" data-module="govuk-skip-link">Skip to main content</a>
```
 
### Header structure
```html
<header class="govuk-template__header">
  <!-- 1. GOV.UK header with Tudor Crown SVG (logo only) -->
  <div class="govuk-header">
    <div class="govuk-header__container govuk-width-container">
      <div class="govuk-header__logo">
        <a href="/" class="govuk-header__homepage-link">
          <svg focusable="false" role="img" class="govuk-header__logotype"
               xmlns="http://www.w3.org/2000/svg"
               viewBox="0 0 324 60" height="30" width="162"
               fill="currentcolor" aria-label="GOV.UK">
            <title>GOV.UK</title>
            <!-- Tudor Crown circles and paths + GOV.UK wordmark paths -->
          </svg>
        </a>
      </div>
    </div>
  </div>
  <!-- 2. Service navigation (service name + nav links) -->
  <section class="govuk-service-navigation"
           data-module="govuk-service-navigation"
           aria-label="Service information">
    <div class="govuk-width-container">
      <div class="govuk-service-navigation__container">
        <span class="govuk-service-navigation__service-name">
          <a href="/" class="govuk-service-navigation__link">Your service name</a>
        </span>
        <nav aria-label="Menu" class="govuk-service-navigation__wrapper">
          <button type="button"
                  class="govuk-service-navigation__toggle govuk-js-service-navigation-toggle"
                  aria-controls="navigation" hidden>Menu</button>
          <ul class="govuk-service-navigation__list" id="navigation">
            <li class="govuk-service-navigation__item">
              <a class="govuk-service-navigation__link" href="/page">Nav item</a>
            </li>
          </ul>
        </nav>
      </div>
    </div>
  </section>
</header>
```
 
### Main content
```html
<div class="govuk-width-container">
  <!-- optional: govuk-phase-banner here -->
  <main class="govuk-main-wrapper" id="main-content" role="main">
    @RenderBody()
  </main>
</div>
```
 
### Footer structure
```html
<footer class="govuk-template__footer">
  <div class="govuk-footer">
    <div class="govuk-width-container">
      <!-- Tudor Crown SVG (crown only, no wordmark) -->
      <svg aria-hidden="true" focusable="false" role="presentation"
           class="govuk-footer__crown"
           xmlns="http://www.w3.org/2000/svg"
           viewBox="0 0 64 60" height="30" width="32" fill="currentcolor">
        <!-- crown paths only -->
      </svg>
      <div class="govuk-footer__meta">
        <div class="govuk-footer__meta-item govuk-footer__meta-item--grow">
          <h2 class="govuk-visually-hidden">Support links</h2>
          <ul class="govuk-footer__inline-list">
            <li class="govuk-footer__inline-list-item">
              <a class="govuk-footer__link" href="#">Accessibility statement</a>
            </li>
          </ul>
          <!-- OGL licence SVG + text -->
        </div>
        <div class="govuk-footer__meta-item">
          <a class="govuk-footer__link govuk-footer__copyright-logo" href="...">© Crown copyright</a>
        </div>
      </div>
    </div>
  </div>
</footer>
```
 
### JS initialisation (before `</body>`)
```html
<script type="module" src="/govuk/govuk-frontend.min.js"></script>
<script type="module">
  import { initAll } from '/govuk/govuk-frontend.min.js'
  initAll()
</script>
```
 
## Step 4 — Verify Build
 
Use the `search` tool to confirm the project file exists (`**/*.csproj`), then ask the user to run `dotnet build` and confirm the output shows `Build succeeded. 0 Error(s)`. Report any compiler errors back for resolution before declaring the layout fix complete.
 
## Common Issues
 
| Symptom | Cause | Fix |
|---|---|---|
| Page unstyled / plain HTML | CSS stub file (80 bytes) | Re-download CSS from unpkg |
| Wrong font (Arial/system font) | Fonts not in `wwwroot/assets/fonts/` | Download 4 font files |
| Crown logo not showing in header | Old text logo or wrong SVG | Use Tudor Crown inline SVG (v6) |
| **Crown copyright logo missing in footer** | **`govuk-crest.svg` absent from `wwwroot/assets/images/`** | **Fetch `govuk-crest.svg` from unpkg using the fetch + edit tools (see Step 1c); it is loaded as a CSS `background-image` — no browser error is shown when missing** |
| Nav links not visible on mobile | Missing `govuk-js-service-navigation-toggle` button | Add toggle button with `hidden` attribute |
| Components not interactive | No JS or `initAll()` not called | Add module script + `initAll()` |
| "Error: " not in page title | `ViewData["HasErrors"]` not set | Set `ViewData["HasErrors"] = true` in pages with errors |

---

## Standards

This skill is loaded by the [gds-ui agent](./../../agents/gds-ui.agent.md). All layout rewrites require a human approval checkpoint (Step 2a) before any files are written. All outputs are subject to AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/) and [Defra SDS — Common coding standards](https://defra.github.io/software-development-standards/standards/common_coding_standards/).