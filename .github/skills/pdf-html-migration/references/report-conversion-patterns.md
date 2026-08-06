# Report Conversion Patterns
 
Reference for the `pdf-report-converter` agent. Defines the four report
archetypes, the TallPDF → Razor translation table, and per-archetype Razor
markup patterns. The translation table also covers iText 7 and FastReport for
cross-codebase genericity.
 
---
 
## Archetype Definitions
 
### Archetype A — Data-Driven Iterative
 
**Identifies as:** The report body method is primarily a `For Each` / `foreach`
loop over a collection of domain objects (sections, questions, contributions).
No hardcoded integer index pairs.
 
**Examples in this codebase:** `FullProfilePdf`, `ContributionReportPdf`,
`DiseaseReviewsReportPdf`, `ProfileGuidanceReportPdf`, `QAGuidanceReportPdf`
 
**View model pattern:**
```csharp
public class FullProfileViewModel : PdfLayoutModel
{
    public IReadOnlyList<SectionViewModel> Sections { get; }
 
    public FullProfileViewModel(Guid profileVersionId)
    {
        var profileVersion = ProfileVersionInfo.GetProfileVersionInfo(profileVersionId);
        ReportTitle = "Full profile";
        ProfileName = profileVersion.FullTitle;
        IsDraft = profileVersion.Status == ProfileVersionStatus.Draft;
        Sections = BuildSections(profileVersion);
    }
}
```
 
**Razor view pattern:**
```html
@model FullProfileViewModel
@{ Layout = "_PdfLayout"; }
 
@foreach (var section in Model.Sections)
{
    <div class="page-break-avoid">
        <h2 id="section-@section.Number">@Html.Raw(section.Title)</h2>
 
        @foreach (var question in section.Questions)
        {
            <div class="question-block">
                <p class="question-text"><strong>@Html.Raw(question.Text)</strong></p>
                <div class="answer-value">@Html.Raw(question.Value)</div>
            </div>
        }
    </div>
}
```
 
---
 
### Archetype B — Hardcoded Mapping
 
**Identifies as:** The report body calls a base class method with literal integer
pairs — e.g. `BuildNonTechnicalTitledQuestionTable(6, 14, "2", 0)` —
representing fixed `(sectionNumber, questionNumber)` positions.
 
**Examples in this codebase:** `SummaryProfilePdf`, `SummaryPrioritisationPdf`,
`QABriefPdf`
 
**Guidance sibling coupling:** Where a guidance report exists that mirrors the
same question mapping (e.g. `SummaryProfileGuidanceReportPdf` mirrors
`SummaryProfilePdf`), place the shared mapping in a single `static class`:
 
```csharp
// Shared between SummaryProfileViewModel and SummaryProfileGuidanceViewModel
internal static class SummaryProfileMapping
{
    // Each entry: (sectionNumber, questionNumber, displayLabel)
    public static readonly (int Section, int Question, string Label)[] Questions =
    {
        (1, 1,  "1"),
        (6, 14, "2"),
        (6, 1,  "3"),
        (6, 5,  "4"),
        (6, 3,  "5"),
        // ... all mapped questions
    };
}
```
 
**View model pattern:**
```csharp
public class SummaryProfileViewModel : PdfLayoutModel
{
    public IReadOnlyList<MappedQuestionViewModel> Questions { get; }
 
    public SummaryProfileViewModel(Guid profileVersionId)
    {
        var profileVersion = ProfileVersionInfo.GetProfileVersionInfo(profileVersionId);
        ReportTitle = "Summary profile";
        ProfileName = profileVersion.FullTitle;
        IsDraft = profileVersion.Status == ProfileVersionStatus.Draft;
 
        Questions = SummaryProfileMapping.Questions
            .Select(m =>
            {
                var section = profileVersion.GetSection(m.Section);
                // Safe resolution — no DirectCast, no FirstOrDefault without null check
                var question = section?.Questions
                    .FirstOrDefault(q => q.QuestionNumber == m.Question);
 
                return new MappedQuestionViewModel
                {
                    Label = m.Label,
                    QuestionText = question?.Text ?? string.Empty,
                    AnswerValue  = question?.Value ?? string.Empty,
                    IsResolved   = question is not null
                };
            })
            .ToList();
    }
}
```
 
**Razor view pattern:**
```html
@model SummaryProfileViewModel
@{ Layout = "_PdfLayout"; }
 
<table class="pdf-table">
    <thead>
        <tr><th>#</th><th>Question</th><th>Answer</th></tr>
    </thead>
    <tbody>
        @foreach (var q in Model.Questions)
        {
            <tr class="@(!q.IsResolved ? "diff-changed" : "")">
                <td>@q.Label</td>
                <td>@Html.Raw(q.QuestionText)</td>
                <td>@Html.Raw(q.AnswerValue)</td>
            </tr>
        }
    </tbody>
</table>
```
 
