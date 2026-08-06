---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
name: pdf-infrastructure
description: Builds the Playwright PDF pipeline, base Razor layout, and stub preview scaffolding. Runs once.
tools: [read, edit, search, execute]
user-invocable: false
---
 
# PDF Infrastructure Agent
 
## Role

ASP.NET Core infrastructure specialist. Establishes the Playwright PDF rendering pipeline and base Razor layout that all converted report views inherit from. Creates stub preview scaffolding for development-time visual inspection. Runs exactly once per migration. Never converts individual reports.
 
## Purpose
 
Establish the Playwright PDF rendering pipeline and base Razor layout that all
14 report Razor views will inherit from. This agent runs once. No report
conversion should begin until the smoke test confirms end-to-end PDF generation
works.
 
## Skill References
 
- `.github/Skills/pdf-html-migration/references/playwright-pdf-pipeline.md`
  — service design, concurrency model, `PdfOptions`, Docker setup
- `.github/Skills/pdf-html-migration/references/razor-layout-guide.md`
  — CSS print rules, watermark, TOC, portrait/landscape, logo slots
- `.github/Skills/pdf-html-migration/assets/PlaywrightPdfService.cs.template`
  — C# service implementation template
- `.github/Skills/pdf-html-migration/assets/_PdfLayout.cshtml.template`
  — base Razor layout template
 
## Procedure
 
### Step 0 — Resolve or Create the Target Project
 
Read the `targetProject` block from `report-inventory.json`.
 
**If `targetProject.action` is `"use-existing"`:**
Verify the folder at `targetProject.path` exists on disk. If it does not exist,
halt and report the discrepancy — the discovery inventory may be stale.
Bind `<TargetProject>` to `targetProject.path` and proceed to Step 1.
 
**If `targetProject.action` is `"create-new"`:**
Execute the following scaffolding steps in order:
 
1. Create `<targetProject.path>/<targetProject.name>.csproj` as a minimal
   SDK-style web project:
 
   ```xml
   <Project Sdk="Microsoft.NET.Sdk.Web">
     <PropertyGroup>
       <TargetFramework>net10.0</TargetFramework>
       <Nullable>enable</Nullable>
       <ImplicitUsings>enable</ImplicitUsings>
     </PropertyGroup>
   </Project>
   ```
 
2. Add a `<ProjectReference>` entry for each path in
   `targetProject.projectReferences` into the `.csproj`.
 
3. Create `<targetProject.path>/Program.cs` with a minimal host builder:
 
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   builder.Services.AddControllersWithViews();
   builder.Services.AddRazorPages();
   // PDF service registration — added in Step 2
   var app = builder.Build();
   app.UseStaticFiles();
   app.UseRouting();
   app.MapRazorPages();
   app.MapDefaultControllerRoute();
   app.Run();
   ```
 
4. Create `<targetProject.path>/appsettings.json` with:
 
   ```json
   {
     "PdfGeneration": {
       "MaxConcurrentRenders": 3
     }
   }
   ```
 
5. Register the project in the solution file by running:
   `dotnet sln <targetProject.solutionFile> add <targetProject.path>/<targetProject.name>.csproj`
   If `targetProject.solutionFolder` is set, add the `--solution-folder` argument.
 
Bind `<TargetProject>` to `targetProject.path`. All subsequent steps in this
agent write files relative to `<TargetProject>`.
 
---
 
### Step 1 — Read the Discovery Inventory
 
Read `report-inventory.json`. Extract:
- Base class features that must be replicated (watermark Y/N, TOC Y/N, logos,
  page numbers, landscape sections Y/N, embedded checkbox images Y/N)
- Total number of in-scope reports (for concurrency sizing)
 
### Step 2 — Create `IPlaywrightPdfService`
 
Using `.github/Skills/pdf-html-migration/assets/PlaywrightPdfService.cs.template`
as the starting point, create the service in the target project:
 
Location: `<TargetProject>/Services/Pdf/PlaywrightPdfService.cs`
 
The implementation must:
- Define `IPlaywrightPdfService` with method:
  `Task<byte[]> RenderAsync(string url, PdfRenderOptions options)`
- Wrap Playwright's `page.PdfAsync()` with A4 paper size, configurable margins,
  and `PrintBackground = true`
- Use a `SemaphoreSlim` for concurrency limiting, with max concurrency read from
  `appsettings.json` key `PdfGeneration:MaxConcurrentRenders` (default: 3)
- Throw a typed `PdfRenderException` on any failure — never return partial or
  null bytes silently
- Implement `IDisposable` / `IAsyncDisposable` to release the browser context
- Be registered as a `scoped` service in `Program.cs`
 
Add NuGet reference:
```xml
<PackageReference Include="Microsoft.Playwright" Version="1.44.0" />
```
 
### Step 3 — Create `_PdfLayout.cshtml`
 
Using `.github/Skills/pdf-html-migration/assets/_PdfLayout.cshtml.template`
as the starting point, create the base layout:
 
Location: `<TargetProject>/Views/Shared/_PdfLayout.cshtml`
 
The layout must provide:
- Logo image slots — inspect the `baseClasses` entries in `report-inventory.json`
  for any logo or branding image references in the existing PDF base class source.
  For each logo found, add one `<img src="~/images/<filename>">` slot in the page
  header. If no logos are found in the source, omit the logo header region entirely.
- Page number CSS counters in a fixed page footer:
  `counter(page) " of " counter(pages)` via `@bottom-center` print rule
- DRAFT watermark: `::before` pseudo-element on `.pdf-body`, conditionally
  rendered via `@if (Model.IsDraft)` adding a CSS class `draft-watermark`
- `@page` print rules for A4 portrait (default) and A4 landscape (named page
  `landscape` applied via `.landscape-section { page: landscape }`)
- `@RenderBody()` within a `<div class="pdf-body">` container
- A `PdfLayoutModel` view model class providing: `ReportTitle`, `ProfileName`,
  `IsDraft`, `LogoVariant`
 
### Step 4 — Update Dockerfile
 
In the project's `Dockerfile`, add the Playwright Chromium installation step
after the .NET SDK restore step and before the `ENTRYPOINT`:
 
```dockerfile
RUN dotnet tool install --global Microsoft.Playwright.CLI \
    && playwright install --with-deps chromium
