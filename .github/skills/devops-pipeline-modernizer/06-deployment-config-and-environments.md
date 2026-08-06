# 06 — Deployment Config & Environments

## Objective
Define a single source of truth for non-secret deployment targets and fixed environment promotion.

## Fixed environments (not user input)
- dev
- test
- pre-prod
- prod

## Configuration model
- Keep pipeline templates generic and repository-agnostic.
- Store non-secret deployment targets in one repo config file.
- Store secrets in secret store only; never in repo config.

Azure DevOps config location:
- `pipelines/config/deployment.config.yml`

GitHub Actions config location:
- `.github/config/deployment.config.json`

## Required config fields
Top-level fields:
- `solutionFile`
- `appProjectPath`
- `dbProjectPath`

Per-environment fields:
- `serviceConnection`
- `resourceGroup`
- `webAppName`
- `sqlServerName`
- `databaseName`

## Azure DevOps example
```yaml
solutionFile: src/YourSolution.sln
appProjectPath: src/YourApp/YourApp.csproj
dbProjectPath: src/YourDatabase/YourDatabase.sqlproj

environments:
  dev:
    serviceConnection: sc-dev
    resourceGroup: rg-dev
    webAppName: app-dev
    sqlServerName: sql-dev
    databaseName: appdb
  test:
    serviceConnection: sc-test
    resourceGroup: rg-test
    webAppName: app-test
    sqlServerName: sql-test
    databaseName: appdb
  pre-prod:
    serviceConnection: sc-preprod
    resourceGroup: rg-preprod
    webAppName: app-preprod
    sqlServerName: sql-preprod
    databaseName: appdb
  prod:
    serviceConnection: sc-prod
    resourceGroup: rg-prod
    webAppName: app-prod
    sqlServerName: sql-prod
    databaseName: appdb
```

## GitHub Actions example
```json
{
  "solutionFile": "src/YourSolution.sln",
  "appProjectPath": "src/YourApp/YourApp.csproj",
  "dbProjectPath": "src/YourDatabase/YourDatabase.sqlproj",
  "environments": {
    "dev": { "serviceConnection": "sc-dev", "resourceGroup": "rg-dev", "webAppName": "app-dev", "sqlServerName": "sql-dev", "databaseName": "appdb" },
    "test": { "serviceConnection": "sc-test", "resourceGroup": "rg-test", "webAppName": "app-test", "sqlServerName": "sql-test", "databaseName": "appdb" },
    "pre-prod": { "serviceConnection": "sc-preprod", "resourceGroup": "rg-preprod", "webAppName": "app-preprod", "sqlServerName": "sql-preprod", "databaseName": "appdb" },
    "prod": { "serviceConnection": "sc-prod", "resourceGroup": "rg-prod", "webAppName": "app-prod", "sqlServerName": "sql-prod", "databaseName": "appdb" }
  }
}
```

## Rules
- No secrets, passwords, tokens, or connection strings in config files.
- No app/repo identity hardcoded in pipeline templates.
- Infra is pre-provisioned; do not generate Bicep in this skill.

## Output
- `docs/devops/environments.md`
- repo deployment config file in ADO or GitHub format
