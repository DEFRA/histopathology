# Skill: framework-upgrade
 
## Purpose
Apply the framework version changes determined by `upgrade-path-analysis`. Handles **two distinct upgrade paths**:
 
- **Classic → Classic**: Bump `TargetFrameworkVersion` to `v4.8` across all project files and update `web.config` runtime settings. No project format changes.
- **Classic → Modern**: Convert classic project files to SDK-style, migrate `web.config` to `appsettings.json`, migrate `Global.asax` to `Program.cs` middleware, and replace `System.Web` entry points with ASP.NET Core equivalents.
 
Read `upgrade-path-analysis-report.json` before executing — use the `pathType` field to select the correct mode.
 
---
 
## Mode A — Classic → Classic
 
### A1 — Upgrade Project File TargetFrameworkVersion
 
For every `.vbproj` / `.csproj` with `<TargetFrameworkVersion>` below `v4.8`:
 
```xml
<!-- Before -->
<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
 
<!-- After -->
<TargetFrameworkVersion>v4.8</TargetFrameworkVersion>
```
 
> Do **not** replace `<TargetFrameworkVersion>` with the SDK-style `<TargetFramework>net48</TargetFramework>` element — these are classic `packages.config` projects and the SDK-style element does not apply.
 
### A2 — Upgrade httpRuntime in web.config
 
Apply to **every** `web.config` in the solution, including WCF service hosts and ASMX sub-applications:
 
```xml
<!-- Before -->
<httpRuntime targetFramework="4.0" />
 
<!-- After -->
<httpRuntime targetFramework="4.8" requestValidationMode="2.0" />
```
 
- If `requestValidationMode` is already present and set to `4.0`, change it to `2.0`.
- If it is set to `2.0` already, leave it unchanged.
- `requestValidationMode="2.0"` is required for legacy WebForms pages that post HTML-encoded input (rich text editors, user-entered fields with angle brackets).
 
### A3 — Update System.* Assembly References
 
Check each project's `<Reference>` elements. For GAC-resolved .NET Framework assemblies, remove explicit `Version=` attributes from references that have no `<HintPath>` — the runtime bind redirects will resolve the correct version:
 
| Assembly | Resolution |
|---|---|
| `System.Web` | GAC-resolved — no change needed |
| `System.Web.Extensions` | Remove version pin if `<HintPath>` is absent |
| `System.Data` | GAC-resolved — no change needed |
| `System.Xml` | GAC-resolved — no change needed |
 
### A4 — Update Assembly Binding Redirects
 
Ensure the `<assemblyBinding>` section in `web.config` is current. The `nuget-package-upgrade` skill manages package-specific redirects; this step ensures framework-level redirects are present:
 
```xml
<runtime>
  <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <!-- Example: redirect all older versions to the version installed by the NuGet upgrade -->
    <dependentAssembly>
      <assemblyIdentity name="System.Runtime.CompilerServices.Unsafe"
                        publicKeyToken="b03f5f7f11d50a3a" culture="neutral" />
      <bindingRedirect oldVersion="0.0.0.0-6.0.0.0" newVersion="6.0.0.0" />
    </dependentAssembly>
  </assemblyBinding>
</runtime>
```
 
### A5 — Validate Solution File Integrity
 
Confirm:
- Solution root contains exactly one `*.sln` file.
- All project GUIDs in the `.sln` have a matching project file on disk.
- No project references a `.dll` compiled for a framework version higher than `v4.8`.
 
Report any mismatch as a **blocking issue**.
 
---
 
## Mode B — Classic → Modern
 
### B1 — Convert Project File to SDK-Style
 
Replace the entire content of each classic `.csproj` or `.vbproj` with an SDK-style project file. Use the appropriate SDK:
 
| Project Type | SDK |
|---|---|
| ASP.NET Core web app | `Microsoft.NET.Sdk.Web` |
| Class library | `Microsoft.NET.Sdk` |
| Console app / Windows Service | `Microsoft.NET.Sdk` |
| Worker Service | `Microsoft.NET.Sdk.Worker` |
 
**Before (classic format excerpt):**
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="15.0" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <Import Project="$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props" />
  <PropertyGroup>
    <TargetFrameworkVersion>v4.6.2</TargetFrameworkVersion>
    <!-- ... many property groups ... -->
  </PropertyGroup>
  <!-- hundreds of <Compile Include="..."> and <Content Include="..."> items -->
</Project>
```
 
**After (SDK-style):**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>MyApplication</RootNamespace>
    <AssemblyName>MyApplication</AssemblyName>
  </PropertyGroup>
  <!-- PackageReference items added by nuget-package-upgrade skill -->
</Project>
```
 
