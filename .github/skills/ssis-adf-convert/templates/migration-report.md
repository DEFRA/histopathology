 # SSIS to ADF Migration Report
 
Package: `{{package_name}}`
Artifact Root: `{{package_artifact_root}}`
 
## 1. Summary
 
| Field | Value |
| --- | --- |
| Source Platform | SSIS |
| Target Platform | Azure Data Factory |
| Total Packages | `{{total_packages}}` |
| Converted Pipelines | `{{converted_pipelines}}` |
| Conversion Coverage | `{{coverage_percent}}` |
| Report Date | `{{report_date}}` |
 
## 2. Artifact Inventory
 
| Artifact Type | Count | Location |
| --- | --- | --- |
| Factory | `{{factory_count}}` | `factory/` |
| Integration Runtime | `{{ir_count}}` | `integrationRuntime/` |
| Linked Services | `{{ls_count}}` | `linkedService/` |
| Datasets | `{{dataset_count}}` | `dataset/` |
| Pipelines | `{{pipeline_count}}` | `pipeline/` |
| Triggers | `{{trigger_count}}` | `trigger/` |
 
## 3. Managed Identity Plan
 
| Identity Setting | Value |
| --- | --- |
| Identity Type | `{{identity_type}}` |
| Principal ID | `{{principal_id_placeholder}}` |
| Key Role Assignments | `{{role_assignment_summary}}` |
 
## 4. Mapping Notes
 
| Package | SSIS Logic | ADF Mapping | Confidence | Notes |
| --- | --- | --- | --- | --- |
| `{{package_name}}` | `{{ssis_logic}}` | `{{adf_mapping}}` | Medium | {{notes}} |
 
## 5. Risks and Manual Migration Items
 
| # | Category | Description | Impact | Mitigation |
| --- | --- | --- | --- | --- |
| 1 | `{{category}}` | {{description}} | {{impact}} | {{mitigation}} |
 
## 6. Deployment and Validation Checklist
 
- [ ] Deploy `ARMTemplateForFactory.json` and `ARMTemplateParametersForFactory.json`
- [ ] Confirm managed identity creation and capture principal ID
- [ ] Apply RBAC roles to source/sink resources
- [ ] Configure linked service credentials using Key Vault
- [ ] Execute smoke run for each generated pipeline
- [ ] Validate row counts and data quality for critical entities
 