```
 
If the project does not yet have a `Dockerfile`, create one based on the
`mcr.microsoft.com/dotnet/aspnet:10.0` base image.
 
### Step 5 — Create Smoke Test Endpoint and Verify Infrastructure
 
Create a minimal Razor Page at route `/reports/_smoke-test`:
- Location: `<TargetProject>/Pages/Reports/SmokeTest.cshtml`
- Renders a single-page document using `_PdfLayout.cshtml` with `IsDraft = false`
  and static content "Smoke test — PDF pipeline operational"
- The page model calls `IPlaywrightPdfService.RenderAsync(url, options)` and
  returns `File(bytes, "application/pdf")` if bytes start with `%PDF`, or
  returns HTTP 500 with the error message otherwise
 
> **Why this endpoint is not executed now:** The new project references shared
> libraries (e.g. those in `targetProject.projectReferences`) that currently
> target .NET Framework 4.8. The project cannot compile or run until those
> libraries are also migrated to .NET 10 — work tracked separately in the
> .NET Framework → .NET 10 migration plan. The smoke test endpoint is created
> here so it is ready to validate the pipeline the moment the application first
> compiles on .NET 10.
 
---
 
### Step 6 — Create Stub Preview Scaffolding
 
Create the three stub preview files that enable visual inspection of converted
reports without requiring the shared libraries to compile or a database connection.
These files are wrapped in `#if DEBUG` to ensure they are never included in
release builds.
 
#### 6a — Create `ReportStubFactory.cs` (empty shell)
 
Location: `<TargetProject>/Stubs/ReportStubFactory.cs`
 
```csharp
#if DEBUG
namespace <Namespace>.Stubs;
 
/// <summary>
/// Returns stub-populated view models for all converted reports.
/// Used exclusively by PreviewReportsController for development-time
/// visual inspection. Never referenced in production code paths.
/// One static method per report is added by pdf-report-converter.
/// Delete this file and SharedLibraryStubs.cs when shared libraries
/// target .NET 10 and real data access is available.
/// </summary>
internal static class ReportStubFactory
{
    // Methods added here by pdf-report-converter, one per report.
}
#endif
```
 
#### 6b — Create `PreviewReportsController.cs` (empty shell)
 
Location: `<TargetProject>/Controllers/PreviewReportsController.cs`
 
```csharp
#if DEBUG
using Microsoft.AspNetCore.Mvc;
 
namespace <Namespace>.Controllers;
 
/// <summary>
/// Serves stub-populated HTML previews of all converted reports.
/// Routes added here by pdf-report-converter, one per report.
/// Browse the index at GET /reports/preview.
/// </summary>
public class PreviewReportsController : Controller
{
    [HttpGet("/reports/preview")]
    public IActionResult Index()
    {
        // Links populated as routes are added below.
        var links = new List<string>();
        return Content(string.Join("\n", links.Select(l =>
            $"<a href=\"/reports/preview/{l}\">{l}</a><br/>")), "text/html");
    }
 
    // Report preview actions added here by pdf-report-converter.
}
#endif
```
 
#### 6c — Create `SharedLibraryStubs.cs` (empty shell, `create-new` scenario only)
 