> In SDK-style projects, source files, Razor views, and static content are included by convention — explicit `<Compile>`, `<Content>`, and `<None>` items are not needed and must not be copied.
 
### B2 — Migrate web.config to appsettings.json
 
Move application configuration from `web.config` `<appSettings>` to `appsettings.json`:
 
**web.config (source):**
```xml
<appSettings>
  <add key="DatabaseName"     value="SampleDb" />
  <add key="MaxRetryCount"    value="3" />
  <add key="FeatureXEnabled"  value="true" />
</appSettings>
```
 
**appsettings.json (target):**
```json
{
  "AppSettings": {
    "DatabaseName": "SampleDb",
    "MaxRetryCount": 3,
    "FeatureXEnabled": true
  },
  "ConnectionStrings": {},
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```
 
- **Secret values** (passwords, keys, connection strings): do **not** migrate to `appsettings.json` — leave a placeholder comment and note they must be provided via Key Vault / environment variables.
- Preserve `web.config` as a minimal IIS reverse-proxy config (see B4).
 
### B3 — Migrate Global.asax to Program.cs
 
**Global.asax patterns and their ASP.NET Core equivalents:**
 
| Legacy Pattern | ASP.NET Core Equivalent |
|---|---|
| `Application_Start` | `WebApplication.CreateBuilder()` + `builder.Services.*` registrations |
| `Application_End` | `IHostApplicationLifetime.ApplicationStopped` callback |
| `Application_Error` | Global exception handler middleware (`UseExceptionHandler`) |
| `Application_BeginRequest` | Custom `IMiddleware` or `app.Use(...)` |
| `Session_Start` / `Session_End` | Not available — use distributed session events if needed |
| `RouteConfig.RegisterRoutes` | `app.MapControllerRoute(...)` or `app.MapRazorPages()` |
 
**Skeleton `Program.cs` for a migrated web application:**
```csharp
// Program.cs — generated by framework-upgrade skill
// Replace placeholder service registrations with actual application dependencies.
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
 
var builder = WebApplication.CreateBuilder(args);
 
// TODO: Register application services here (was Application_Start in Global.asax)
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
 
var app = builder.Build();
 
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error/GeneralError");
    app.UseHsts();
}
 
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
 
app.MapRazorPages();
app.MapDefaultControllerRoute();
 
app.Run();
```
 
### B4 — Retain Minimal web.config for IIS Deployment
 
Keep a minimal `web.config` at the project root for IIS hosting (ASP.NET Core In-Process hosting module):
 
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*"
           modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet"
                arguments=".\MyApplication.dll"
                stdoutLogEnabled="false"
                stdoutLogFile=".\logs\stdout"
                hostingModel="inprocess" />
  </system.webServer>
</configuration>
```
 
### B5 — Flag WebForms Pages for Manual Migration
 
WebForms (`.aspx`, `.ascx`, `.master`) have **no automated migration path** to ASP.NET Core. For each file:
1. Log it in `framework-upgrade-report.json` as **requires-manual-migration**.
2. Create a stub Razor Page (`.cshtml` + `.cshtml.cs`) with the same route that returns `501 Not Implemented`, so the build succeeds.
3. Add a `// TODO: Migrate from WebForms` comment at the top of every stub.
 
Stubs are placeholders only — page logic must be manually ported to Razor Pages, MVC, or Blazor by the development team.
 
---
 
## Outputs
 
| Output | Description |
|---|---|
| `framework-upgrade-report.json` | JSON: files changed, old version, new version, path type, WebForms stubs created, blocking issues |
| Modified project files | `.vbproj` / `.csproj` updated in place |
| Modified `web.config` files | `httpRuntime` / binding redirects updated (Mode A), or minimised to IIS proxy config (Mode B) |
| `appsettings.json` | Created (Mode B only) |
| `Program.cs` | Created or updated (Mode B only) |
| WebForms stub Razor Pages | Created (Mode B only, one per `.aspx`) |
 
---
 
## Constraints
 
- **Mode A**: Do **not** convert classic project files to SDK-style — that is a Mode B action.
- **Mode B**: Do **not** retain `System.Web` references in the migrated project file — they are incompatible with .NET 5+.
- Do **not** modify `.designer.vb`, `.designer.cs`, or auto-generated code files.
- Do **not** migrate secret values to `appsettings.json` under any circumstances.
- WebForms pages must **never** be silently deleted — stubs must be created to preserve route coverage.