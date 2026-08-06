# Agent Versioning Strategy

## Purpose

This document defines how agent versions are created, owned, and evolved in this repository.

## Baseline Rule

- All existing agents are **Version 1 (v1)**.
- Existing v1 agent implementations are the baseline and must not be changed for feature evolution.
- Future enhancements must be implemented as new versions (`v2`, `v3`, and so on).

## Folder Structure Standard

Each agent must have a dedicated version root and one folder per version:

```text
.github/agents/versions/<agent-name>/
  v1/
  v2/
  v3/
```

### Naming

- `<agent-name>` must match the canonical agent identifier used in `.github/agents/`.
- Version folders must use lowercase `v` followed by an integer (for example: `v1`, `v2`).

## Ownership Model

For each version, the owning team must be explicit in that version's README file.

Ownership responsibilities:

1. Maintain functional scope and constraints for that version.
2. Keep version-specific documentation accurate.
3. Track breaking changes, migration notes, and deprecation status.
4. Approve any emergency patch to already released versions.

## Evolution Rules

1. Do not implement enhancement requests by editing original `v1` directly.
2. Create a new version folder first (for example `v2/`), then implement changes there.
3. Add a version README containing:
   - Version number
   - Owner
   - Scope and behavior
   - Differences from previous version
   - Migration guidance
4. Keep previous versions available for traceability until formally deprecated.

## v1 Protection Rule

- v1 represents the original released implementation.
- v1 can only receive narrowly scoped critical fixes (security or severe production-impact defects), approved by maintainers.
- Any non-critical change must be implemented in the next version.

## Agent Documentation Requirements

Each agent documentation page must include:

- Current version (currently `v1` for all existing agents)
- Link to this versioning strategy document
- Link/path to the version-specific folder structure
- Guidance for creating the next version (`v2+`) without modifying v1

## Pull Request Checklist for New Versions

- [ ] New version folder created under `.github/agents/versions/<agent-name>/vN/`
- [ ] Version README added with owner, scope, and change summary
- [ ] Previous version reference retained
- [ ] Migration guidance from previous version added
- [ ] Central documentation updated (`README.md` and relevant agent guide)