---
 
### Archetype C — Side-by-Side Comparison
 
**Identifies as:** The class takes two entity IDs (left/right) and uses
comparison block helper classes to render a diff view of two versions.
 
**Examples in this codebase:** `ProfileVersionComparisonReportPdf`
 
**Comparison block → Razor partial mapping:**
 
| Original block class | Razor partial |
|---|---|
| `StandardAnswerComparisonTable` | `_StandardAnswerComparison.cshtml` |
| `StandardAnswerComparisonDifferenceTable` | `_StandardAnswerComparisonDiff.cshtml` |
| `RepeatingAnswerComparisonTable` | `_RepeatingAnswerComparison.cshtml` |
| `RepeatingAnswerComparisonDifferenceTable` | `_RepeatingAnswerComparisonDiff.cshtml` |
| `PerSpeciesAnswerComparisonTable` | `_PerSpeciesAnswerComparison.cshtml` |
| `PerSpeciesAnswerComparisonDifferenceTable` | `_PerSpeciesAnswerComparisonDiff.cshtml` |
| `FieldValueListComparisonCell` | `_FieldValueListComparison.cshtml` |
| `FieldValueListComparisonDifferenceCell` | `_FieldValueListComparisonDiff.cshtml` |
 
**View model pattern:**
```csharp
public record AnswerPair(
    string Label,
    string LeftValue,
    string RightValue,
    bool IsDifferent,
    AnswerType Type
);
 
public class ProfileVersionComparisonViewModel : PdfLayoutModel
{
    public string LeftTitle { get; }
    public string RightTitle { get; }
    public IReadOnlyList<SectionComparisonViewModel> Sections { get; }
 
    public ProfileVersionComparisonViewModel(Guid leftId, Guid rightId)
    {
        // Fetch both versions and compute IsDifferent per question
        // IsDifferent = !string.Equals(left.Value, right.Value, Ordinal)
    }
}
```
 
**Razor view pattern:**
```html
@model ProfileVersionComparisonViewModel
@{ Layout = "_PdfLayout"; }
 
@foreach (var section in Model.Sections)
{
    <h2 class="page-break-before">@Html.Raw(section.Title)</h2>
    <table class="pdf-table">
        <thead>
            <tr>
                <th>Question</th>
                <th>@Html.Raw(Model.LeftTitle)</th>
                <th>@Html.Raw(Model.RightTitle)</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var answer in section.Answers)
            {
                <tr class="@(answer.IsDifferent ? "diff-changed" : "")">
                    <td>@Html.Raw(answer.Label)</td>
                    <td>@Html.Raw(answer.LeftValue)</td>
                    <td>@Html.Raw(answer.RightValue)</td>
                </tr>
            }
        </tbody>
    </table>
}
```
 
---
 
### Archetype D — Chart-Embedded
 
**Identifies as:** The class imports a Windows charting library
(`System.Web.DataVisualization`, `OxyPlot`, `LiveCharts`, etc.) and renders
one or more charts into the PDF.
 
**Examples in this codebase:** `ProfileRankingReportPdf`,
`ProfileRankingReportRFIPdf`
 
**Chart library replacement:**
 
| Original library | Replacement |
|---|---|
| `System.Web.DataVisualization` | Chart.js |
| `OxyPlot` | Chart.js |
| `LiveCharts` | Chart.js |
| `SkiaSharp` | Chart.js (if browser-rendered) or retain SkiaSharp server-side |
 
**View model pattern:**
```csharp
public class ChartDataset
{
    public string Label { get; set; } = string.Empty;
    public IReadOnlyList<double> Data { get; set; } = [];
    public string BackgroundColor { get; set; } = "rgba(54, 162, 235, 0.6)";
}
 
public class RankingChartData
{
    public IReadOnlyList<string> Labels { get; set; } = [];
    public IReadOnlyList<ChartDataset> Datasets { get; set; } = [];
}
 
public class ProfileRankingViewModel : PdfLayoutModel
{
    public string FilterType { get; set; } = "All";
    public RankingChartData ChartData { get; set; } = new();
    public IReadOnlyList<RankingRowViewModel> RankingRows { get; set; } = [];
}
```
 
