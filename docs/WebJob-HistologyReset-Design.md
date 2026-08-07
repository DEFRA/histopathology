# WebJob Design — Histology Reset Histology Numbers

**Document type:** Architectural Decision + Implementation Reference  
**Date:** 2026-08-03  
**Author:** GitHub Copilot (Run #53)  
**Status:** Approved — pending deployment

---

## 1. Legacy SQL Agent Job Definition

| Property | Value |
|---|---|
| Job name | Histology Reset Histology Numbers |
| Schedule | Annually — 1 January, 04:00 AM |
| Database | Histology |
| Command | `EXECUTE EditResetHistologyRef` |
| Owner | SQL Server Agent |
| Notes | Resets histology reference numbers at the start of each calendar year |

---

## 2. Decision Matrix — Azure WebJob vs Function App

| Criterion | Azure WebJob | Azure Function App |
|---|---|---|
| **Trigger type** | Triggered WebJob with NCrontab schedule | Timer trigger |
| **Hosting** | Runs inside existing App Service (Histo.Web) | Requires separate Function App resource |
| **Cost** | No additional Azure resource cost | Additional Consumption or Premium plan cost |
| **Scaling** | Shares App Service plan — adequate for single annual SP call | Independent scaling — unnecessary for this workload |
| **Deployment** | Part of existing CI/CD pipeline | Requires separate pipeline job |
| **Complexity** | Low — single project, single function | Medium — new resource, new pipeline, new managed identity config |
| **Monitoring** | App Service log streaming + Application Insights | Application Insights (requires explicit integration) |
| **Cold start** | None — always-on App Service | Possible on Consumption plan |
| **Suitability** | ✅ Ideal for simple, infrequent scheduled tasks | Preferred for high-throughput, independently scalable workloads |

**Verdict: Azure WebJob**

The annual SP call is trivial in workload. A WebJob reuses the existing App Service plan at zero extra cost, shares the same managed identity, and is deployed as part of the existing pipeline. A Function App would introduce a new Azure resource, a new pipeline stage, and additional operational overhead for a job that runs once per year and completes in milliseconds.

---

## 3. New Project Structure

```
src/
  Histo.WebJobs/
    Histo.WebJobs.csproj
    Program.cs
    Functions.cs
    appsettings.json
    appsettings.Development.json
```

Add `Histo.WebJobs` to `HistopathologySystem.slnx`.

---

## 4. `Histo.WebJobs.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Azure.WebJobs.Extensions" Version="5.*" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.*" />
  </ItemGroup>

</Project>
```

---

## 5. `Program.cs`

```csharp
using Microsoft.Extensions.Hosting;

var builder = new HostBuilder()
    .ConfigureWebJobs(b =>
    {
        b.AddAzureStorageCoreServices();
        b.AddTimers();
    })
    .ConfigureLogging((context, b) =>
    {
        b.AddConsole();
    });

await builder.RunAsync();
```

---

## 6. `Functions.cs`

```csharp
using Microsoft.Azure.WebJobs;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class Functions
{
    private readonly IConfiguration _config;

    public Functions(IConfiguration config)
    {
        _config = config;
    }

    // NCrontab 6-field: {second} {minute} {hour} {day} {month} {day-of-week}
    // 0 0 4 1 1 *  =  04:00:00 on 1 January every year
    [FunctionName("ResetHistologyNumbers")]
    public async Task ResetHistologyNumbersAsync(
        [TimerTrigger("0 0 4 1 1 *")] TimerInfo timer,
        ILogger log)
    {
        log.LogInformation("ResetHistologyNumbers triggered at {Time}", DateTimeOffset.UtcNow);

        var connectionString = _config.GetConnectionString("HistologyDb")
            ?? throw new InvalidOperationException("HistologyDb connection string is not configured.");

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("EXECUTE EditResetHistologyRef", connection)
        {
            CommandTimeout = 60
        };

        await command.ExecuteNonQueryAsync();

        log.LogInformation("EditResetHistologyRef completed successfully.");
    }
}
```

---

## 7. Cron Expression Explanation

| Field | Value | Meaning |
|---|---|---|
| Second | `0` | At second 0 |
| Minute | `0` | At minute 0 |
| Hour | `4` | At 04:00 |
| Day of month | `1` | On the 1st |
| Month | `1` | In January |
| Day of week | `*` | Any day of week |

**Result:** Fires once annually at 04:00:00 UTC on 1 January.

**UTC note:** The UK is UTC+0 in January (no daylight saving). 04:00 UTC exactly matches the legacy SQL Agent schedule.

---

## 8. Configuration Files

### `appsettings.json`

```json
{
  "ConnectionStrings": {
    "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histology-db-connection-string/)"
  },
  "AzureWebJobsStorage": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/webjobs-storage-connection-string/)"
}
```

### `appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=localhost;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

---

## 9. Production — Managed Identity Connection String

Replace the `HistologyDb` Key Vault secret value with a managed identity connection string that carries no credentials:

```
Server=tcp:{sql-server-name}.database.windows.net,1433;
Initial Catalog=Histology;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

The App Service system-assigned managed identity must be granted a SQL login inside the Histology database (see post-deployment SQL grant below).

---

## 10. CI/CD Pipeline Step

WebJob triggered artefacts must be published to `app_data/jobs/triggered/{job-name}/` within the Web App deployment package. Add the following MSBuild publish step to the existing pipeline after the main web package is built:

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

The WebJob binaries placed under `app_data/jobs/triggered/HistologyReset/` are picked up automatically by the App Service WebJobs runtime when the main web package is deployed.

---

## 11. Post-Deployment Verification

### SQL managed identity grant (run once per environment against Histology database)

```sql
-- Connect as SQL admin account
CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

Scope the grant to the specific stored procedure rather than the full `db_datareader`/`db_datawriter` roles — the WebJob only needs `EXECUTE` on this one object.

### Manual trigger test

In the Azure Portal → App Service → WebJobs blade, select `HistologyReset` and click **Run**. Verify:

1. The WebJob status changes to `Running` then `Success`.
2. App Service logs show `EditResetHistologyRef completed successfully.`
3. The relevant histology reference rows in the database have been reset.

---

## 12. Monitoring Alerts

| Alert | Condition | Action |
|---|---|---|
| WebJob failure | WebJob status = `Failed` | Alert on-call; inspect log stream |
| Missing execution | No execution logged by 05:00 on 1 January | Alert on-call; trigger manually if missed |
| SP timeout | `CommandTimeout` exceeded (60 s) | Review SP execution plan; index audit |

Configure alerts via Application Insights custom log queries or the App Service WebJobs dashboard.

---

## 13. Open Items

| # | Item | Owner | Priority |
|---|---|---|---|
| 1 | Populate Key Vault secrets (`histology-db-connection-string`, `webjobs-storage-connection-string`) | Platform Engineering | Before first deploy |
| 2 | Run SQL managed identity grant against each environment's Histology database | DBA | Before first deploy |
| 3 | Add `Histo.WebJobs` to `HistopathologySystem.slnx` solution | Dev | Before CI/CD step |
| 4 | Confirm `EditResetHistologyRef` SP behaviour — does it handle idempotent re-runs safely? | Dev / DBA | Before UAT |
| 5 | Configure missing-execution alert in Application Insights | Platform Engineering | Before production go-live |
