# HtmlTemplateGenerationSkill
 
## Purpose
Generates reusable, PDF-ready HTML templates from `ReportDefinition.json`.
 
inputs:
- `{reportOutputFolder}/definition/{ReportName}.ReportDefinition.json`
- Guidance: DinkToPdf uses a Chromium-based rendering engine — modern CSS (flexbox, grid, CSS variables) is fully supported. Prefer table-based layouts and inline styles only when reproducing dense tabular report layouts. Do **not** reference external stylesheets; all CSS must be inline or in a `<style>` block within the HTML file.

## Responsibilities
- Consider all files from the `{reportOutputFolder}/definition/` folder.
- Skip any report that already has a matching `{reportOutputFolder}/templates/{ReportName}.html` (idempotent).
- Build pixel-accurate HTML templates using `elements[].token`, `x`, `y`, `width`, `height`, `font`, `alignment`, `border` from the definition.
- Use CSS for layout, position and styling from inputs json.
- Insert placeholder tokens from JSON (e.g. `{{Field.SubmissionID}}`).
- Apply `renderHints`: repeat header row when `repeatHeaderOnEachPage: true`; insert page-break when `pageBreakOnGroup: true`.
 
## Batch Behaviour
- Process in batches of 4–6 definition files per run.
- Log only: filename + pass/fail. Write errors to `output/logs/failures.json`.
 
## Outputs
Create or Update:
1. `{reportOutputFolder}/templates/{ReportName}.html`
   - One file per `ReportDefinition.json`.
   - Primary template used by `HtmlToPdfConversionSkill` for token replacement at render time.
2. `{srcFolder}/wwwroot/reports/{ReportName}.html` — **modern paradigm only**
   - Copy of the template placed inside the C# project so it is served by `IWebHostEnvironment.WebRootPath` at runtime.
   - Only written when `runtimeParadigm=modern` and `srcFolder` is provided.
   - Do **not** write this copy for `classic` paradigm.