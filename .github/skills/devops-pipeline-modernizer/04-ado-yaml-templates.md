# 04 — Azure DevOps YAML Templates (Blueprint)

> Use these patterns as generic templates. This skill is run once per repository.

## Suggested structure
- /pipelines/azure-pipelines.yml (root)
- /pipelines/config/deployment.config.yml
- /pipelines/templates/ci-build.yml
- /pipelines/templates/ci-test.yml
- /pipelines/templates/ci-publish.yml
- /pipelines/templates/ci-scan.yml
- /pipelines/templates/cd-deploy-db.yml
- /pipelines/templates/cd-deploy-app.yml
- /pipelines/templates/cd-smoke-test.yml

## Deployment config source of truth
Use the schema and examples in [06 Deployment Config & Environments](./06-deployment-config-and-environments.md).

## Root pipeline (example)
```yaml
trigger:
  branches:
    include:
      - main

pr:
  branches:
    include:
      - main
      - feature/*
      - hotfix/*

variables:
  - template: config/deployment.config.yml

stages:
- stage: CI
  jobs:
  - template: templates/ci-build.yml
    parameters:
      buildConfiguration: Release
  - template: templates/ci-test.yml
  - template: templates/ci-publish.yml
  - template: templates/ci-scan.yml

- stage: Deploy_Dev
  dependsOn: CI
  jobs:
  - template: templates/cd-deploy-db.yml
    parameters:
      environmentName: dev
      serviceConnection: $(environments.dev.serviceConnection)
      sqlServerName: $(environments.dev.sqlServerName)
      databaseName: $(environments.dev.databaseName)
  - template: templates/cd-deploy-app.yml
    parameters:
      environmentName: dev
      serviceConnection: $(environments.dev.serviceConnection)
      resourceGroup: $(environments.dev.resourceGroup)
      webAppName: $(environments.dev.webAppName)
  - template: templates/cd-smoke-test.yml
    parameters:
      webAppName: $(environments.dev.webAppName)

- stage: Deploy_Test
  dependsOn: Deploy_Dev
  jobs:
  - template: templates/cd-deploy-db.yml
    parameters:
      environmentName: test
      serviceConnection: $(environments.test.serviceConnection)
      sqlServerName: $(environments.test.sqlServerName)
      databaseName: $(environments.test.databaseName)
  - template: templates/cd-deploy-app.yml
    parameters:
      environmentName: test
      serviceConnection: $(environments.test.serviceConnection)
      resourceGroup: $(environments.test.resourceGroup)
      webAppName: $(environments.test.webAppName)
  - template: templates/cd-smoke-test.yml
    parameters:
      webAppName: $(environments.test.webAppName)

- stage: Deploy_PreProd
  dependsOn: Deploy_Test
  jobs:
  - template: templates/cd-deploy-db.yml
    parameters:
      environmentName: pre-prod
      serviceConnection: $(environments.pre-prod.serviceConnection)
      sqlServerName: $(environments.pre-prod.sqlServerName)
      databaseName: $(environments.pre-prod.databaseName)
  - template: templates/cd-deploy-app.yml
    parameters:
      environmentName: pre-prod
      serviceConnection: $(environments.pre-prod.serviceConnection)
      resourceGroup: $(environments.pre-prod.resourceGroup)
      webAppName: $(environments.pre-prod.webAppName)
  - template: templates/cd-smoke-test.yml
    parameters:
      webAppName: $(environments.pre-prod.webAppName)

- stage: Deploy_Prod
  dependsOn: Deploy_PreProd
  jobs:
  - template: templates/cd-deploy-db.yml
    parameters:
      environmentName: prod
      serviceConnection: $(environments.prod.serviceConnection)
      sqlServerName: $(environments.prod.sqlServerName)
      databaseName: $(environments.prod.databaseName)
  - template: templates/cd-deploy-app.yml
    parameters:
      environmentName: prod
      serviceConnection: $(environments.prod.serviceConnection)
      resourceGroup: $(environments.prod.resourceGroup)
      webAppName: $(environments.prod.webAppName)
  - template: templates/cd-smoke-test.yml
    parameters:
      webAppName: $(environments.prod.webAppName)
```

## Notes
- Keep template YAML generic and parameterized.
- Do not hardcode app name, repository name, or resource names in template files.
- This same blueprint is reused across repositories; only the repo config file changes.