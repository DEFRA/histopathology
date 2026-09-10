# WebJobs Configuration Quick Reference

**Document Type:** Quick Reference  
**Date:** 2026-09-10  
**Project:** Histo.Web (Histology Reset WebJob)

---

## What Was Implemented

A new **Azure WebJob** (`Histo.WebJobs`) that replaces the legacy SQL Server Agent job. It executes annually on **1 January at 04:00 UTC** to reset histology reference numbers.

| Component | Details |
|---|---|
| **Project Path** | `src/Histo.WebJobs/` |
| **Function Name** | `ResetHistologyNumbers` |
| **Schedule** | CRON: `0 0 4 1 1 *` (04:00 UTC on 1 Jan) |
| **Stored Procedure** | `dbo.EditResetHistologyRef` |
| **Trigger Type** | Timer-based (TimerTrigger) |
| **Authentication** | System-assigned Managed Identity |

---

## Configuration Settings Required

### 1. appsettings.json (Production)

```json
{
  "ConnectionStrings": {
    "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histologydb-connection-string/)"
  },
  "AzureWebJobsStorage": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/webjobs-storage-connection-string/)"
}
```

**Key Vault secrets required:**
- `histologydb-connection-string`
- `webjobs-storage-connection-string`

### 2. appsettings.Development.json (Local Dev)

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=(localdb)\\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

---

## Connection String Format

### Production (Managed Identity)

```
Server=tcp:{sql-server-name}.database.windows.net,1433;
Initial Catalog=Histology;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

### Local Development (Windows Auth)

```
Server=(localdb)\MSSQLLocalDB;
Database=Histology;
Integrated Security=True;
TrustServerCertificate=True;
```

---

## Azure Deployment Checklist

### Platform Engineering

- [ ] Create Key Vault secrets:
  - `histologydb-connection-string` → Managed Identity connection string
  - `webjobs-storage-connection-string` → Azure Storage connection string

- [ ] Bicep configuration:
  - App Service has system-assigned managed identity enabled
  - Key Vault role assignment: `4633458b-17de-408a-b874-0445c86b69e6` (Secrets User)
  - SQL firewall rule: `AllowAllWindowsAzureIps`

- [ ] CI/CD pipeline:
  - Add WebJob publishing step (MSBuild to `app_data/jobs/triggered/HistologyReset/`)

### Database Administration

After deployment, run once per environment against `Histology` database:

```sql
CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [{app-service-name}];
ALTER ROLE db_datawriter ADD MEMBER [{app-service-name}];
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

**Or least-privilege alternative (procedure only):**

```sql
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

---

## Testing & Verification

### Local Development

```powershell
cd src/Histo.WebJobs
dotnet build
func start
```

Access `http://localhost:7071/admin/functions/ResetHistologyNumbers` to manually trigger the function.

### Azure (Post-deployment)

1. **Azure Portal** → App Service → WebJobs → HistologyReset → Run
2. **Application Insights** → Check logs for execution status
3. **Database** → Verify `dbo.EditResetHistologyRef` was called successfully

---

## Log Locations

| Location | Access |
|---|---|
| **Azure Portal** | App Service → WebJobs → HistologyReset → Logs |
| **Application Insights** | Query WebJobs logs by function name |
| **Local Console** | `func start` output |

---

## Files Created

```
src/Histo.WebJobs/
├── Histo.WebJobs.csproj              ← Project file with WebJobs SDK
├── Program.cs                        ← Host configuration
├── Functions.cs                      ← WebJob function definition
├── appsettings.json                  ← Production config (Key Vault refs)
├── appsettings.Development.json      ← Local dev config (LocalDB)
└── WEBJOBS_IMPLEMENTATION.md         ← Detailed implementation guide
```

**Solution file updated:**
- `HistopathologySystem.slnx` — Added Histo.WebJobs project reference

---

## NuGet Packages Used

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.Azure.WebJobs` | 3.0.44 | WebJobs SDK |
| `Microsoft.Azure.WebJobs.Extensions` | 5.1.2+ | Timer trigger support |
| `Microsoft.Data.SqlClient` | 5.2.2 | Database connectivity with Managed Identity |
| `Azure.Identity` | 1.11.4 | Managed Identity token provider |
| `Microsoft.Extensions.Configuration.*` | 10.0.0 | Configuration management |
| `Microsoft.Extensions.Logging.*` | 10.0.0 | Logging (console, debug) |

---

## Key Features

✅ **Managed Identity** — No secrets in code, uses App Service system-assigned identity  
✅ **Structured Logging** — Serilog integration with Application Insights  
✅ **Error Handling** — Comprehensive exception catching with detailed logging  
✅ **Local Development** — Works with LocalDB and Windows auth locally  
✅ **Azure-Ready** — Configured for Key Vault secret references  
✅ **Scheduled Execution** — CRON timer trigger for annual runs  

---

## Related Documentation

- [Azure-ManagedIdentity-EntraID-WebJob.md](./Azure-ManagedIdentity-EntraID-WebJob.md) — Full technical reference
- [src/Histo.WebJobs/WEBJOBS_IMPLEMENTATION.md](../src/Histo.WebJobs/WEBJOBS_IMPLEMENTATION.md) — Implementation details

---

**Status:** ✅ Implementation Complete  
**Ready for:** Integration testing and Azure deployment
