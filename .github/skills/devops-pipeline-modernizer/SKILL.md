---
# Last reviewed: 2026-07-29 — review quarterly, when defra-ai-config-examples is updated, or when Defra AI Toolkit guidance changes
skill:
  id: devops-pipeline-modernizer
  name: DevOps Pipeline Modernizer
  version: 1.0.0
  owner: Platform/DevOps
  intent: >
    Modernize CI/CD for a single .NET 10 repository with template-driven pipelines,
    fixed environment promotion, solution-driven app/database discovery, and
    config-file-based deployment to existing Azure infrastructure.
  recommended_agent: DevOps Pipeline Modernizer
  inputs_required:
    - repo_type: "Azure DevOps or GitHub Actions"
    - solution_path: "path to .sln file (or allow auto-discovery)"
    - current_pipelines: "paths to existing pipeline files"
  outputs:
    - docs/devops/solution-inventory.md
    - docs/devops/branching-strategy.md
    - docs/devops/environments.md
    - pipeline templates (ADO or GitHub Actions)
    - pipelines/config/deployment.config.yml (or equivalent JSON)
---

# DevOps Pipeline Modernizer (Skill)

This skill provides a structured, repeatable workflow to modernize CI/CD for a single repository run.
It is executed separately per repository.

## How to use (with a custom agent)
Reference these playbooks from your `devops.agent.md` body using Markdown links
to keep the agent prompt small and reusable. Custom agents can reference other
files via Markdown links. 

Suggested usage prompts:
- "Run Phase A discovery and produce a modernization plan"
- "Create ADO pipeline templates and a root azure-pipelines.yml"
- "Generate parameterized templates and deployment config for this repo"

## Phases / Playbooks
- [00 Scope & Guardrails](./00-scope-and-guardrails.md)
- [01 Discovery & Assessment](./01-discovery-assessment.md)
- [02 Branching Strategy](./02-branching-strategy.md)
- [03 CI/CD Template Architecture](./03-ci-cd-template-architecture.md)
- [04 Azure DevOps YAML Templates](./04-ado-yaml-templates.md)
- [05 GitHub Actions Templates](./05-github-actions-templates.md)
- [06 Deployment Config & Environments](./06-deployment-config-and-environments.md)
- [07 Validation, Security, Rollout & Checklist](./07-validation-security-rollout-checklist.md)

---

## Standards

This skill is loaded by the [devops-pipeline-modernizer agent](./../../agents/devops-pipeline-modernizer.agent.md). All outputs are subject to human review and AI transparency disclosure before use, per the [Defra AI Toolkit — Deliver with AI](https://digital.defra.gov.uk/ai-toolkit/deliver-with-ai). Follows [Defra SDS — GitHub Copilot guide](https://defra.github.io/software-development-standards/guides/github_copilot/).