# 02 — SSIS to ADF Artifact Conversion
 
Use this phase to convert SSIS analysis output into ADF artifact JSON required to deploy a new factory.
 
## Inputs
 
- `analysisIndexPath`: path to `analysis/index/ssis-analysis-index.json`
- `outputFolder`: output folder for generated JSON
- `dataFactoryName`
- `subscriptionId`
- `resourceGroupName`
- `location`
 
## Execution (Instruction-Only)
 
1. Read `analysis/index/ssis-analysis-index.json` and each package analysis JSON.
2. For each SSIS package, map to one ADF pipeline and generate artifacts in a package-specific folder.
3. Create one managed identity details file per package with identity type, principalId placeholder, resource scopes, and RBAC roles.
4. Record unsupported mappings under manual migration notes.
5. Ensure output naming follows user conventions.
 
## Required JSON Outputs
 
- `packages/<package-name>/factory/factory.json`
- `packages/<package-name>/integrationRuntime/AutoResolveIntegrationRuntime.json`
- `packages/<package-name>/linkedService/*.json`
- `packages/<package-name>/dataset/*.json`
- `packages/<package-name>/pipeline/*.json`
- `packages/<package-name>/trigger/*.json` (when schedule or event equivalents are identified)
- `packages/<package-name>/ARMTemplateForFactory.json`
- `packages/<package-name>/ARMTemplateParametersForFactory.json`
- `packages/<package-name>/managed-identity-details.json`
 
## Managed Identity Requirements
 
Each package `managed-identity-details.json` must include:
 
- Identity type (`SystemAssigned` or `UserAssigned`)
- Principal ID placeholder (resolved post-deployment)
- Target resources needing access
- Required RBAC roles and scope suggestions
 
## Quality Gates
 
- Factory JSON has identity enabled
- Every converted package produces one pipeline JSON
- ARM template references all generated resources
- Unmappable transformations are captured under manual migration notes
- Every discovered `.dtsx` has a corresponding `packages/<package-name>/` artifact folder