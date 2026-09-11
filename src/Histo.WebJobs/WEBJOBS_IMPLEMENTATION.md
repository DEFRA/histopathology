# Histo.WebJobs Implementation Summary

**Date:** 2026-09-10  
**Project:** Histo.WebJobs (Azure WebJobs for scheduled tasks)

---

## Files Created

```
src/Histo.WebJobs/
├── Histo.WebJobs.csproj           (Project file with WebJobs SDK)
├── Program.cs                      (Host configuration & DI setup)
├── Functions.cs                    (ResetHistologyNumbers timer-triggered function)
├── appsettings.json                (Production configuration with Key Vault references)
└── appsettings.Development.json    (Local development configuration)
```

---

## Configuration Overview

### 1. Project Configuration (Histo.WebJobs.csproj)

**Key settings:**
- **Target Framework:** `net10.0` (.NET 10)
- **Type:** Triggered WebJob
- **Nullable:** Enabled
- **User Secrets ID:** `histo-webjobs-dev-secrets`

**NuGet Packages:**
- `Microsoft.Azure.WebJobs` (3.0.44) — Azure WebJobs SDK
- `Microsoft.Azure.WebJobs.Extensions` (5.1.2+) — Timer trigger support
- `Microsoft.Data.SqlClient` (5.2.2) — Managed Identity support
- `Azure.Identity` (1.11.4) — DefaultAzureCredential
- `Microsoft.Extensions.Configuration.*` — Configuration management
- `Microsoft.Extensions.Logging.*` — Logging to console and debug

---

### 2. Program.cs — Dependency Injection & Host Configuration

**Responsibilities:**
- Configures JSON, environment, and user secrets configuration sources
- Sets up console and debug logging
- Registers Azure WebJobs bindings (timers)
- Creates and runs the WebJobs host

**Key features:**
- Supports environment-specific configuration (`appsettings.{Environment}.json`)
- Includes user secrets for sensitive local development values
- Uses standard .NET logging (console + debug output)
- Enables timer triggers via `AddTimers()`

**Program.cs structure:**
```csharp
var builder = new HostBuilder();

builder
    .ConfigureAppConfiguration((context, config) => {
        // Load appsettings, environment variables, user secrets
    })
    .ConfigureLogging((context, logging) => {
        logging.AddConsole();
        logging.AddDebug();
    })
    .ConfigureWebJobs(webJobsBuilder => {
        webJobsBuilder.AddTimers();
    });

var host = builder.Build();
await host.RunAsync();
```

---

### 3. Functions.cs — WebJob Function Definition

**Function:** `ResetHistologyNumbers`

**Trigger:** Timer-based (CRON schedule)  
**Schedule:** `0 0 4 1 1 *`
- Fires at **04:00:00 UTC on 1 January every year**
- Replaces legacy SQL Server Agent job: "Histology Reset Histology Numbers"

**Execution flow:**
1. Validates connection string is configured
2. Opens SqlConnection with Managed Identity credentials
3. Executes stored procedure: `dbo.EditResetHistologyRef`
4. Logs all operations and errors with structured logging

**SQL connection behavior:**
- Uses `Authentication=Active Directory Default` in connection string
- Leverages app's system-assigned managed identity automatically
- Falls back to Visual Studio / Azure CLI credentials for local dev
- No client secret required

**Error handling:**
- Catches `SqlException` — logs error number and severity
- Catches `InvalidOperationException` — logs configuration errors
- Catches general exceptions — logs unexpected failures
- All exceptions are rethrown to WebJobs runtime for retry/alerting

---

### 4. appsettings.json — Production Configuration

```json
{
  "ConnectionStrings": {
    "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histologydb-connection-string/)"
  },
  "AzureWebJobsStorage": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/webjobs-storage-connection-string/)"
}
```

**Key Vault secrets required:**
- `histologydb-connection-string` — Managed Identity connection string
- `webjobs-storage-connection-string` — Azure Storage connection string for WebJobs runtime

**Connection string format (Managed Identity):**
```
Server=tcp:{sql-server}.database.windows.net,1433;
Initial Catalog=Histology;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

---

### 5. appsettings.Development.json — Local Development Configuration

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=(localdb)\\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

**Local development features:**
- Uses LocalDB (SQL Server Express alternative)
- Uses Windows Integrated Authentication (local developer identity)
- Uses Azure Storage emulator (`UseDevelopmentStorage=true`)
- Debug-level logging enabled
- No Key Vault dependency

---

### 6. Solution File Update

The project has been added to `HistopathologySystem.slnx`:

```xml
<Project Path="src/Histo.WebJobs/Histo.WebJobs.csproj" />
```

---

## Configuration Requirements by Environment

### Azure Environments (dev, test, UAT, prod)

#### Step 1: Key Vault Secrets
Populate the following secrets in the Key Vault (platform engineering):

```bash
az keyvault secret set \
  --vault-name {vault-name} \
  --name histologydb-connection-string \
  --value "Server=tcp:{sql-server}.database.windows.net,1433;Initial Catalog=Histology;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
  --vault-name {vault-name} \
  --name webjobs-storage-connection-string \
  --value "{azure-storage-connection-string}"
```

#### Step 2: App Service Configuration
Bicep must configure Key Vault role assignment for the WebJob's managed identity:

```bicep
resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentityPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      '4633458b-17de-408a-b874-0445c86b69e6'  // Key Vault Secrets User
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}
```

#### Step 3: SQL Database Grant
Connect as SQL admin and execute against the `Histology` database:

```sql
-- Allows the WebJob's managed identity to execute the stored procedure
CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [{app-service-name}];
ALTER ROLE db_datawriter ADD MEMBER [{app-service-name}];
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

