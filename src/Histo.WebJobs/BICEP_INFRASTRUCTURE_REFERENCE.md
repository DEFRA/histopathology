// Bicep configuration reference for WebJob infrastructure setup
// File: infra/modules/webJobInfrastructure.bicep
// Purpose: Key Vault role assignments and SQL firewall rules for WebJobs

// Key Vault Secrets User role (allows App Service to read secrets)
// Role ID: 4633458b-17de-408a-b874-0445c86b69e6
@description('Role ID for Key Vault Secrets User')
param keyVaultSecretsUserRoleId string = '4633458b-17de-408a-b874-0445c86b69e6'

@description('Key Vault resource')
param keyVault object

@description('App Service managed identity principal ID')
param managedIdentityPrincipalId string

@description('SQL Server resource')
param sqlServer object

// ================================================================================
// Key Vault Role Assignment — Allows App Service to read secrets
// ================================================================================
// This role assignment enables the WebJob and web app to resolve Key Vault 
// references in appsettings.json (e.g., @Microsoft.KeyVault(...))
//
// Example config in app settings:
// - Saml2__TenantId: @Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/saml2-tenant-id/)
// - ConnectionStrings__HistologyDb: @Microsoft.KeyVault(SecretUri=https://{vault}.vault.azure.net/secrets/histologydb-connection-string/)
// ================================================================================

resource keyVaultRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, managedIdentityPrincipalId, keyVaultSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleId
    )
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// ================================================================================
// SQL Firewall Rule — Allow Azure Services
// ================================================================================
// This rule allows the App Service (and WebJob within it) to reach Azure SQL
// via the Azure service endpoint.
//
// Important: After deploying this rule, execute SQL grants manually:
//
//   CREATE USER [{app-service-name}] FROM EXTERNAL PROVIDER;
//   ALTER ROLE db_datareader ADD MEMBER [{app-service-name}];
//   ALTER ROLE db_datawriter ADD MEMBER [{app-service-name}];
//   GRANT EXECUTE ON OBJECT::dbo.EditResetHistologyRef TO [{app-service-name}];
// ================================================================================

resource sqlFirewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// ================================================================================
// Key Vault Secrets — Required values (populated manually)
// ================================================================================
// These secrets must be created in Key Vault before the WebJob can run.
// Use this checklist for platform engineering:
//
// 1. histologydb-connection-string
//    Value format:
//    Server=tcp:{sql-server-name}.database.windows.net,1433;
//    Initial Catalog=Histology;
//    Authentication=Active Directory Default;
//    Encrypt=True;
//    TrustServerCertificate=False;
//    Connection Timeout=30;
//
// 2. webjobs-storage-connection-string
//    Value: Azure Storage connection string (DefaultEndpointsProtocol=https;...)
//
// 3. saml2-tenant-id (for Entra ID authentication)
//    Value: {tenant-guid}
//
// 4. appinsights-connection-string (optional)
//    Value: Application Insights connection string
//
// Example PowerShell commands:
// 
//   az keyvault secret set \
//     --vault-name {vault-name} \
//     --name histologydb-connection-string \
//     --value "Server=tcp:{sql}.database.windows.net,1433;Initial Catalog=Histology;Authentication=Active Directory Default;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
//
//   az keyvault secret set \
//     --vault-name {vault-name} \
//     --name webjobs-storage-connection-string \
//     --value "{storage-connection-string}"
//
// ================================================================================

output keyVaultRoleAssignmentId string = keyVaultRoleAssignment.id
output sqlFirewallRuleId string = sqlFirewallAllowAzureServices.id

// ================================================================================
// App Service Configuration — Wire Key Vault references
// ================================================================================
// Add these application settings to the Web App resource in your main Bicep file.
// WebJobs run inside the same App Service process/sandbox as Histo.Web and inherit
// these settings as environment variables automatically — do NOT duplicate them in
// Histo.WebJobs/appsettings.json (production). This is the single source of truth
// for both projects.
//
// appSettings: [
//   {
//     name: 'Saml2__TenantId'
//     value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/saml2-tenant-id/)'
//   }
//   {
//     name: 'ConnectionStrings__HistologyDb'
//     value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/histologydb-connection-string/)'
//   }
//   {
//     name: 'AzureWebJobsStorage'
//     value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/webjobs-storage-connection-string/)'
//   }
//   {
//     name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
//     value: '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/appinsights-connection-string/)'
//   }
// ]
//
// Note: Double underscore '__' is converted to ':' in IConfiguration hierarchy
// So 'Saml2__TenantId' maps to appsettings key 'Saml2:TenantId'
//
// Histo.WebJobs/appsettings.json (production) only needs to contain non-secret,
// job-specific settings such as Logging levels. Histo.WebJobs/appsettings.Development.json
// keeps its own LocalDB/UseDevelopmentStorage values since local dev has no App Service
// environment variables to inherit from.
//
// ================================================================================

// ================================================================================
// CI/CD Pipeline — WebJob Packaging
// ================================================================================
// Add this step to your build/deploy pipeline (e.g., Azure Pipelines YAML):
//
// - name: Publish WebJob
//   run: |
//     msbuild "src/Histo.WebJobs/Histo.WebJobs.csproj" `
//       /p:Configuration=Release `
//       /p:DeployOnBuild=true `
//       /p:WebPublishMethod=FileSystem `
//       /p:PublishUrl="${{ env.ARTIFACT_DIR }}/app_data/jobs/triggered/HistologyReset" `
//       /nologo
//
// The WebJobs runtime will pick up binaries in:
// - app_data/jobs/triggered/HistologyReset/ (for triggered jobs)
// - app_data/jobs/continuous/ (for continuous jobs)
//
// Deployment package structure:
// ```
// app_data/
// ├── jobs/
// │   └── triggered/
// │       └── HistologyReset/
// │           ├── Histo.WebJobs.exe
// │           ├── appsettings.json
// │           ├── Microsoft.Azure.WebJobs.dll
// │           └── ...
// ```
//
// ================================================================================
