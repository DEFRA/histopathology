# Razor PDF Layout Guide
 
Reference for the `pdf-infrastructure` agent. Covers the base `_PdfLayout.cshtml`
implementation using CSS print media queries to replicate features previously
handled by programmatic PDF libraries.
 
---
 
## Base Layout Structure
 
```html
@model PdfLayoutModel
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>@Model.ReportTitle</title>
    <style>
        /* ── Print media setup ────────────────────────────────────────── */
        @@media print {
            @@page {
                size: A4 portrait;
                margin: 15mm 15mm 20mm 15mm;
 
                @@top-center {
                    content: "@Model.ReportTitle";
                    font-family: Arial, Helvetica, sans-serif;
                    font-size: 9pt;
                    color: #555;
                }
 
                @@bottom-center {
                    content: "Page " counter(page) " of " counter(pages);
                    font-family: Arial, Helvetica, sans-serif;
                    font-size: 9pt;
                    color: #555;
                }
            }
 
            /* Named page for landscape sections */
            @@page landscape {
                size: A4 landscape;
            }
 
            .landscape-section {
                page: landscape;
            }
 
            /* Page break helpers */
            .page-break-before { break-before: page; }
            .page-break-avoid  { break-inside: avoid; }
        }
 
        /* ── DRAFT watermark ──────────────────────────────────────────── */
        .draft-watermark::before {
            content: "DRAFT";
            position: fixed;
            top: 50%;
            left: 50%;
            transform: translate(-50%, -50%) rotate(-45deg);
            font-size: 120pt;
            font-family: Arial, Helvetica, sans-serif;
            font-weight: bold;
            color: rgba(180, 0, 0, 0.12);
            z-index: -1;
            pointer-events: none;
        }
 
        /* ── Logo header ──────────────────────────────────────────────── */
        .pdf-logo-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 1px solid #ccc;
            padding-bottom: 8px;
            margin-bottom: 16px;
        }
 
        .pdf-logo-header img {
            height: 50px;
            width: auto;
        }
 
        /* ── Report title block ───────────────────────────────────────── */
        .pdf-report-title {
            text-align: center;
            margin-bottom: 24px;
        }
 
        .pdf-report-title h1 {
            font-size: 16pt;
            font-family: Arial, Helvetica, sans-serif;
        }
 
        .pdf-report-title .profile-name {
            font-size: 12pt;
            color: #333;
        }
 
        /* ── PDF body ─────────────────────────────────────────────────── */
        .pdf-body {
            font-family: Arial, Helvetica, sans-serif;
            font-size: 10pt;
            color: #000;
            line-height: 1.4;
        }
 
        /* ── Tables ───────────────────────────────────────────────────── */
        .pdf-table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 12px;
        }
 
        .pdf-table th,
        .pdf-table td {
            border: 1px solid #ccc;
            padding: 4px 8px;
            vertical-align: top;
            font-size: 9pt;
        }
 
        .pdf-table th {
            background-color: #f0f0f0;
            font-weight: bold;
        }
 
        /* ── Diff highlighting (for comparison reports) ───────────────── */
        .diff-changed {
            background-color: #fff3cd;
        }
 
        .diff-added {
            background-color: #d4edda;
        }
 
        .diff-removed {
            background-color: #f8d7da;
        }
    </style>
</head>
<body class="pdf-body @(Model.IsDraft ? "draft-watermark" : "")">
 
    <!-- Logo header -->
    <div class="pdf-logo-header">
        <img src="~/images/logo-primary.png" alt="primary logo" />
        @if (Model.LogoVariant == PdfLogoVariant.PrimaryAndSecondary)
        {
            <img src="~/images/logo-secondary.png" alt="secondary logo" />
        }
    </div>
 
    <!-- Report title -->
    <div class="pdf-report-title">
        <h1>@Html.Raw(Model.ReportTitle)</h1>
        @if (!string.IsNullOrEmpty(Model.ProfileName))
        {
            <div class="profile-name">@Html.Raw(Model.ProfileName)</div>
        }
    </div>
 
    <!-- Report body -->
    @RenderBody()
 
</body>
</html>
```
 
---
 
## `PdfLayoutModel`
 
```csharp
public enum PdfLogoVariant { PrimaryOnly, PrimaryAndSecondary }
 
public class PdfLayoutModel
{
    /// <summary>The report type title, e.g. "Full profile".</summary>
    public string ReportTitle { get; set; } = string.Empty;
 
    /// <summary>The profile name/title. May contain HTML — rendered via Html.Raw.</summary>
    public string? ProfileName { get; set; }
 
    /// <summary>
    /// When true, the DRAFT watermark CSS class is applied to the body.
    /// Set from the profile version status.
    /// </summary>
    public bool IsDraft { get; set; }
 
    /// <summary>Which logo combination to show in the header.</summary>
    public PdfLogoVariant LogoVariant { get; set; } = PdfLogoVariant.PrimaryAndSecondary;
}
```
 
All report-specific view models must inherit from `PdfLayoutModel`:
 
