# 07 — Validation, Security, Rollout & Checklist

## Objective
Provide one consolidated execution checklist for scan gates, deployment validation, rollout phases, and rollback readiness.

## Security scans and quality gates
Scan categories:
- SAST: code scanning
- SCA: dependency scanning
- Secret scanning: ensure secrets are not committed

Gate expectations by environment:
- dev and test: automated promotion after successful checks
- pre-prod and prod: manual approvals and required checks

PR quality gates:
- CI success required
- scan pass required
- optional coverage threshold enforcement

## Validation steps (per change)
- YAML lint/validate
- deployment config schema validation
- build + unit tests
- security scans
- deploy to dev
- deploy database first, then app
- smoke tests
- promote to test
- promote to pre-prod and prod with approvals

## Rollout strategy
- Phase 1: introduce templates (no behavior change)
- Phase 2: switch root pipeline to templates
- Phase 3: add scans/gates incrementally
- Phase 4: add DB deployment and smoke-test hardening
- Phase 5: optimize and enforce policies

## Rollback strategy
- Keep old pipeline YAML for 1-2 releases
- Provide a switch-back toggle (branch or pipeline variable)
- Document rollback steps, including previous DACPAC rollback process

## Deliverables checklist
CI/CD deliverables:
- [ ] solution inventory created
- [ ] repo deployment config file created
- [ ] root pipeline/workflows created
- [ ] templates created and parameterized
- [ ] PR validation enabled
- [ ] approvals/gates set for pre-prod/prod
- [ ] documentation added under docs/devops/

Deployment sequencing deliverables:
- [ ] database project detected from solution
- [ ] DACPAC artifact produced and published
- [ ] DB deploy stage runs before app deploy in each environment
- [ ] smoke test runs after app deploy

Documentation deliverables:
- [ ] branching-strategy.md
- [ ] environments.md
- [ ] modernization assessment doc
- [ ] validation and rollback plan