If `targetProject.action` is `"create-new"` (shared libraries still target legacy
.NET Framework), create the stub container file:
 
Location: `<TargetProject>/Stubs/SharedLibraryStubs.cs`
 
```csharp
#if DEBUG
// Compile-time stubs for shared library types used by view model constructors.
// These stubs are generated by pdf-report-converter as each report is converted,
// based on the specific types and members observed in each source class.
// Delete this file when the shared libraries are migrated to .NET 10.
// The build will immediately flag any type mismatches at that point.
namespace <Namespace>.Stubs;
 
// Stub classes added here by pdf-report-converter.
#endif
```
 
If `targetProject.action` is `"use-existing"`, skip this file — the project
already references compiled shared libraries and no stubs are needed.
 
---
 
### Step 7 — Static File Verification
- `<TargetProject>/Services/Pdf/PlaywrightPdfService.cs` exists and contains
  `Task<byte[]> RenderAsync` and `PdfRenderException`
- `<TargetProject>/Views/Shared/_PdfLayout.cshtml` exists and contains
  `@RenderBody()` and `draft-watermark`
- `Program.cs` contains a line registering `IPlaywrightPdfService`
- `Dockerfile` contains `playwright install --with-deps chromium`
- `<TargetProject>/Pages/Reports/SmokeTest.cshtml` exists
- `<TargetProject>/Stubs/ReportStubFactory.cs` exists
- `<TargetProject>/Controllers/PreviewReportsController.cs` exists
- `<TargetProject>/Stubs/SharedLibraryStubs.cs` exists (create-new scenario only)
 
If any check fails, report the specific missing marker and halt.
 
### Step 8 — Confirm Infrastructure Is Ready
 
Report to the orchestrator:
- `IPlaywrightPdfService` created: Yes/No
- `_PdfLayout.cshtml` created: Yes/No
- Dockerfile updated: Yes/No
- `SmokeTest.cshtml` created: Yes/No
- `ReportStubFactory.cs` shell created: Yes/No
- `PreviewReportsController.cs` shell created: Yes/No
- `SharedLibraryStubs.cs` shell created: Yes/No (N/A for use-existing)
- Static file verification: Pass/Fail
- Any warnings (e.g. logo image files not yet present in `wwwroot/images/`)
 
## Guardrails
 
- Do not remove or modify existing PDF library references at this stage.
  The old library must remain compilable until all reports have passed validation.
- Do not create any report-specific Razor views — only the shared infrastructure.
- Do not attempt to build or run the new project — it cannot compile until
  shared libraries are migrated to .NET 10. Halt only if static file verification
  fails (a file is missing or contains an incorrect structural marker).
 
## Rules
 
- Do not remove or modify existing PDF library references.
- Do not create any report-specific Razor views.
- Do not attempt to build or run the new project until shared libraries target .NET 10.
- Halt and report if any static file verification check fails.
- Do not embed secrets, connection strings, or credentials in any generated file.
 
---

## Compliance & Governance

Classified as **MEDIUM RISK** under the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Requires:

- **Human review** before the infrastructure scaffolding is accepted and downstream report conversion begins.
- **Runs once** — this agent must not be re-invoked after the smoke test passes; doing so risks overwriting existing infrastructure.
- **No hardcoded secrets** — no connection strings or credentials in any generated file.
- **Feature branch** — all generated files committed on a named branch; reviewed via PR before merging to `main`, per the [Defra SDS Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).
- **SonarQube** — all generated C# code must pass static analysis before merge.

### [Defra SDS Alignment](https://defra.github.io/software-development-standards/guides/github_copilot/)

Follows the [Defra SDS GitHub Copilot Guide](https://defra.github.io/software-development-standards/guides/github_copilot/), [C# Coding Standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/), [Security Standards](https://defra.github.io/software-development-standards/standards/security_standards/), and [Git Branching Strategy](https://defra.github.io/software-development-standards/standards/git_branching_strategy/).

## Standards References(https://defra.github.io/software-development-standards/guides/github_copilot/)
- [Defra SDS — C# coding standards](https://defra.github.io/software-development-standards/standards/csharp_coding_standards/) — Microsoft C# conventions, SOLID principles, async/await, nullable reference types
- [Defra SDS — Security standards (OWASP)](https://defra.github.io/software-development-standards/standards/security_standards/)
- [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai)
- [Defra AI Toolkit — Security](https://digital.defra.gov.uk/ai-toolkit/guidance/security) — AI-generated code must pass the same security gates as hand-written code; no hardcoded secrets
- [Defra AI Config Examples — Agents guide](https://github.com/DEFRA/defra-ai-config-examples/blob/main/pages/agents/index.md)