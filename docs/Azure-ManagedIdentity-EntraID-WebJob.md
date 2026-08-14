# Azure Managed Identity, Entra ID & WebJob — Implementation Reference

**Document type:** Implementation Reference  
**Date:** 2026-08-04  
**Application:** Histo.Web (ASP.NET Core Razor Pages, .NET 10)  
**Status:** Phase 2 — pending implementation

---

## Table of Contents

1. [Overview](#1-overview)
2. [Managed Identity — DB CRUD](#2-managed-identity--db-crud)
   - 2.1 [How it works with SqlConnectionFactory](#21-how-it-works-with-sqlconnectionfactory)
   - 2.2 [Connection string changes](#22-connection-string-changes)
   - 2.3 [Post-deployment SQL grant](#23-post-deployment-sql-grant)
   - 2.4 [Local development](#24-local-development)
3. [Entra ID Authentication — SAML 2.0](#3-entra-id-authentication--saml-20)
   - 3.1 [NuGet packages](#31-nuget-packages)
   - 3.2 [appsettings.json additions](#32-appssettingsjson-additions)
   - 3.3 [Program.cs — Phase 2 changes](#33-programcs--phase-2-changes)
   - 3.4 [Claims normalisation layer](#34-claims-normalisation-layer)
   - 3.5 [Page-level authorization](#35-page-level-authorization)
   - 3.6 [Key Vault secrets for Entra ID](#36-key-vault-secrets-for-entra-id)
   - 3.7 [Enterprise Application setup — manual steps](#37-enterprise-application-setup--manual-steps)
4. [WebJob — Histology Reset](#4-webjob--histology-reset)
   - 4.1 [Project structure](#41-project-structure)
   - 4.2 [Managed Identity for WebJob SQL access](#42-managed-identity-for-webjob-sql-access)
   - 4.3 [Cron schedule](#43-cron-schedule)
   - 4.4 [CI/CD pipeline step](#44-cicd-pipeline-step)
   - 4.5 [Post-deployment SQL grant for WebJob](#45-post-deployment-sql-grant-for-webjob)
5. [Bicep infrastructure changes](#5-bicep-infrastructure-changes)
6. [Key Vault secrets summary](#6-key-vault-secrets-summary)
7. [Manual steps checklist](#7-manual-steps-checklist)
8. [Open items](#8-open-items)

---

## 1. Overview

This document covers three related Azure integration tasks for Histo.Web:

| Topic | What changes | Code changes required |
|---|---|---|
| Managed Identity — DB CRUD | Connection string only; no `SqlConnectionFactory` code changes | No |
| Entra ID authentication | New NuGet packages, `appsettings.json`, `Program.cs` Phase 2 activation | Yes |
| WebJob — Histology Reset | New `Histo.WebJobs` project; replaces legacy SQL Agent job | Yes (new project) |

All three share the same App Service system-assigned managed identity. No separate service principal or client secret is needed for database access.

---

## 2. Managed Identity — DB CRUD

### 2.1 How it works with SqlConnectionFactory

`SqlConnectionFactory.CreateConnection()` constructs a `new SqlConnection(_connectionString)`. When the connection string contains `Authentication=Active Directory Default`, `Microsoft.Data.SqlClient` calls the `DefaultAzureCredential` token chain automatically at connection-open time:

- **On App Service:** picks up the system-assigned managed identity token via the App Service token endpoint.
- **Locally (dev):** falls through to Visual Studio / Azure CLI logged-in credentials.

**No changes to `SqlConnectionFactory.cs` or `IDbConnectionFactory` are required.** The only change is the connection string value.

### 2.2 Connection string changes

#### `appsettings.json` (base — non-dev environments)

Replace the current hardcoded credential string with a Key Vault reference placeholder. The placeholder is resolved at runtime by App Service from Key Vault:

```json
"ConnectionStrings": {
  "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histologydb-connection-string/)"
}
```

The Key Vault secret value (see [Section 6](#6-key-vault-secrets-summary)) must be set to the managed identity connection string:

```
Server=tcp:{sql-server-name}.database.windows.net,1433;
Initial Catalog=Histology;
Authentication=Active Directory Default;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
```

> **Security note:** The current `appsettings.json` contains a plaintext `Password=` value. Remove this before merging Phase 2. The credential must not appear in any committed file.

#### `appsettings.Development.json` (local dev — create this file)

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=(localdb)\\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

Add `appsettings.Development.json` to `.gitignore` if it contains any developer-specific values beyond the above.

### 2.3 Post-deployment SQL grant

This step cannot be automated in Bicep — it must be run manually against each environment's database after the App Service is provisioned.

Connect as the SQL admin account and run against the `Histology` database:

```sql
-- Replace {app-service-name} with the Web App resource name (e.g. acme-histo-app-dev-uks)
CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [{app-service-name}];
ALTER ROLE db_datawriter ADD MEMBER [{app-service-name}];
GRANT EXECUTE TO [{app-service-name}];
```

Run once per environment (dev → test → UAT → prod) **before** smoke tests are executed. If this step is missing, the application produces SQL login-failure errors that look identical to a misconfigured connection string.

### 2.4 Local development

| Scenario | Connection string to use | Auth mechanism |
|---|---|---|
| LocalDB | `Integrated Security=True` (in `appsettings.Development.json`) | Windows Identity |
| Azure SQL dev instance | `Authentication=Active Directory Default` | Azure CLI `az login` or Visual Studio account |

No additional NuGet packages are needed — `Microsoft.Data.SqlClient` is already referenced via `Histo.Infrastructure`.

---

## 3. Entra ID Authentication — SAML 2.0

The authentication protocol is **SAML 2.0**, not OIDC. Entra ID acts as the Identity Provider (IdP); Histo.Web is the Service Provider (SP). No client secret is required — assertion integrity is verified using the IdP's signing certificate loaded from the Entra ID federation metadata endpoint.

### 3.1 NuGet packages

Add to `src/Histo.Web/Histo.Web.csproj`:

```xml
<PackageReference Include="Sustainsys.Saml2.AspNetCore2" Version="2.*" />
```

Run from the solution root:

```powershell
dotnet add src/Histo.Web/Histo.Web.csproj package Sustainsys.Saml2.AspNetCore2
```

`Sustainsys.Saml2.AspNetCore2` registers three endpoints automatically — no dedicated controller or Razor Page is needed:

| Endpoint | Purpose |
|---|---|
| `GET /Saml2` | SP metadata (used when configuring Entra ID Enterprise Application) |
| `POST /Saml2/Acs` | Assertion Consumer Service — receives SAML assertion from Entra ID |
| `GET /Saml2/Logout` | Single Logout (SLO) initiation and response handling |

### 3.2 appsettings.json additions

Add the `Saml2` section. Use placeholder tokens — `TenantId` is stored in Key Vault and injected as an App Service application setting (see [Section 3.6](#36-key-vault-secrets-for-entra-id)).

**Dev environment** (confirmed DEFRA hosting URLs — 2026-08-14):

```json
{
  "Saml2": {
    "EntityId": "https://dev-cde.azure.defra.cloud",
    "ReturnUrl": "https://dev-cde.azure.defra.cloud/",
    "TenantId": "__TENANT_ID__"
  }
}
```

**Production / other environments** (replace hostname when confirmed):

```json
{
  "Saml2": {
    "EntityId": "https://{app-service-name}.azurewebsites.net/Saml2",
    "ReturnUrl": "https://{app-service-name}.azurewebsites.net/",
    "TenantId": "__TENANT_ID__"
  }
}
```

- `EntityId` — the SP identifier registered in the Entra ID Enterprise Application. Must be unique per environment.
- `ReturnUrl` — where the user is sent after successful authentication.
- `TenantId` — Entra ID Directory (tenant) GUID. Used to construct the IdP entity ID and federation metadata URL.

For local development, override in `appsettings.Development.json`:

```json
{
  "Saml2": {
    "EntityId": "https://localhost:{port}/Saml2",
    "ReturnUrl": "https://localhost:{port}/",
    "TenantId": "__TENANT_ID__"
  }
}
```

### 3.3 Program.cs — Phase 2 changes

Replace the `// Phase 2` comments in `Program.cs` with the following.

**Usings** — add at the top of `Program.cs`:

```csharp
using Sustainsys.Saml2;
using Sustainsys.Saml2.AspNetCore2;
using Sustainsys.Saml2.Metadata;
using System.Security.Claims;
```

**DI registrations** — add after `builder.Services.AddRazorPages()`:

```csharp
// Phase 2: Entra ID authentication via SAML 2.0
var samlTenantId = builder.Configuration["Saml2:TenantId"]
    ?? throw new InvalidOperationException("Saml2:TenantId is not configured.");
var samlEntityId = builder.Configuration["Saml2:EntityId"]
    ?? throw new InvalidOperationException("Saml2:EntityId is not configured.");

builder.Services.AddAuthentication(defaultScheme: Saml2Defaults.Scheme)
    .AddSaml2(options =>
    {
        options.SPOptions.EntityId = new EntityId(samlEntityId);

        if (Uri.TryCreate(builder.Configuration["Saml2:ReturnUrl"], UriKind.Absolute, out var returnUri))
            options.SPOptions.ReturnUrl = returnUri;

        options.IdentityProviders.Add(
            new IdentityProvider(
                new EntityId($"https://sts.windows.net/{samlTenantId}/"),
                options.SPOptions)
            {
                // Entra ID federation metadata — certificate loaded automatically
                MetadataLocation = $"https://login.microsoftonline.com/{samlTenantId}/federationmetadata/2007-06/federationmetadata.xml",
                LoadMetadata = true,
                AllowUnsolicitedAuthnResponse = false  // require SP-initiated sign-in only
            });
    });

// Phase 2: Centralised claims normalisation (per auth requirements)
builder.Services.AddScoped<IClaimsTransformation, SamlClaimsTransformer>();
```

**Middleware pipeline** — replace the `// Phase 2: app.UseAuthentication(); app.UseAuthorization();` comment:

```csharp
app.UseAuthentication();   // must be between UseRouting() and MapRazorPages()
app.UseAuthorization();
```

The complete middleware order must be:

```csharp
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();   // ← Phase 2 addition
app.UseAuthorization();    // ← Phase 2 addition
app.MapHealthChecks("/health");
app.MapRazorPages();
```

### 3.4 Claims normalisation layer

All incoming SAML claims must flow through a single normalisation class, as required by the auth instructions. Create `src/Histo.Web/Auth/SamlClaimsTransformer.cs`:

```csharp
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Histo.Web.Auth;

/// <summary>
/// Normalises Entra ID SAML 2.0 attribute claims into canonical internal claims.
/// All claim reading must go through this class — never parse SAML claim URIs inline.
/// </summary>
public sealed class SamlClaimsTransformer : IClaimsTransformation
{
    // Entra ID SAML attribute URIs
    private const string SamlName      = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
    private const string SamlUpn       = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn";
    private const string SamlEmail     = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress";
    private const string SamlGroups    = "http://schemas.microsoft.com/ws/2008/06/identity/claims/groups";
    private const string SamlObjectId  = "http://schemas.microsoft.com/identity/claims/objectidentifier";

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        // Canonical name: prefer UPN → name → email
        var canonicalName =
            principal.FindFirst(SamlUpn)?.Value ??
            principal.FindFirst(SamlName)?.Value ??
            principal.FindFirst(SamlEmail)?.Value;

        if (canonicalName is not null &&
            !identity.HasClaim(ClaimTypes.Name, canonicalName))
        {
            identity.AddClaim(new Claim(ClaimTypes.Name, canonicalName));
        }

        // Map Entra ID group Object IDs to ASP.NET Core role claims
        // Groups must be configured to emit in the Entra ID Enterprise Application
        foreach (var groupClaim in principal.FindAll(SamlGroups))
        {
            if (!identity.HasClaim(ClaimTypes.Role, groupClaim.Value))
                identity.AddClaim(new Claim(ClaimTypes.Role, groupClaim.Value));
        }

        return Task.FromResult(principal);
    }
}
```

> **Never** read `http://schemas.xmlsoap.org/...` URIs directly in page models or services. Always read the canonical `ClaimTypes.Name` / `ClaimTypes.Role` values that this transformer produces.

### 3.5 Page-level authorization

**Option A — authorize the entire application** (recommended for an internal system):

```csharp
builder.Services.AddRazorPages(o =>
{
    o.Conventions.AuthorizeFolder("/");               // require login for all pages
    o.Conventions.AllowAnonymousToPage("/Index");     // allow unauthenticated access to landing page
    o.Conventions.AllowAnonymousToPage("/Error");     // allow unauthenticated access to error page
});
```

**Option B — per-page `[Authorize]` attribute** (selective, for gradual rollout):

```csharp
[Authorize]
public class SubmissionsModel : PageModel { ... }
```

**Option C — role-based authorization using Entra ID group Object IDs** (mapped to roles by `SamlClaimsTransformer`):

```csharp
[Authorize(Roles = "{entra-group-object-id}")]
public class AdminModel : PageModel { ... }
```

Groups must be configured to emit in the SAML token — see Section 3.7.

**Sign-in / sign-out in the layout** — trigger the SAML flow from a Razor Page or layout:

```csharp
// Sign in (Razor Page action method)
public IActionResult OnGetSignIn()
    => Challenge(new AuthenticationProperties { RedirectUri = "/" }, Saml2Defaults.Scheme);

// Sign out (clears both the app cookie and initiates SAML SLO)
public IActionResult OnGetSignOut()
    => SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        Saml2Defaults.Scheme,
        CookieAuthenticationDefaults.AuthenticationScheme);
```

### 3.6 Key Vault secrets for Entra ID

SAML 2.0 does **not** require a client secret. The IdP's signing certificate is loaded automatically from the Entra ID federation metadata URL. The only value that needs to be injected from Key Vault is the tenant ID.

| Key Vault secret name | Value | App Service app setting name |
|---|---|---|
| `saml2-tenant-id` | Entra ID Directory (tenant) GUID | `Saml2__TenantId` |

App Service application setting format (Key Vault reference):

```
Saml2__TenantId = @Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/saml2-tenant-id/)
```

> **Double underscore `__`** maps `Saml2__TenantId` to `Saml2:TenantId` in `IConfiguration`.

The `EntityId` and `ReturnUrl` values are not secrets and can be stored as plain App Service application settings.

### 3.7 Enterprise Application setup — manual steps

SAML 2.0 uses an **Enterprise Application** in Entra ID, not an OAuth App Registration. These steps must be performed in the Azure Portal and cannot be automated by Bicep.

> **Confirmed DEFRA hosting URLs (dev environment — 2026-08-14):**
> - Entity ID: `https://dev-cde.azure.defra.cloud`
> - ACS URL: `https://dev-cde.azure.defra.cloud/Saml2/Acs`
>
> These confirmed values must be used for all Entra ID Enterprise App configuration and in `appsettings.json` / Bicep for the dev environment. Production URLs will follow the same pattern with the prod hostname.

1. Navigate to **Entra ID → Enterprise applications → New application → Create your own application**.
2. **Name:** `Histo.Web {environment}` → select **Integrate any other application you don't find in the gallery** → Create.
3. Navigate to **Single sign-on → SAML**.
4. Configure the **Basic SAML Configuration**:

   | Field | Value |
   |---|---|
   | Identifier (Entity ID) | `https://{app-service-name}.azurewebsites.net/Saml2` |
   | Reply URL (ACS URL) | `https://{app-service-name}.azurewebsites.net/Saml2/Acs` |
   | Sign-on URL | `https://{app-service-name}.azurewebsites.net/` |
   | Logout URL | `https://{app-service-name}.azurewebsites.net/Saml2/Logout` |
   | Relay State | *(leave blank)* |

5. Under **Attributes & Claims**, configure the claims emitted in the SAML token:

   | Claim name | Source attribute |
   |---|---|
   | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name` | `user.userprincipalname` |
   | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress` | `user.mail` |
   | `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn` | `user.userprincipalname` |
   | `http://schemas.microsoft.com/ws/2008/06/identity/claims/groups` | *(add Group claim — All Groups or specific Security Groups)* |

6. Under **SAML Signing Certificate**, download the **Federation Metadata XML** URL — confirm it matches `https://login.microsoftonline.com/{tenant-id}/federationmetadata/2007-06/federationmetadata.xml`.
7. Under **Users and groups**, assign the relevant users or groups access to the application.
8. Note the **Tenant ID** from the Entra ID overview blade — store in Key Vault as `saml2-tenant-id`.

> **Local dev:** Register a separate Enterprise Application with ACS URL `https://localhost:{port}/Saml2/Acs` and Entity ID `https://localhost:{port}/Saml2`. Use the same tenant but a separate registration.

---

## 4. WebJob — Histology Reset

The WebJob replaces the legacy SQL Server Agent job (`Histology Reset Histology Numbers`) which runs `EXECUTE EditResetHistologyRef` annually on 1 January at 04:00.

Full design detail is in [docs/WebJob-HistologyReset-Design.md](WebJob-HistologyReset-Design.md). This section summarises the Managed Identity integration specifically.

### 4.1 Project structure

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

### 4.2 Managed Identity for WebJob SQL access

The WebJob runs inside the Histo.Web App Service and **shares the same system-assigned managed identity** as the web application. No separate identity or App Registration is needed.

`Functions.cs` uses `Authentication=Active Directory Default` in the connection string — identical to the Histo.Web pattern:

```csharp
[FunctionName("ResetHistologyNumbers")]
public async Task ResetHistologyNumbersAsync(
    [TimerTrigger("0 0 4 1 1 *")] TimerInfo timer,
    ILogger log)
{
    log.LogInformation("ResetHistologyNumbers triggered at {Time}", DateTimeOffset.UtcNow);

    var connectionString = _config.GetConnectionString("HistologyDb")
        ?? throw new InvalidOperationException("HistologyDb connection string is not configured.");

    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();                    // token acquired here via DefaultAzureCredential

    await using var command = new SqlCommand("EXECUTE EditResetHistologyRef", connection)
    {
        CommandTimeout = 60
    };

    await command.ExecuteNonQueryAsync();
    log.LogInformation("EditResetHistologyRef completed successfully.");
}
```

**WebJob `appsettings.json`** — same Key Vault reference pattern as `Histo.Web`:

```json
{
  "ConnectionStrings": {
    "HistologyDb": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/histologydb-connection-string/)"
  },
  "AzureWebJobsStorage": "@Microsoft.KeyVault(SecretUri=https://{vault-name}.vault.azure.net/secrets/webjobs-storage-connection-string/)"
}
```

**WebJob `appsettings.Development.json`** (local dev):

```json
{
  "ConnectionStrings": {
    "HistologyDb": "Server=(localdb)\\MSSQLLocalDB;Database=Histology;Integrated Security=True;TrustServerCertificate=True;"
  },
  "AzureWebJobsStorage": "UseDevelopmentStorage=true"
}
```

### 4.3 Cron schedule

The NCrontab 6-field expression `0 0 4 1 1 *` fires at **04:00:00 UTC on 1 January** every year.

| Field | Value | Meaning |
|---|---|---|
| Second | `0` | At second 0 |
| Minute | `0` | At minute 0 |
| Hour | `4` | At 04:00 |
| Day of month | `1` | On the 1st |
| Month | `1` | In January |
| Day of week | `*` | Any |

UK is UTC+0 in January — the schedule matches the legacy SQL Agent job exactly.

### 4.4 CI/CD pipeline step

WebJob triggered artefacts must be published to `app_data/jobs/triggered/{job-name}/` within the Web App deployment package. Add after the main web build step:

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

The App Service WebJobs runtime picks up binaries placed under `app_data/jobs/triggered/HistologyReset/` automatically when the web package is deployed.

### 4.5 Post-deployment SQL grant for WebJob

The WebJob uses the same managed identity as the web app. If the web app SQL grant (Section 2.3) has already been executed, no additional grant is needed — the identity is the same user in the database.

If the WebJob requires a narrower permission scope (least-privilege), you can restrict it to the specific stored procedure only:

```sql
-- Least-privilege alternative to the full db_datawriter grant
GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
```

---

## 5. Bicep infrastructure changes

The following Bicep additions are required to support Phase 2. These supplement the existing `infra/` modules.

### Web App — managed identity (already required, confirm it is set)

```bicep
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  // ...
  identity: {
    type: 'SystemAssigned'
  }
  // ...
}

output managedIdentityPrincipalId string = webApp.identity.principalId
```

### Key Vault role assignment — Secrets User

The Key Vault role assignment module wires the web app's managed identity to the Key Vault so that Key Vault references in App Service application settings resolve correctly:

```bicep
// modules/keyVaultRoleAssignment.bicep
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, principalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}
```

### App Service application settings for Entra ID SAML 2.0

Add the following to the `appSettings` array in the Web App Bicep resource. Only the tenant ID is a Key Vault secret; the entity ID and return URL are plain settings:

```bicep
{
  name: 'Saml2__TenantId'
  value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/saml2-tenant-id/)'
}
{
  name: 'Saml2__EntityId'
  value: 'https://${webAppName}.azurewebsites.net/Saml2'
}
{
  name: 'Saml2__ReturnUrl'
  value: 'https://${webAppName}.azurewebsites.net/'
}
```

### SQL firewall rule

Ensure the `AllowAllWindowsAzureIps` firewall rule is present on the SQL Server to allow the App Service managed identity to reach Azure SQL:

```bicep
resource sqlFirewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}
```

---

## 6. Key Vault secrets summary

All secrets must be populated manually after the Key Vault resource is provisioned. Never store these values in any file committed to source control.

| Secret name | Description | Populated by |
|---|---|---|
| `histologydb-connection-string` | Managed identity Azure SQL connection string (no credentials) | Platform Engineering |
| `saml2-tenant-id` | Entra ID Directory (tenant) GUID — used to build federation metadata URL | Platform Engineering |
| `webjobs-storage-connection-string` | Azure Storage connection string for WebJobs dashboard | Platform Engineering |
| `appinsights-connection-string` | Application Insights connection string | Platform Engineering |

> **No client secret** — SAML 2.0 does not use a client secret. The IdP signing certificate is loaded automatically from the Entra ID federation metadata URL. No `azuread-client-id` or `azuread-client-secret` secrets are required.

**CLI population example:**

```bash
az keyvault secret set \
  --vault-name {vault-name} \
  --name histologydb-connection-string \
  --value "Server=tcp:{sql}.database.windows.net,1433;Initial Catalog=Histology;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az keyvault secret set \
  --vault-name {vault-name} \
  --name saml2-tenant-id \
  --value "{tenant-guid}"
```

---

## 7. Manual steps checklist

These steps cannot be automated by Bicep or the CI/CD pipeline. Each must be completed per environment before smoke tests are run.

### Infrastructure setup (once per environment)

- [ ] Enable system-assigned managed identity on the App Service Web App (Bicep — verify it is set)
- [ ] Provision Azure SQL Database with `minimalTlsVersion = '1.2'` (Bicep)
- [ ] Provision Key Vault with `enableRbacAuthorization: true` (Bicep)
- [ ] Deploy Key Vault Secrets User role assignment for Web App managed identity (Bicep)

### Post-Bicep manual steps (once per environment)

- [ ] Populate Key Vault secrets (see [Section 6](#6-key-vault-secrets-summary))
- [ ] Run SQL managed identity grant for Histo.Web — `CREATE USER ... FROM EXTERNAL PROVIDER` + roles (Section 2.3)
- [ ] Create Entra ID Enterprise Application per environment and configure SAML SSO (Section 3.7)
- [ ] Configure Attributes & Claims in Entra ID Enterprise Application to emit UPN, email, and group claims (Section 3.7)
- [ ] Assign users / groups to the Enterprise Application in Entra ID
- [ ] Store Entra ID `TenantId` in Key Vault as `saml2-tenant-id` (Section 3.6)
- [ ] Restart App Service after Key Vault secrets are populated (so Key Vault references resolve)

### Verification steps

- [ ] Smoke test: `GET https://{app-service-name}.azurewebsites.net/health` → HTTP 200
- [ ] Auth test: navigate to a protected page → redirected to Microsoft sign-in → sign in → returned to page
- [ ] DB test: perform a read and write operation through the UI → no SQL login failures in App Insights
- [ ] WebJob test: trigger `HistologyReset` manually from the Azure Portal WebJobs blade → status `Success`

---

## 8. Open items

| # | Item | Owner | Priority |
|---|---|---|---|
| 1 | Remove plaintext `Password=` from `appsettings.json` before Phase 2 merge | Dev | Critical |
| 2 | Create `appsettings.Development.json` with `Integrated Security=True` | Dev | High |
| 3 | Create Entra ID App Registrations per environment (dev, test, UAT, prod) | Platform Engineering | High |
| 4 | Populate Key Vault secrets per environment | Platform Engineering | High |
| 5 | Run SQL managed identity grants per environment | DBA | High |
| 6 | Add `Sustainsys.Saml2.AspNetCore2` package and activate Phase 2 SAML code in `Program.cs` | Dev | High |
| 7 | Create `src/Histo.Web/Auth/SamlClaimsTransformer.cs` and register `IClaimsTransformation` | Dev | High |
| 8 | Decide on authorization strategy — global `AuthorizeFolder("/")` vs per-page `[Authorize]` | Tech Lead | Medium |
| 9 | Confirm Entra ID group Object IDs to use for role-based authorization | Tech Lead / Platform | Medium |
| 9 | Create `Histo.WebJobs` project and add to solution | Dev | Medium |
| 10 | Configure missing-execution alert in Application Insights for WebJob | Platform Engineering | Before prod go-live |
