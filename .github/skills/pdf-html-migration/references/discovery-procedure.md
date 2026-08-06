# Discovery Procedure
 
Detailed step-by-step scanning procedure for the `pdf-discovery` agent.
Applied to any .NET codebase regardless of which PDF library is in use.
 
---
 
## Step 1 — Library Fingerprinting
 
Search all `.csproj`, `packages.config`, `*.nuspec`, and `app.config` / `web.config`
files. Match the following patterns (case-insensitive):
 
| Search pattern | Library identified | Windows-only? |
|---|---|---|
| `TallComponents` | TallPDF.NET | Yes |
| `iTextSharp` | iText 5 | No |
| `iText7` / `iText.Kernel` | iText 7 | No |
| `PdfSharp` | PdfSharp | No |
| `Syncfusion.Pdf` | Syncfusion PDF | No |
| `FastReport` | FastReport | No |
| `Aspose.PDF` | Aspose.PDF | No |
| `CrystalDecisions` | Crystal Reports | Yes |
| `Microsoft.Reporting` | RDLC / SSRS | No |
| `QuestPDF` | QuestPDF | No |
| `SelectPdf` | SelectPdf | No |
| `wkhtmltopdf` | wkhtmltopdf | Conditional |
 
For each match, also record:
- **DLL path**: Is it a local file reference (checked-in to `lib/` or similar)
  or a NuGet package reference?
- **Version**: Extract from `<PackageReference Version="...">` or
  `<HintPath>` filename
- **Licence key locations**: Search for the library's licence key by looking for
  string patterns associated with the library in config files, transforms, and
  code-behind files (e.g. `"licenseKey"`, `"RegisterLicenseCode"`, key-length
  strings matching known patterns)
 
### Windows-Only Assessment
 
A library is Windows-only if any of the following apply:
- Depends on `System.Drawing` / `System.Web.DataVisualization` GDI+
- Vendor documentation states Linux is unsupported
- The DLL is a native Windows binary (check for PE header: first 2 bytes `MZ`)
 
Flag Windows-only libraries as a **hard blocker** for Linux container deployment.
 
---
 
## Step 2 — Report Class Discovery
 
### Primary Search
 
Search all `.cs` and `.vb` source files for class declarations that:
 
1. Contain `Inherits` / `: ` followed by a class name ending in `Pdf`, `Report`,
   `Document`, or `Base`
2. Import the detected PDF library namespace (e.g. `Imports TallComponents`,
   `using iText.Kernel`)
 
For each candidate class, record:
 
| Field | How to extract |
|---|---|
| `filePath` | Relative path from repo root |
| `className` | Class declaration name |
| `baseClassName` | `Inherits X` / `: X` clause |
| `linesOfCode` | Count non-blank, non-comment lines |
| `constructorParams` | Parameter names and types of all constructors |
| `isDataDriven` | Does the body method loop over a collection? |
| `hasHardcodedMapping` | Does the body method call a base method with literal integers? |
 
### Hardcoded Mapping Detection
 
Search within each report file for patterns like:
- `(1, 1)` / `(6, 14)` — integer pair arguments to a base class method
- `BuildNonTechnicalTitledQuestionTable(` / `GetQuestion(section, question)` /
  equivalent patterns for other libraries
 
If found, set `hasHardcodedMapping: true` and record the method name and
all argument pairs found.
 
---
 
## Step 3 — Entry Point Mapping
 
For each report class, search all source files for instantiation patterns:
 
**VB.NET**: `New <ClassName>(` or `<ClassName>.Create(`
**C#**: `new <ClassName>(` or `<ClassName>.Create(`
 
Trace the containing method back to the web layer:
- **WebForms**: ASPX code-behind `Page_Load` or button-click handler
- **MVC**: Controller action method
- **Razor Pages**: PageModel `OnGet` / `OnPost`
- **API**: Controller action returning `IActionResult`
 
