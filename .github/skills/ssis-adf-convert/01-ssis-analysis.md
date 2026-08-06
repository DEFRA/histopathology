# 01 — SSIS Analysis
 Use this phase to inspect SSIS packages and generate a report that clearly captures **inputs, outputs, and transformation logic**.
 
## Inputs
 
- `packagePath`: single `.dtsx` package file path
- `outputPath`: folder for generated report
 
## Execution (Instruction-Only)
 
1. Validate that `packagePath` points to exactly one `.dtsx` file.
2. Extract only required analysis fields from that package:
   - connection managers and endpoints
   - control flow tasks
   - data flow sources, destinations, and transformations
3. Use compact extraction style for every item:
   - one-line records
   - no repeated prose across sections
   - no explanatory paragraphs unless mapping is unclear
4. Apply token controls while preserving coverage:
   - list all discovered items by name
   - keep descriptions to one sentence per item
   - for repeated/similar components, summarize pattern once and list component names
   - include examples only for ambiguous or non-standard transformations
5. Generate one report for the selected package using the analysis template at:
   - `reports/analysis/<package-name>-ssis-analysis-report.md`
6. Add confidence per extracted mapping (`High/Medium/Low`) and include reasons only for `Medium/Low`.
7. End the package report with a short "Manual Review Needed" list (only unresolved or uncertain items).
8. Enforce hard compactness rules:
   - do not include raw XML blocks
   - do not include full SQL or expression text longer than 120 characters
   - for long SQL/expressions, include short summary + `truncated` marker
   - keep each record in a single line
9. Chat response must include only:
   - package name
   - counts (inputs, outputs, transformations, manual items)
   - generated report file path
 
## Quality Gates
 
- The selected package is a valid `.dtsx` file
- Every data flow has at least one source or destination mapped
- Transformations listed by task with confidence markers for uncertain mappings
- One package analysis report is generated for the selected package
 