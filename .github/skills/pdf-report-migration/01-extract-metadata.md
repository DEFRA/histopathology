# RptDefinitionGenerationSkill
 
**Input:** `{rptSourceFolder}/{ReportName}.rpt` (agent parameter `rptSourceFolder`, default `reports/`)
**Output:** `{reportOutputFolder}/definition/{ReportName}.ReportDefinition.json` (agent parameter `reportOutputFolder`, default `output/`)

---

## Batch Behaviour
- Process 4–6 reports per run.
- Skip any report whose `{reportOutputFolder}/definition/{ReportName}.ReportDefinition.json` already exists (idempotent). This is enforced by the agent-level Stage Skip Detection rule — do not re-implement it here.
- Regeneration request → confirm (⚠️ downstream invalidated); overwrite on confirmation, mark `{reportOutputFolder}/templates/` + `{reportOutputFolder}/renderer/` + `{srcFolder}/Reports/` stale.
- Retry 3×; log failures to `{reportOutputFolder}/logs/failures.json`; continue.
 
## Output Schema
 
### Report-level fields
 
| Field | Description |
|---|---|
| `reportName` | Report name |
| `pageSettings` | `width`, `height` (mm), `orientation`, `marginTop`, `marginBottom`, `marginLeft`, `marginRight` (mm) |
| `dataFields[]` | `name`, `dataType`, `dataSource` |
| `parameters[]` | `name`, `type`, `prompt`, `defaultValue` |
| `formulaFields[]` | `name`, `expression` — normalised per Formula Rules below |
| `sections[]` | `type` (header/detail/footer/group), `sectionHeight` (mm), `elements[]` |
| `renderHints` | `repeatHeaderOnEachPage` (bool), `pageBreakOnGroup` (bool) |
| `complexity` | `"low"` or `"high"` |
 
### Element fields (inside `sections[].elements[]`)
 
| Field | Applies to | Description |
|---|---|---|
| `type` | All | `TextElement`, `FieldElement`, `BoxElement`, `LineElement`, `SubReportElement` |
| `x`, `y`, `width`, `height` | All | mm from section top-left |
| `font` | Text, Field | `family`, `size`(pt), `bold`, `italic`, `underline`, `strikethrough` (bool), `color`(hex) |
| `alignment` | Text, Field | `"left"`, `"center"`, `"right"` |
| `border` | Text, Field, Box | `top`, `bottom`, `left`, `right` (bool) |
| `backgroundColor` | Text, Field, Box | hex or `"none"` |
| `token` | Text, Field, SubReport | `{{Table.Field}}`, `{{Formula.Name}}`, `{{SubReport.Name}}` — omit for Box/Line |
| `lineWeight`, `lineColor` | Box, Line | pt, hex |
| `fillColor` | Box | hex or `"none"` |
| `x1`,`y1`,`x2`,`y2`,`direction` | Line | mm coords; `"horizontal"` or `"vertical"` |
| `subReportName`, `linkedFields[]` | SubReport | filename, link fields; forces `complexity:"high"` on parent |
 
Unresolvable field → set `null`, add `"_review":true` on that element.
 
## Output
Create or Update:
1. `{reportOutputFolder}/definition/{ReportName}.ReportDefinition.json`
  - sole input for `HtmlTemplateGenerationSkill`
 