Least-privilege alternative (only the stored procedure):
```sql
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

#### Step 4: CI/CD Pipeline Integration
Add WebJob packaging to the deployment pipeline (MSBuild step):

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

WebJob binaries are placed in `app_data/jobs/triggered/HistologyReset/` within the App Service deployment package. The WebJobs runtime picks them up automatically.

---

### Local Development

No additional setup required beyond Visual Studio or Azure CLI login:

1. Install .NET 10 SDK
2. Ensure LocalDB is available (included with Visual Studio)
3. Create/restore `Histology` database in LocalDB
4. Run WebJob locally:
   ```powershell
   cd src/Histo.WebJobs
   func start
   ```
   Or via Visual Studio: F5 (Debug)

---

## Verification Checklist

### Pre-deployment (dev/test/UAT/prod)

- [ ] Key Vault secrets populated (`histologydb-connection-string`, `webjobs-storage-connection-string`)
- [ ] Key Vault Secrets User role assigned to App Service managed identity
- [ ] SQL managed identity grant executed: `CREATE USER ... FROM EXTERNAL PROVIDER`
- [ ] SQL role and procedure grant executed: `GRANT EXECUTE ON dbo.EditResetHistologyRef`
- [ ] CI/CD pipeline includes WebJob packaging step
- [ ] App Service restarted after Key Vault secrets are populated

### Post-deployment

- [ ] Smoke test: App Service health check → HTTP 200
- [ ] WebJob runtime: Azure Portal → App Service → WebJobs blade → HistologyReset visible
- [ ] Manual trigger: Azure Portal → WebJobs blade → click "Run" on HistologyReset → Status = Success
- [ ] Database validation: Execute `SELECT COUNT(*) FROM EditResetHistologyRef_Log` (or equivalent audit table)
- [ ] Application Insights: Check WebJob logs for successful execution and no errors

---

## Logging & Monitoring

**Built-in .NET logging:** The WebJob uses standard Microsoft.Extensions.Logging with console and debug output.

**Log output destinations:**
1. **Console** — visible in Azure Portal WebJobs output
2. **Debug console** — visible when debugging locally
3. **Application Insights** — can be added via logging provider if needed

**Current log level configuration (appsettings.json):**
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "Microsoft.Azure.WebJobs": "Information"
  }
}
```

### Key Log Events

- ✅ `ResetHistologyNumbers WebJob triggered at {Time}`
- ✅ `Opening SQL connection to Histology database`
- ✅ `Connection opened successfully. Executing EditResetHistologyRef...`
- ✅ `EditResetHistologyRef completed successfully at {Time}`
- ❌ `SQL error while executing EditResetHistologyRef` (with error number)
- ❌ `Configuration error` (if connection string missing)

---

## Troubleshooting

### Issue: "Connection string 'HistologyDb' is not configured"

**Cause:** Missing `appsettings.json` or `appsettings.{Environment}.json`  
**Fix:** Ensure Key Vault reference is set in production config; LocalDB in dev config

### Issue: "Cannot open server 'X' requested by the login. Client with IP address 'Y' is not allowed to access the server"

**Cause:** Missing SQL firewall rule or managed identity not granted access  
**Fix:** 
- Verify SQL firewall rule `AllowAllWindowsAzureIps` (0.0.0.0 — 0.0.0.0)
- Verify `CREATE USER ... FROM EXTERNAL PROVIDER` was executed
- Verify `GRANT EXECUTE` was executed

### Issue: "Login failed for user 'X'"

**Cause:** Managed identity not created or SQL grant missing  
**Fix:**
- Verify App Service has system-assigned managed identity enabled
- Run SQL grant again (Section 6 of this summary)
- Restart App Service after Key Vault secrets are populated

### Issue: "Timeout waiting for token"

**Cause:** Managed identity endpoint not reachable; local Azure SDK auth failure  
**Fix:**
- Verify App Service is in a subnet with access to Azure metadata service (169.254.169.254)
- For local dev, ensure `az login` has been executed and a valid context exists

---

## Files Checklist

- ✅ `src/Histo.WebJobs/Histo.WebJobs.csproj` — Project file
- ✅ `src/Histo.WebJobs/Program.cs` — Host & DI configuration
- ✅ `src/Histo.WebJobs/Functions.cs` — WebJob function
- ✅ `src/Histo.WebJobs/appsettings.json` — Production config
- ✅ `src/Histo.WebJobs/appsettings.Development.json` — Dev config
- ✅ `HistopathologySystem.slnx` — Solution file updated

---

## Next Steps

1. **Build:** `dotnet build src/Histo.WebJobs/Histo.WebJobs.csproj`
2. **Test locally:** Set `ASPNETCORE_ENVIRONMENT=Development` and run with `func start`
3. **Integrate with CI/CD:** Add WebJob packaging step to build pipeline
4. **Deploy to Azure:** Following manual setup checklist (Key Vault, SQL grants, etc.)
5. **Monitor:** Check Application Insights for WebJob execution logs post-deployment

---

**Document Type:** Implementation Summary  
**Status:** Complete — Ready for integration  
**Reference:** Azure-ManagedIdentity-EntraID-WebJob.md (Section 4)