**Razor view pattern:**
```html
@model ProfileRankingViewModel
@{ Layout = "_PdfLayout"; }
 
<script src="https://cdn.jsdelivr.net/npm/chart.js@@4.4.0/dist/chart.umd.min.js"></script>
 
<div class="landscape-section page-break-before">
 
    <!-- Chart -->
    <canvas id="rankingChart" width="900" height="400"></canvas>
 
    <!-- Ranking table -->
    <table class="pdf-table" style="margin-top: 20px">
        <thead>
            <tr>
                <th>Rank</th><th>Disease / Profile</th><th>Score</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var row in Model.RankingRows)
            {
                <tr>
                    <td>@row.Rank</td>
                    <td>@Html.Raw(row.Title)</td>
                    <td>@row.Score.ToString("F2")</td>
                </tr>
            }
        </tbody>
    </table>
</div>
 
<script>
    (function () {
        var ctx = document.getElementById('rankingChart').getContext('2d');
        var chartData = @Html.Raw(Json.Serialize(Model.ChartData));
        new Chart(ctx, {
            type: 'bar',
            data: chartData,
            options: {
                responsive: false,
                animation: {
                    onComplete: function () {
                        window.chartRendered = true;
                    }
                },
                plugins: {
                    legend: { display: true }
                }
            }
        });
    })();
</script>
```
 
In `PlaywrightPdfService`, pass `WaitForFunction = "chartRendered"` in the
`PdfRenderOptions` for all Archetype D reports.
 
---
 
## Full TallPDF → Razor Translation Table
 
| TallPDF construct | Razor / CSS equivalent |
|---|---|
| `XhtmlParagraph` — inline HTML rendering | `@Html.Raw(Model.FieldValue)` |
| `TextParagraph` with `Fragment` | `<p>@Model.PlainText</p>` |
| `Fragment.HasContextFields = True` / `"#p of #P"` | CSS `counter(page) " of " counter(pages)` in `_PdfLayout.cshtml` |
| `CrossreferenceSection` / `ComposeEntry` (TOC) | `<nav>` with `<a href="#section-N">` anchor links |
| `Actions.GoToAction` (clickable TOC entries) | Standard HTML `<a href="#anchor">` links |
| `ForegroundAreas` + `Drawing.TextShape` (watermark) | CSS `::before` on `.draft-watermark` class |
| `Section` with `SectionOrientation.Landscape` | `<div class="landscape-section">` + CSS named page |
| New `Section` for page break | `<div class="page-break-before">` |
| `Table` with `Row`, `Cell` | `<table class="pdf-table"><tr><td>` |
| `Cell.BackgroundColor = RgbColor(r,g,b)` | `style="background-color: rgb(r,g,b)"` |
| `Pens.Pen(GrayColor)` cell border | `style="border: 1px solid #ccc"` |
| `ImageParagraph` (logo from `Bitmap`) | `<img src="~/images/logo.png">` |
| `Bitmap` from embedded resource | Static file in `wwwroot/images/` |
| Checkbox `Bitmap` (selected/unselected) | SVG or Unicode `&#9745;` / `&#9744;` |
| `DirectCast(obj, SpecificType)` | C# `if (obj is SpecificType t)` pattern match |
| `FirstOrDefault(...)` without null check | `FirstOrDefault(...)` with null guard and `IsResolved` flag |
| `Document.DocumentInfo.Title` | `<title>@Model.ReportTitle</title>` |
| `PdfDocument.Sections.Add(section)` | `@RenderBody()` in layout |
| `ForegroundArea.Contents.Add(stamp)` | CSS `::before` overlay |
 
---
 
## iText 7 Translation Table
 
| iText 7 construct | Razor / CSS equivalent |
|---|---|
| `HtmlConverter.ConvertToElements(html)` | `@Html.Raw(html)` |
| `PdfDocument` + `Document` | `_PdfLayout.cshtml` + Playwright |
| `PageEvent` header/footer handler | CSS `@page` `@@top-center` / `@@bottom-center` |
| `PdfOutline` (bookmark tree) | Anchor `<a href="#id">` links |
| `Paragraph` with `Text` | `<p>@Model.Text</p>` |
| `Table` + `Cell` | `<table><tr><td>` |
| `Cell.SetBackgroundColor(color)` | `style="background-color: ..."` |
 
---
 
## FastReport Translation Table
 
| FastReport construct | Razor / CSS equivalent |
|---|---|
| `.frx` report template | Razor `.cshtml` view |
| `DataBand` iterating a data source | `@foreach` loop over view model collection |
| `TextObject` | `<span>` or `<p>` |
| `TableObject` | `<table>` |
| `PictureObject` | `<img>` |
| `ShapeObject` (line, rectangle) | CSS border or `<hr>` |
| `GroupHeaderBand` | `<h2>` section heading |
| `PageFooterBand` | CSS `@@bottom-center` in `@page` |
| Report parameter | View model property |