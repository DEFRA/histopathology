# WebJobs Implementation — Final Checklist

**Date:** 2026-09-10  
**Status:** ✅ COMPLETE — Project structure created and building successfully  
**Build Result:** ✅ SUCCESS  

---

## What Has Been Implemented

### ✅ Files Created

```
src/Histo.WebJobs/
├── Histo.WebJobs.csproj              ✅ Project file (executable, net10.0)
├── Program.cs                        ✅ WebJobs host configuration
├── Functions.cs                      ✅ ResetHistologyNumbers function
├── appsettings.json                  ✅ Production config (Key Vault references)
├── appsettings.Development.json      ✅ Local dev config (LocalDB)
├── WEBJOBS_IMPLEMENTATION.md         ✅ Detailed implementation guide
└── BICEP_INFRASTRUCTURE_REFERENCE.md ✅ Infrastructure setup guide
```

### ✅ Solution File Updated
- `HistopathologySystem.slnx` — Histo.WebJobs project added

### ✅ Documentation Created
- `docs/WebJobs-Quick-Reference.md` — Quick reference for configuration

---

## Build Status

| Component | Status | Notes |
|-----------|--------|-------|
| Histo.WebJobs | ✅ SUCCESS | Compiles without errors |
| Histo.Infrastructure | ✅ SUCCESS | Project dependency resolved |
| Solution file | ✅ UPDATED | WebJobs project added |
| NuGet packages | ✅ RESOLVED | All dependencies compatible |

---

## Configuration Checklist — Environment Setup

### Step 1: Local Development (Developer)

- [ ] Project builds: `dotnet build src/Histo.WebJobs/Histo.WebJobs.csproj`
- [ ] LocalDB available on development machine
- [ ] `appsettings.Development.json` uses LocalDB connection string:
  ```
  Server=(localdb)\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;
  ```
- [ ] Test locally: Run WebJob with `func start` or debug via Visual Studio

### Step 2: Azure Deployment Prerequisites (Platform Engineering)

#### Key Vault Setup
- [ ] Provision Azure Key Vault with RBAC enabled
- [ ] Create secret: `histologydb-connection-string`
  - Value: Managed Identity connection string
- [ ] Create secret: `webjobs-storage-connection-string`
  - Value: Azure Storage connection string

#### App Service Managed Identity
- [ ] App Service has system-assigned managed identity enabled
- [ ] Managed identity Principal ID available
- [ ] Key Vault Secrets User role assigned to managed identity
  - Role ID: `4633458b-17de-408a-b874-0445c86b69e6`

#### SQL Database
- [ ] Azure SQL Database provisioned
- [ ] Firewall rule: `AllowAllWindowsAzureIps` (0.0.0.0 — 0.0.0.0)
- [ ] Stored procedure exists: `dbo.EditResetHistologyRef`
- [ ] TLS minimum version: 1.2

### Step 3: Post-Deployment Database Configuration (DBA)

Execute **once per environment** (dev, test, UAT, prod) against the `Histology` database:

```sql
-- Replace {app-service-name} with the actual Web App resource name
CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [{app-service-name}];
ALTER ROLE db_datawriter ADD MEMBER [{app-service-name}];
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

**Least-privilege alternative** (if only procedure execution is needed):
```sql
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

### Step 4: CI/CD Pipeline Integration

Add to your build/deploy pipeline after the main web app build:

```yaml
- name: Publish WebJob
  run: |
    msbuild "src/Histo.WebJobs/Histo.WebJobs.csproj" `
      /p:Configuration=Release `
      /p:DeployOnBuild=true `
      /p:WebPublishMethod=FileSystem `
      /p:PublishUrl="${{ env.ARTIFACT_DIR }}/app_data/jobs/triggered/HistologyReset" `
      /nologo
```

### Step 5: Bicep Infrastructure (Infrastructure as Code)

Apply the Key Vault and SQL role assignment configurations:

**Reference:** See `src/Histo.WebJobs/BICEP_INFRASTRUCTURE_REFERENCE.md` for:
- Key Vault role assignment module
- SQL firewall rule configuration
- App Service application settings for Key Vault references

**Minimum Bicep additions:**
```bicep
// Key Vault Secrets User role for managed identity
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentityPrincipalId, '4633458b-17de-408a-b874-0445c86b69e6')
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// SQL firewall rule to allow Azure services
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}
```

### Step 6: Post-Deployment Verification

#### Azure Portal Checks
- [ ] App Service → WebJobs blade → HistologyReset visible
- [ ] WebJob status shows as available (not failed)
- [ ] Manual trigger successful: HistologyReset → Run → Status = Success

#### Application Insights Checks (if configured)
- [ ] Query logs for WebJob execution events
- [ ] No error messages in logs
- [ ] Confirm function executed at scheduled time

#### Database Validation
- [ ] Verify managed identity user exists: `SELECT name FROM sys.database_principals WHERE type='E'`
- [ ] Verify stored procedure grant: `SELECT * FROM sys.database_permissions WHERE grantee_principal_id = USER_ID('{app-service-name}')`
- [ ] Manually execute stored procedure to verify it works

---

## Configuration Summary

### appsettings.json — Production

```json
{
  "ConnectionStrings": {
    "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histologydb-connection-string/)"
  },
  "AzureWebJobsStorage": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/webjobs-storage-connection-string/)",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Azure.WebJobs": "Information"
    }
  }
}
```

### appsettings.Development.json — Local Dev

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=(localdb)\\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureWebJobsStorage": "UseDevelopmentStorage=true",
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.Azure.WebJobs": "Debug"
    }
  }
}
```

