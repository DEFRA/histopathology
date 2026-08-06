# 00 — Scope & Guardrails

## Objective
Modernize delivery pipelines safely, incrementally, and repeatably for one repository per run.

## Non‑negotiables
- Do **not** write secrets, tokens, passwords, client secrets, or connection strings.
- Use Key Vault references and variable groups / secret stores.
- Prefer incremental PRs: "Introduce templates" → "Adopt templates" → "Add gates/scans" → "Stabilize promotions".
- Do not embed app or repo identity in YAML templates. Keep templates generic and parameterized.
- Environments are fixed and must not be requested as input: `dev` → `test` → `pre-prod` → `prod`.

## Assumptions (if not provided)
- Runtime: .NET 10
- Database deployment source: database project in the solution (.sqlproj)
- Deployment model: infra already provisioned in Azure
- PR policy: 1–2 reviewers, build validation, and branch protection on main

## Deliverables (minimum)
- Solution inventory document (app project + db project discovered from .sln)
- Branching strategy doc
- Environment configuration doc
- Repo-level deployment config file (non-secret Azure target references)
- Template-based pipeline structure
- Validation plan and rollback guidance
