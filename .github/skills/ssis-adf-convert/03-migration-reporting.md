# 03 — Migration Reporting
 
Use this phase to produce a stakeholder-ready migration report after artifact generation.
 
## Inputs
 
- `analysisIndexPath`: path to `analysis/index/ssis-analysis-index.json`
- `artifactsPath`: path to generated per-package ADF JSON artifacts
- `outputPath`: output folder path for migration reports
 
## Execution (Instruction-Only)
 
1. Iterate through each package in `analysis/index/ssis-analysis-index.json`.
2. For each package, compute conversion coverage from its analysis and artifact folder.
3. For each package, include artifact inventory, identity/RBAC actions, and manual migration items.
4. Generate one migration report file per package from the migration template.
5. Optionally generate a compact overall index report with links to all package reports.
 
## Required Content
 
- Scope and package inventory
- Conversion coverage metrics
- Artifact inventory
- Managed identity and RBAC plan
- Risks, gaps, and manual migration actions
- Cutover and validation recommendations
 
Use template:
 
- `templates/migration-report.md`
 
## Required Outputs
 
- `reports/migration/<package-name>-migration-report.md`
- `reports/migration/migration-report-index.md` (optional summary)
 
## Quality Gates
 
- Coverage percentage included
- Manual migration list included
- Identity/RBAC actions explicitly listed
- Clear next-step deployment checklist
- Every discovered `.dtsx` has a corresponding migration report file