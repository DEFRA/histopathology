# HtmlToPdfConversionSkill

## Purpose
Generates PDF rendering logic using **free, open-source NuGet packages only**. No paid or commercial PDF runtime is permitted.

Supports two runtime paradigms (auto-detected from project file — no version pinning):
- `classic` → any .NET Framework (4.0–4.8) · VB.NET renderer (`{ReportName}Renderer.vb`) · `System.Web.HttpResponse` + `Server.MapPath`
- `modern` → any ASP.NET Core (.NET 5+) · C# renderer (`{ReportName}Renderer.cs`) · `IWebHostEnvironment` + constructor DI + `Microsoft.AspNetCore.Http.HttpResponse`

inputs:
- `{reportOutputFolder}/templates/{ReportName}.html`
- `runtimeParadigm` — `classic` or `modern` (passed from agent after auto-detection; never assume a specific version number)
- `callingPageContext` — discovered by agent in Pre-Stage-3 (business-layer method, DataSet variable, parameters)
- `srcFolder` — path to the migrated C# .NET 10 source folder (required for `modern` paradigm output placement)
- `reportOutputFolder` — root folder for pipeline outputs (default `output/`)
guardrails:
  - keep PR reviewable and reversible
  - use only free/open-source NuGet packages — no paid runtime

## Responsibilities
- Consider all files from the `{reportOutputFolder}/templates/` folder.
- Skip any report that already has a matching renderer output:
  - `classic`: skip if `{reportOutputFolder}/renderer/{ReportName}Renderer.vb` exists
  - `modern`: skip if `{srcFolder}/Reports/{ReportName}Renderer.cs` exists
  - **Do not skip** (`modern`) if only a `.vb` renderer exists — regenerate as a C# renderer using the existing template
- Produce renderer for HTML→PDF conversion in the language and API matching `runtimeParadigm`.
- Select PDF engine based on project requirements (see NuGet table below).
- Ensure Azure App Service compatibility for the target platform.

---

### Step 0 — Calling-page discovery (MANDATORY — completed by agent before invoking this skill)

The agent passes `callingPageContext` containing:
- The **business-layer method** that populates the DataSet (e.g. `clsQualityNote.CreateReportDataset(iQualityNoteRef, dsReportDataset)`)
- The **DataSet variable name** (e.g. `dsReportDataset`)
- The **parameter(s)** passed to that method

**This skill does not re-search for the calling page.** It consumes context passed by the agent.

Accept the pre-populated DataSet — renderer does token replacement + PDF conversion only. **Never duplicate data-loading in the renderer.**

If `callingPageContext` is absent (no calling page found in `srcFolder` — e.g. pre-migration pass):
- Generate a standalone renderer with a `// TODO: wire data source — re-run Stage 3 after migration completes` placeholder in the data-loading method.
- Log the renderer as `status: "stub"` in `{reportOutputFolder}/logs/failures.json` with `reason: "callingPageContext absent — srcFolder not yet migrated"`.
- **Do not** generate a renderer that silently loads data from an unknown source.

---

## Required NuGet Packages (free / open-source only)

| Package | Target | Notes |
|---|---|---|
| `DinkToPdf` | `classic` + `modern` | Primary. wkhtmltopdf wrapper. Requires native `libwkhtmltox` binary deployed with app. Supported on any .NET Framework version and any ASP.NET Core version. |
| `QuestPDF` Community Edition | `classic` + `modern` | Alternative. Pure managed; no native binary. Community licence free for open-source / small commercial use. Supported on any .NET Framework and .NET 5+ version. |

Add the chosen package to the project file. Do not add both unless the project requires a fallback.

---

## Data Binding Pattern — `classic` (VB.NET / System.Web / any .NET Framework version)

### Pattern A — Calling page passes its existing DataSet (preferred)

```vb
' QualityNoteForm.aspx.vb  (existing code — keep as-is)
Dim dsReportDataset As New DataSet()
Dim objQualityNote As New AppClassLib.clsQualityNote()
If Not objQualityNote.CreateReportDataset(iQualityNoteRef, dsReportDataset) Then
    Throw New Exception("QualityNote.CreateReportDataset returned false.")
End If

' Replace Crystal Reports lines with:
Dim renderer As New QualityNoteRenderer(
    CType(Application("DinkToPdfConverter"), IConverter),
    Server.MapPath("~/output/templates/QualityNote.html"))
renderer.RenderToBrowser(dsReportDataset, Response)
```

### Renderer class (`QualityNoteRenderer.vb`)

```vb
Public Class QualityNoteRenderer
    Private ReadOnly _converter As IConverter
    Private ReadOnly _templatePath As String

    Public Sub New(converter As IConverter, templatePath As String)
        _converter = converter
        _templatePath = templatePath
    End Sub

    Public Sub RenderToBrowser(ds As DataSet, response As HttpResponse)
        Dim html As String = BuildHtml(ds)
        Dim doc = New HtmlToPdfDocument() With {
            .GlobalSettings = New GlobalSettings() With {
                .PaperSize = PaperKind.A4,
                .Orientation = Orientation.Portrait
            },
            .Objects = {New ObjectSettings() With {.HtmlContent = html}}
        }
        Dim pdf As Byte() = _converter.Convert(doc)
        response.ContentType = "application/pdf"
        response.BinaryWrite(pdf)
        response.End()
    End Sub

    Private Function BuildHtml(ds As DataSet) As String
        Dim template As String = File.ReadAllText(_templatePath)
        ' Replace tokens from DataSet rows — e.g. {{FieldName}} → ds.Tables(0).Rows(0)("FieldName")
        For Each col As DataColumn In ds.Tables(0).Columns
            template = template.Replace("{{" & col.ColumnName & "}}", _
                CStr(ds.Tables(0).Rows(0)(col.ColumnName)))
        Next
        Return template
    End Function
End Class
```