---

## Managed Identity Connection String

**For Key Vault secret `histologydb-connection-string`:**

```
Server=tcp:{sql-server-name}.database.windows.net,1433;
Initial Catalog=Histology;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

Replace `{sql-server-name}` with your SQL Server hostname (e.g., `dev-histo-sql-uks.database.windows.net`)

---

## WebJob Execution Schedule

| Property | Value |
|----------|-------|
| **Function** | `ResetHistologyNumbers` |
| **CRON Schedule** | `0 0 4 1 1 *` |
| **Day** | 1 January every year |
| **Time** | 04:00:00 UTC |
| **Time Zone** | UTC (no daylight saving adjustment) |
| **Stored Procedure** | `dbo.EditResetHistologyRef` |
| **Timeout** | 60 seconds |

---

## NuGet Dependencies

**Final package list (verified to build successfully):**

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Azure.WebJobs | 3.0.44 | WebJobs SDK |
| Microsoft.Azure.WebJobs.Extensions | 5.1.2+ | Timer triggers |
| Microsoft.Data.SqlClient | 5.2.2 | Database with Managed Identity |
| Azure.Identity | 1.11.4 | Token provider |
| Microsoft.Extensions.Configuration.* | 10.0.0 | Configuration |
| Microsoft.Extensions.Logging.* | 10.0.0 | Logging |

---

## Testing & Validation

### Local Testing

```powershell
# Navigate to WebJobs project
cd src/Histo.WebJobs

# Build
dotnet build

# Run WebJob host
func start
```

Access: `http://localhost:7071/admin/functions/ResetHistologyNumbers` to view function and trigger manually.

### Azure Testing

1. **Deploy** the web app (includes WebJobs)
2. **Navigate** to App Service → WebJobs in Azure Portal
3. **Click** on HistologyReset → Run
4. **Monitor** the execution and check logs
5. **Verify** stored procedure was executed in database

### Expected Log Output

```
[INFO] ResetHistologyNumbers WebJob triggered at {timestamp}. Next execution scheduled: {next-run}
[INFO] Opening SQL connection to Histology database...
[INFO] Connection opened successfully. Executing EditResetHistologyRef...
[INFO] EditResetHistologyRef completed successfully at {timestamp}
```

---

## Troubleshooting Guide

### Issue: "Connection string 'HistologyDb' is not configured"

**Cause:** Missing appsettings.json or incorrect environment  
**Fix:** Verify `appsettings.json` exists and Key Vault reference is set  
**Verify:** Check App Service application settings include `ConnectionStrings__HistologyDb`

### Issue: "Cannot open server 'X' requested by the login"

**Cause:** Managed identity SQL grant missing  
**Fix:** Execute SQL grant (Section Step 3) as database admin  
**Verify:** Run `SELECT USER_NAME({principal-id})` in SQL

### Issue: "Timeout waiting for token"

**Cause:** Managed identity endpoint unreachable  
**Fix:** Verify App Service has system-assigned identity enabled  
**Verify:** Check Key Vault role assignment exists

### Issue: WebJob not triggering at scheduled time

**Cause:** App Service not running; WebJob runtime issue  
**Fix:** Check App Service is started; restart if needed  
**Verify:** Check WebJob logs in Azure Portal

---

## Documentation References

| Document | Location | Purpose |
|----------|----------|---------|
| Implementation Details | [src/Histo.WebJobs/WEBJOBS_IMPLEMENTATION.md](../src/Histo.WebJobs/WEBJOBS_IMPLEMENTATION.md) | Comprehensive implementation guide |
| Infrastructure Setup | [src/Histo.WebJobs/BICEP_INFRASTRUCTURE_REFERENCE.md](../src/Histo.WebJobs/BICEP_INFRASTRUCTURE_REFERENCE.md) | Bicep examples and role assignments |
| Quick Reference | [docs/WebJobs-Quick-Reference.md](./WebJobs-Quick-Reference.md) | Configuration summary |
| Original Design | [Azure-ManagedIdentity-EntraID-WebJob.md](./Azure-ManagedIdentity-EntraID-WebJob.md) | Full technical design (Section 4) |

---

## Next Steps

1. **Immediate:** Share this checklist with Platform Engineering for infrastructure setup
2. **Short-term:** Integrate CI/CD pipeline step (WebJob publishing)
3. **Pre-deployment:** Complete all infrastructure setup (Key Vault, SQL, Bicep)
4. **Deployment:** Deploy to dev environment and test
5. **Validation:** Verify all checklist items before production rollout

---

## Sign-Off

- **Project:** Histo.WebJobs (Histology Reset Scheduler)
- **Status:** ✅ Implementation Complete
- **Build Status:** ✅ Success
- **Ready for:** Infrastructure setup and deployment
- **Owner:** Development Team
- **Date Completed:** 2026-09-10

---

**For Questions:** Refer to the detailed implementation guide or architecture documentation.
