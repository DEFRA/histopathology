# Database Scripts

Two SQL scripts are required to be run against any target environment (dev, test, staging, production) before the migrated application can function correctly.

---

## 001_create_histologyuser_login.sql

**Run as:** sysadmin / sa on the target SQL Server instance.

The production database (restored from the bacpac) contains a `HistologyUser` database user that is orphaned — it exists in the database but is not linked to any server-level login. The legacy application connected via Windows integrated authentication, so this was never an issue. The migrated application connects via SQL authentication using the `HistologyUser` credentials.

This script:
1. Creates the `HistologyUser` server-level login if it does not already exist.
2. Remaps the orphaned database user to that login so SQL authentication works correctly.

Without this script the application will fail to connect to the database on any environment where the SQL login has not been previously set up.

---

## 008_grant_permissions.sql

**Run as:** db_owner or security admin on the Histology database. Must be run after `001`.

The bacpac contains no explicit EXECUTE grants for `HistologyUser` because the legacy application used Windows integrated authentication and relied on Active Directory group membership for database access rather than per-user SP grants.

The migrated application uses SQL authentication via `HistologyUser`, which has no inherited AD group permissions. Two stored procedures are called by the migrated app that are legacy SPs (no new objects are created):

| SP | Called by |
|---|---|
| `GetBatchesWithStatus` | `BatchRepository` — all batch list pages (not received, on hold, in progress, for editing, completed) |
| `GetBatchBlocksByID` | `BlockTestRepository` — QualityData page (loads block-level test rows) |

This script grants EXECUTE on both SPs to `HistologyUser`. Without it the application will receive a permissions error when navigating to any batch list page or the QualityData page.

---

## Run order

```
001_create_histologyuser_login.sql   ← run first (requires sysadmin)
008_grant_permissions.sql            ← run second (requires db_owner)
```