Record:
- Entry point file path
- HTTP route / URL pattern
- Query string parameters consumed
- How PDF bytes are returned (e.g. `Response.BinaryWrite`,
  `Response.DownloadPdfBytes`, `File(bytes, "application/pdf")`)
 
---
 
## Step 4 — Chart Dependency Detection
 
Within each report class file and its imports, search for:
 
| Pattern | Library | Action required |
|---|---|---|
| `System.Web.DataVisualization.Charting` | GDI+ chart | Replace with Chart.js |
| `OxyPlot` | OxyPlot | Replace with Chart.js |
| `LiveCharts` | LiveCharts | Replace with Chart.js |
| `SkiaSharp` | SkiaSharp | Evaluate: may be retained server-side |
| `Chart chart = new Chart()` | Generic | Confirm library from imports |
 
Any file with a chart dependency: set complexity to **Very High** regardless of
line count.
 
---
 
## Step 5 — Base Class Analysis
 
Identify abstract / `MustInherit` base classes in the report class hierarchy.
For each base class:
 
1. Read its full source file
2. Identify which cross-cutting features it provides:
 
| Feature | What to look for in code |
|---|---|
| Page header | Method named `CreateSectionHeader` or `BuildHeader` |
| Page footer / page numbers | Method named `CreateSectionFooter`; string `"#p of #P"` or `counter(page)` |
| DRAFT watermark | `ForegroundAreas`, `Drawing.TextShape`, string `"DRAFT"` |
| Table of contents | `CrossreferenceSection`, `ComposeEntry`, `TableOfContents` |
| Portrait/landscape switching | `SectionOrientation` enum, `Landscape` property |
| Embedded image resources | `GetType().Assembly.GetManifestResourceStream` |
| Document metadata | `DocumentInfo.Title`, `DocumentInfo.Subject` |
 
3. Record each feature as a requirement for `_PdfLayout.cshtml`
 
---
 
## Step 6 — Shared Block Discovery
 
Search for classes that:
- Are referenced by more than one report class
- Are in a folder named `Blocks`, `PdfBlocks`, `Helpers`, `Extensions`, or similar
- Inherit from a table, paragraph, or row base class from the PDF library
 
For each shared block, record:
- Class name and file path
- Which reports reference it
- What rendering responsibility it holds (e.g. `ContributionsTable` — renders
  contributor rows; `LogoHeaderTable` — renders logo header)
 
These become candidates for shared Razor partial views named
`_<BlockName>.cshtml`.
 
---
 
## Step 7 — Complexity Scoring
 
Apply the scoring table mechanically — do not apply subjective judgement:
 
| Tier | Primary criteria | Secondary criteria |
|---|---|---|
| **Low** | < 150 own lines | No chart, no hardcoded mapping, thin subclass |
| **Medium** | 150–300 own lines | Data-driven iteration, moderate shared block usage |
| **High** | 300–600 own lines | Hardcoded mapping, or coupling to sibling report's mapping |
| **Very High** | > 600 own lines, OR chart dependency | Multiple filter variants, or chart + two Windows libraries |
 
Where a report meets criteria for multiple tiers, assign the higher tier.
 
---
 
## Step 8 — `test/pdf-validation-profiles.json` Prompt
 
During discovery, identify what test data scenarios are needed:
- Is there a scenario requiring a draft profile? → needs `Status = 'Draft'` record
- Is there a comparison report? → needs two versions of the same profile
- Is there a bespoke report? → needs a `BespokeReportTemplate` record
- Is there a ranking report? → needs `SpeciesCategory` data
 
Record these as `"requiredScenarios"` in `report-inventory.json`. The
reference generator agent uses this list when querying the database for IDs.
 
---
 
## Output Schema
 
Write `report-inventory.json` using
`.github/Skills/pdf-html-migration/assets/report-inventory.json.template`.
 
Every field in the template must be populated. Use `null` only where a field
is genuinely not applicable (e.g. `chartLibrary: null` for a report with no
chart dependency). Do not omit fields.
 