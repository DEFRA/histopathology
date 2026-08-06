# 03 — CI/CD Template Architecture

## Objective
Create reusable templates for a single repository to reduce duplication and enforce standards.

## Template families
CI templates:
- build
- test
- package/publish
- scan (SAST/SCA/secret scan)
- SBOM (optional)

CD templates:
- deploy-db (per environment)
- deploy-app (per environment)
- smoke tests post-deploy
- approvals/gates for higher envs
- rollback procedure

## Parameters to standardize
- environment
- buildConfiguration
- artifactName
- serviceConnection
- resourceGroup, webAppName, sqlServerName, databaseName, slotName

## Config model
- Keep YAML templates app-agnostic and repo-agnostic.
- Store app-specific Azure target values in one repo config file:
	- `pipelines/config/deployment.config.yml` (Azure DevOps)
	- `.github/config/deployment.config.json` (GitHub Actions)
- The pipeline reads config values at runtime for each fixed environment: dev, test, pre-prod, prod.

## Output
- Template folder structure + example root pipeline composing templates