```csharp
public class FullProfileViewModel : PdfLayoutModel
{
    // Report-specific properties
}
```
 
---
 
## CSS Print Feature Reference
 
### Page Numbers
 
CSS `counter(page)` and `counter(pages)` are supported natively by Chromium's
print rendering engine. They do not require JavaScript.
 
```css
@@page {
    @@bottom-center {
        content: "Page " counter(page) " of " counter(pages);
    }
}
```
 
This replaces TallPDF's `Fragment.HasContextFields = True` with `"#p of #P"`.
 
### DRAFT Watermark
 
The `::before` pseudo-element on `.draft-watermark` is applied only when
`Model.IsDraft = true`. The watermark is purely CSS — no image resource needed.
 
This replaces TallPDF's `ForegroundAreas` + `Drawing.TextShape` rotated text.
 
### Portrait and Landscape in the Same Document
 
Use CSS named pages. The `_PdfLayout.cshtml` defines `@page landscape`; apply
it to the landscape section by adding `class="landscape-section"`:
 
```html
<!-- Portrait sections -->
<div>
    <h2>Front page content</h2>
</div>
 
<!-- Landscape section (e.g. ranking table) -->
<div class="landscape-section page-break-before">
    <table class="pdf-table">...</table>
</div>
```
 
This replaces TallPDF's `SectionOrientation.Landscape` property on a `Section`.
 
### Page Breaks
 
```html
<!-- Force a new page before this element -->
<div class="page-break-before">
 
<!-- Prevent a table row or block from splitting across pages -->
<tr class="page-break-avoid">
```
 
### Table of Contents
 
Chromium cannot inject page numbers into the DOM after rendering — there is no
equivalent to TallPDF's `CrossreferenceSection`. Use anchor links only:
 
```html
<!-- TOC (no page numbers — anchor links only) -->
<nav class="toc">
    <h2>Contents</h2>
    <ol>
        <li><a href="#section-1">Section 1: Disease overview</a></li>
        <li><a href="#section-2">Section 2: Epidemiology</a></li>
    </ol>
</nav>
 
<!-- Section heading with matching anchor -->
<h2 id="section-1" class="page-break-before">Section 1: Disease overview</h2>
```
 
In-PDF anchor links are honoured by Chromium — clicking a TOC entry in the
rendered PDF navigates to the section.
 
**Decision required (PDF-01):** Confirm with the project team whether TOC without
page numbers is acceptable, or whether a two-pass rendering approach is needed.
If two-pass is required, see the two-pass TOC note below.
 
#### Two-Pass TOC (advanced, optional)
 
If page numbers in the TOC are required:
1. Render the document once without TOC to a temp PDF
2. Use a PDF manipulation library (PdfPig, iText 7 read-only) to count actual
   page numbers for each anchor
3. Inject the page numbers into the view model
4. Re-render the full document with the TOC populated
 
This approach is complex and adds latency. Only implement if explicitly required.
 
### Checkboxes
 
Replace bitmap checkbox images (`CheckboxSelected.png`, `CheckboxUnselected.png`)
with Unicode characters or inline SVG:
 
```html
<!-- Unicode approach -->
@(question.IsSelected ? "&#9745;" : "&#9744;")
 
<!-- SVG approach (more reliable cross-platform rendering) -->
@if (question.IsSelected)
{
    <svg width="14" height="14" viewBox="0 0 14 14" xmlns="http://www.w3.org/2000/svg">
        <rect x="1" y="1" width="12" height="12" rx="1" fill="none" stroke="#000" stroke-width="1.5"/>
        <polyline points="3,7 6,10 11,4" fill="none" stroke="#000" stroke-width="1.5"/>
    </svg>
}
else
{
    <svg width="14" height="14" viewBox="0 0 14 14" xmlns="http://www.w3.org/2000/svg">
        <rect x="1" y="1" width="12" height="12" rx="1" fill="none" stroke="#000" stroke-width="1.5"/>
    </svg>
}
```
 
### Logo Images
 
Replace embedded GDI+ `Bitmap` resources with static web assets:
 
1. Copy the logo image files (found by inspecting the existing PDF base class
   embedded resources) to `wwwroot/images/`
2. Reference via `<img src="~/images/logo-primary.png">` (and optionally
   `logo-secondary.png`) in `_PdfLayout.cshtml` — rename to match your actual
   filenames
3. Ensure `PrintBackground: true` is set in `PdfOptions` so the image is
   included in the PDF (Playwright does not print images by default without this)
 
---
 
## Font Matching
 
To minimise font-hinting differences between GDI+ (TallPDF) and Chromium:
 
```css
body, .pdf-body {
    font-family: Arial, Helvetica, sans-serif;
    font-size: 10pt;
    -webkit-print-color-adjust: exact;
    print-color-adjust: exact;
}
```
 
`-webkit-print-color-adjust: exact` and `print-color-adjust: exact` force
Chromium to honour background colours in print mode (required for coloured table
cells, diff highlighting, and the logo background).