---

## Data Binding Pattern — `modern` (C# / ASP.NET Core / any .NET 5+ version)

### Pattern A — Controller or Razor Page passes its existing DataSet (preferred)

```csharp
// QualityNoteController.cs (existing code — keep as-is)
var ds = new DataSet();
var qualityNote = new AppClassLib.ClsQualityNote();
if (!qualityNote.CreateReportDataset(qualityNoteRef, ds))
    throw new InvalidOperationException("CreateReportDataset returned false.");

// Replace Crystal Reports lines with:
var pdf = await _renderer.RenderAsync(ds);
return File(pdf, "application/pdf", "QualityNote.pdf");
```

### Renderer class (`QualityNoteRenderer.cs`)

```csharp
using DinkToPdf;
using DinkToPdf.Contracts;
using System.Data;

public class QualityNoteRenderer
{
    private readonly IConverter _converter;
    private readonly IWebHostEnvironment _env;

    public QualityNoteRenderer(IConverter converter, IWebHostEnvironment env)
    {
        _converter = converter;
        _env = env;
    }

    public Task<byte[]> RenderAsync(DataSet ds)
    {
        var templatePath = Path.Combine(_env.WebRootPath, "output", "templates", "QualityNote.html");
        var html = BuildHtml(ds, templatePath);

        var doc = new HtmlToPdfDocument
        {
            GlobalSettings = new GlobalSettings
            {
                PaperSize = PaperKind.A4,
                Orientation = Orientation.Portrait
            },
            Objects = { new ObjectSettings { HtmlContent = html } }
        };

        return Task.FromResult(_converter.Convert(doc));
    }

    private static string BuildHtml(DataSet ds, string templatePath)
    {
        var template = File.ReadAllText(templatePath);
        // Replace tokens from DataSet rows — e.g. {{FieldName}} → ds.Tables[0].Rows[0]["FieldName"]
        foreach (DataColumn col in ds.Tables[0].Columns)
        {
            template = template.Replace(
                $"{{{{{col.ColumnName}}}}}",
                ds.Tables[0].Rows[0][col.ColumnName]?.ToString() ?? string.Empty);
        }
        return template;
    }
}
```

### DI Registration (`Program.cs` — add once per project, not per renderer)

```csharp
// Register DinkToPdf converter as singleton (native library — one instance per process)
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Register each renderer
builder.Services.AddScoped<QualityNoteRenderer>();
```

> **Note:** DI registration in `Program.cs` is performed by the **agent** as a Post-Stage-3 step, not by this skill. This snippet is shown here for reference only. The skill writes the renderer file to `{srcFolder}/Reports/{ReportName}Renderer.cs` and notifies the agent; the agent then updates `Program.cs`.

### `modern` API differences from `classic`

| classic (.NET Framework / System.Web) | modern (ASP.NET Core / any .NET 5+) |
|---|---|
| `Server.MapPath("~/output/templates/")` | `_env.WebRootPath + "/output/templates/"` (`IWebHostEnvironment`) |
| `System.Web.HttpResponse` | `Microsoft.AspNetCore.Http.HttpResponse` |
| `Application("DinkToPdfConverter")` | `IConverter` injected via constructor DI |
| `Response.BinaryWrite(pdf); Response.End()` | `return File(pdf, "application/pdf", "name.pdf")` in controller |
| `Dim renderer As New ...` | `_renderer` injected via constructor DI |

---

## Batch Behaviour
- Process in batches of 4–6 templates per run.
- Log only: filename + pass/fail + PDF file size (bytes). Write errors to `output/logs/failures.json`.
- Fail a report if rendered PDF size = 0 bytes.

## Outputs

| Paradigm | Output file |
|---|---|
| `classic` | `{reportOutputFolder}/renderer/{ReportName}Renderer.vb` |
| `modern` | `{srcFolder}/Reports/{ReportName}Renderer.cs` (inside the C# project — compiles automatically) |
| `modern` (stub) | `{srcFolder}/Reports/{ReportName}Renderer.cs` with TODO placeholder; logged as `status: "stub"` in `{reportOutputFolder}/logs/failures.json` |

Generate the renderer file for all HTML templates that do not already have a matching renderer.

## Compatibility

| Platform | DinkToPdf | QuestPDF Community |
|---|---|---|
| Any .NET Framework (4.0–4.8) | ✅ | ✅ |
| Any ASP.NET Core (.NET 5–future) / MVC | ✅ | ✅ |
| Any ASP.NET Core (.NET 5–future) / Razor Pages | ✅ | ✅ |
| Azure App Service (Windows) | ✅ | ✅ |

## Guarantees
- No Crystal Reports runtime
- No paid PDF runtime
- Azure App Service-safe PDF generation
- Renderer output matches the visual layout of the source `.rpt`
- No `System.Web` reference in `modern` paradigm renderers
- No `linuxFxVersion` assumption — Windows App Service plan only