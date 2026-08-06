# 02 — Branching Strategy

## Goal
Choose a strategy that matches release cadence, risk profile, and team size.

## Preferred strategy (default): Trunk-based
- main = trunk
- short-lived feature branches
- PR required into main
- release tags from main
- hotfix branches only when needed

## Alternative: GitFlow-lite
- main + develop (optional)
- release branches for release trains
- hotfix branches from main

## Must include
- Branch naming conventions
- PR policies (required reviewers, build checks, status checks)
- Versioning scheme (SemVer + build metadata or date-based)
- Hotfix policy
- Release tagging policy
- Merge strategy (squash vs rebase vs merge commit)

## Output
- `docs/devops/branching-strategy.md`