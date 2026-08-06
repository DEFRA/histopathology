# 05 — GitHub Actions Templates (Blueprint)

## Suggested structure
- /.github/workflows/ci-cd.yml
- /.github/config/deployment.config.json
- /.github/actions/composite/build-test/action.yml
- /.github/actions/composite/scan/action.yml

## Deployment config source of truth
Use the schema and examples in [06 Deployment Config & Environments](./06-deployment-config-and-environments.md).

## CI/CD workflow (example)
```yaml
name: CI-CD
on:
  pull_request:
  push:
    branches: ["main"]

jobs:
  build_test_scan:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Validate deployment config
        run: cat .github/config/deployment.config.json > /dev/null
      - name: Build/Test
        uses: ./.github/actions/composite/build-test
      - name: Scan
        uses: ./.github/actions/composite/scan

  deploy_dev:
    needs: build_test_scan
    environment: dev
    runs-on: ubuntu-latest
    steps:
      - name: Deploy DB, deploy app, smoke test
        run: echo "deploy dev"

  deploy_test:
    needs: deploy_dev
    environment: test
    runs-on: ubuntu-latest
    steps:
      - name: Deploy DB, deploy app, smoke test
        run: echo "deploy test"

  deploy_preprod:
    needs: deploy_test
    environment: pre-prod
    runs-on: ubuntu-latest
    steps:
      - name: Deploy DB, deploy app, smoke test
        run: echo "deploy pre-prod"

  deploy_prod:
    needs: deploy_preprod
    environment: prod
    runs-on: ubuntu-latest
    steps:
      - name: Deploy DB, deploy app, smoke test
        run: echo "deploy prod"
```

Notes:
- Keep workflow YAML generic and parameterized.
- Do not hardcode app or repo identity in workflow files.
- Run this skill separately in each repository.