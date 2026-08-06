# SSIS Analysis Report
 
Package: `{{package_name}}`
 
## Summary
 
- SSIS Root Path: `{{ssis_root_path}}`
- Packages Discovered: `{{package_count}}`
- Inputs: `{{input_count}}`
- Outputs: `{{output_count}}`
- Transformations: `{{transformation_count}}`
- Analysis Date: `{{analysis_date}}`
- Notes: {{notes}}
 
## Input Mapping (All Items)
 
- `{{task_name}}` | `{{source_component}}` | `{{source_endpoint}}` | Confidence: High
 
## Output Mapping (All Items)
 
- `{{task_name}}` | `{{destination_component}}` | `{{destination_endpoint}}` | Confidence: High
 
## Transformation Logic (All Items)
 
- `{{task_name}}` | `{{transformation}}` | `{{adf_activity}}` | `{{logic_description}}` | Confidence: Medium
 
## Manual Review Needed
 
- `{{item_name}}` | {{reason}} | {{suggested_action}}