# 01 — Discovery & Assessment (Read-only)

## Inputs
- Pipeline platform: Azure DevOps YAML or GitHub Actions
- Path to current pipeline files
- Path to solution file (.sln) or repo root for solution discovery

## Steps
1) Parse the solution file and classify projects:
   - identify application project(s) for deploy
   - identify database project (.sqlproj)
   - record build/publish artifacts needed
2) Inventory existing pipelines:
   - triggers, stages/jobs, deployment mechanism, approvals
   - artifacts, test strategy, scan steps, manual steps
3) Identify duplication and template candidates:
   - build/test/package/deploy/scan/notify
4) Evaluate environment configuration:
   - where variables are stored
   - how secrets are handled
   - service connections / permissions
5) Produce a modernization report:
   - Current-state summary
   - Gaps/Risks
   - Target-state proposal
   - Proposed repo structure

Notes:
- Environments are fixed: dev, test, pre-prod, prod.
- Do not request environment names as input.

## Output format
- `docs/devops/solution-inventory.md`
- `docs/devops/pipeline-modernization-assessment.md`
with:
- Current state
- Proposed target state
- Recommended templates and parameters
- Migration plan (phased)
- Risks & mitigations