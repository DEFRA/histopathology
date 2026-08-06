# Test Generation Patterns
 
Reference document for `e2e-baseline-test-generator`. Loaded by that agent only.
 
## Purpose
 
Define the exact C# code patterns, NUnit conventions, naming rules, and
Playwright locator strategies for generating the three output files per page
and the project scaffold. All patterns are C# only — never VB.
 
---
 
## Project Scaffold Patterns
 
### `E2EProject.csproj`
 
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Playwright.NUnit" Version="1.44.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="NUnit" Version="4.1.0" />
    <PackageReference Include="NUnit3TestAdapter" Version="4.5.0" />
  </ItemGroup>
</Project>
```
 
No `<ProjectReference>` elements. Ever.
 
### `GlobalUsings.cs`
 
```csharp
global using Microsoft.Playwright;
global using Microsoft.Playwright.NUnit;
global using NUnit.Framework;
global using {{Namespace}}.Infrastructure;
global using {{Namespace}}.Config;
global using {{Namespace}}.Selectors;
global using {{Namespace}}.Pages;
```
 
### `Config/TestSettings.cs`
 
```csharp
namespace {{Namespace}}.Config;
 
public static class TestSettings
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("E2E_BASE_URL")
        ?? throw new InvalidOperationException(
            "E2E_BASE_URL environment variable must be set before running tests. " +
            "Example: $env:E2E_BASE_URL = 'http://localhost:5000'");
}
```
 
### `Config/Routes.cs`
 
```csharp
namespace {{Namespace}}.Config;
 
/// <summary>
/// URL route constants derived from the pre-migration web project.
/// Update these values post-migration to point at Razor Page routes.
/// </summary>
public static class Routes
{
    // Generated from page-inventory.json
    // One constant per in-scope page, named after the page (PascalCase, no .aspx extension)
    public static string ManageProfile => $"{TestSettings.BaseUrl}/ManageProfile.aspx";
    public static string Search => $"{TestSettings.BaseUrl}/Search.aspx";
    // ... one per page
}
```
 
---
 
## Infrastructure Class Patterns
 
### `Infrastructure/BasePageTest.cs`
 
```csharp
namespace {{Namespace}}.Infrastructure;
 
/// <summary>
/// Base class for all E2E page tests.
/// Configures Playwright browser options and provides common navigation helpers.
/// </summary>
public class BasePageTest : PageTest
{
    public override BrowserNewContextOptions ContextOptions() =>
        new()
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 }
        };
}
```
 
### `Infrastructure/SemanticLocatorExtensions.cs`
 
```csharp
namespace {{Namespace}}.Infrastructure;
 
/// <summary>
/// Value types representing selector descriptors in the registry.
/// Converted to Playwright ILocator instances by the ToLocator extension method.
/// </summary>
public readonly record struct SemanticSelector(string Value)
{
    public ILocator ToLocator(IPage page) => page.GetByLabel(Value);
}
 
public readonly record struct RoleSelector(AriaRole Role, string? Name)
{
    public bool ClientConfirm { get; init; }
    public ILocator ToLocator(IPage page) =>
        Name is null
            ? page.GetByRole(Role)
            : page.GetByRole(Role, new() { Name = Name });
}
 
public readonly record struct TextSelector(string Value)
{
    public ILocator ToLocator(IPage page) => page.GetByText(Value, new() { Exact = true });
}
 
public readonly record struct TelerikSelector(
    string ControlType,
    string GeneratedIdPattern,
    string? AssociatedLabel)
{
    // Resolved by TelerikHelper, not directly via Playwright locator
}
```
 
### `Infrastructure/TelerikHelper.cs`
 
```csharp
namespace {{Namespace}}.Infrastructure;
 
/// <summary>
/// Wraps Telerik Web Forms control interactions.
/// Pre-migration: uses generated CSS ID patterns (fragile, Telerik-specific).
/// Post-migration: replace method bodies with semantic getByLabel/getByRole calls.
/// </summary>
public static class TelerikHelper
{
    /// <summary>
    /// Types a date value into a RadDatePicker input.
    /// Post-migration: replace body with page.GetByLabel(label).FillAsync(date.ToString("yyyy-MM-dd"))
    /// </summary>
    public static async Task FillDatePicker(IPage page, TelerikSelector selector, DateTime date)
    {
        var input = page.Locator(selector.GeneratedIdPattern);
        await input.FillAsync(date.ToString("dd/MM/yyyy"));
        await input.PressAsync("Tab"); // commit the value
    }
 
    /// <summary>
    /// Selects a value from a RadComboBox.
    /// Post-migration: replace body with page.GetByLabel(label).SelectOptionAsync(value)
    /// </summary>
    public static async Task SelectComboBox(IPage page, TelerikSelector selector, string value)
    {
        var input = page.Locator(selector.GeneratedIdPattern);
        await input.FillAsync(value);
        await page.Locator($".rcbList li:has-text('{value}')").ClickAsync();
    }
 
    /// <summary>
    /// Checks a node in a RadTreeView by its visible text.
    /// Post-migration: replace body with page.GetByRole(AriaRole.Treeitem, new(){Name=nodeText}).ClickAsync()
    /// </summary>
    public static async Task CheckTreeNode(IPage page, TelerikSelector selector, string nodeText)
    {
        var tree = page.Locator(selector.GeneratedIdPattern);
        var node = tree.Locator($".rtText:has-text('{nodeText}')");
        var checkbox = node.Locator("xpath=preceding-sibling::input[@type='checkbox']");
        await checkbox.CheckAsync();
    }
 
    /// <summary>
    /// Gets the text of a specific cell in a RadGrid row.
    /// </summary>
    public static async Task<string> GetGridCellText(
        IPage page, TelerikSelector selector, int rowIndex, int columnIndex)
    {
        var grid = page.Locator(selector.GeneratedIdPattern);
        var cell = grid.Locator($"tr.rgRow:nth-child({rowIndex + 1}) td:nth-child({columnIndex + 1})");
        return await cell.InnerTextAsync();
    }
}
```
 
---
 
## Selectors Class Pattern
 
```csharp
namespace {{Namespace}}.Selectors;
 
/// <summary>
/// Selector registry for {{PageName}}.
/// Pre-migration values: derived from the .aspx markup label text and button text.
/// Post-migration: update values here if the Razor Page uses different label text,
/// then re-run e2e-baseline-test-generator for this page.
/// </summary>
public static class {{PageName}}Selectors
{
    public static class Fields
    {
        public static readonly SemanticSelector FirstName = new("First Name");
        public static readonly TelerikSelector DateOfBirth =
            new("RadDatePicker", "[id$='_dpDOB_dateInput']", "Date of Birth");
    }
 
    public static class Actions
    {
        public static readonly RoleSelector Save = new(AriaRole.Button, "Save");
        public static readonly RoleSelector Delete =
            new(AriaRole.Button, "Delete") { ClientConfirm = true };
        public static readonly RoleSelector Cancel = new(AriaRole.Link, "Cancel");
    }
 
    public static class Feedback
    {
        public static readonly TextSelector Success = new("Record saved successfully");
        public static readonly RoleSelector ErrorSummary = new(AriaRole.Alert, null);
        public static readonly TextSelector FirstNameError = new("First Name is required");
    }
 
    public static class Navigation
    {
        public static readonly RoleSelector PageHeading =
            new(AriaRole.Heading, "Edit Profile Title");
    }
}
```
 
---
 
## Page Object Pattern
 
```csharp
namespace {{Namespace}}.Pages;
 
/// <summary>
/// Page object for {{PageName}}.
/// Encapsulates all locator resolution and interaction methods for this page.
/// Never use selectors directly in test specs — always go through this class.
/// </summary>
public class {{PageName}}Page(IPage page)
{
    // --- Locators (resolved from selector registry) ---
 
    public ILocator FirstNameField =>
        {{PageName}}Selectors.Fields.FirstName.ToLocator(page);
 
    public ILocator SaveButton =>
        {{PageName}}Selectors.Actions.Save.ToLocator(page);
 
    public ILocator DeleteButton =>
        {{PageName}}Selectors.Actions.Delete.ToLocator(page);
 
    public ILocator SuccessMessage =>
        {{PageName}}Selectors.Feedback.Success.ToLocator(page);
 
    public ILocator ErrorSummary =>
        {{PageName}}Selectors.Feedback.ErrorSummary.ToLocator(page);
 
    public ILocator FirstNameError =>
        {{PageName}}Selectors.Feedback.FirstNameError.ToLocator(page);
 
    // --- Scenario Action Methods ---
 
    public async Task FillAndSave(string firstName /*, other required params */)
    {
        await FirstNameField.FillAsync(firstName);
        await SaveButton.ClickAsync();
    }
 
    /// <summary>
    /// Clicks Delete and confirms the confirmation dialog.
    /// Pre-migration: uses Bootstrap modal triggered by app.confirmPrompt().
    /// Post-migration: update if confirmation pattern changes.
    /// </summary>
    public async Task DeleteWithConfirmation()
    {
        await DeleteButton.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "OK" }).ClickAsync();
    }
 
    public async Task DeleteAndCancel()
    {
        await DeleteButton.ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();
    }
 
    /// <summary>
    /// Fills a RadDatePicker field via TelerikHelper.
    /// Post-migration: replace TelerikHelper call with:
    ///   await page.GetByLabel("Date of Birth").FillAsync(date.ToString("yyyy-MM-dd"));
    /// </summary>
    public async Task FillDateOfBirth(DateTime date) =>
        await TelerikHelper.FillDatePicker(
            page, {{PageName}}Selectors.Fields.DateOfBirth, date);
 
    /// <summary>
    /// Fills the TinyMCE rich-text editor (renders in an iframe).
    /// </summary>
    public async Task FillRichTextField(string text)
    {
        // TinyMCE renders in an iframe — use FrameLocator
        var editor = page.FrameLocator("iframe.tox-edit-area__iframe");
        await editor.Locator("body").FillAsync(text);
    }
}
```
 
---
 
## Test Spec Pattern
 
```csharp
namespace {{Namespace}}.Specs;
 
[TestFixture]
public class {{PageName}}Tests : BasePageTest
{
    [Test]
    public async Task Save_valid_record()
    {
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        await p.FillAndSave("John Smith");
 
        await Expect(p.SuccessMessage).ToBeVisibleAsync();
    }
 
    [Test]
    public async Task Submit_without_FirstName_shows_error()
    {
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        // Leave First Name empty, click Save
        await p.SaveButton.ClickAsync();
 
        await Expect(p.FirstNameError).ToBeVisibleAsync();
    }
 
    [Test]
    public async Task Delete_with_confirmation_removes_record()
    {
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        await p.DeleteWithConfirmation();
 
        // Assert the record is gone — use a specific feedback locator
        await Expect(p.SuccessMessage).ToBeVisibleAsync();
    }
 
    [Test]
    public async Task Cancel_delete_leaves_page_unchanged()
    {
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        await p.DeleteAndCancel();
 
        // Assert no success message — page is unchanged
        await Expect(p.SuccessMessage).Not.ToBeVisibleAsync();
    }
 
    [Test]
    [Ignore("Requires test data: a user account with the Reviewer role")]
    [Category("role-gated")]
    public async Task Reviewer_cannot_see_Delete_button()
    {
        // requiresRole: Reviewer
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        await Expect(p.DeleteButton).Not.ToBeVisibleAsync();
    }
 
    [Test]
    [Ignore("Requires test data: empty dataset with no records")]
    public async Task Empty_state_message_shown_when_no_records()
    {
        var p = new {{PageName}}Page(Page);
        await Page.GotoAsync(Routes.{{PageName}});
 
        await Expect(page.GetByText("No records found")).ToBeVisibleAsync();
    }
}
```
 
---
 
## UpdatePanel / Partial Render Pattern
 
For scenarios with `clientSideBehaviour: true` and `UpdatePanel` context:
 
```csharp
[Test]
public async Task Search_results_update_without_page_reload()
{
    var p = new SearchPage(Page);
    await Page.GotoAsync(Routes.Search);
 
    // Capture the URL before triggering the partial postback
    var urlBefore = Page.Url;
 
    await p.SearchByKeyword("malaria");
 
    // Results updated — URL unchanged (no full navigation)
    Assert.That(Page.Url, Is.EqualTo(urlBefore));
    await Expect(p.ResultsArea).ToBeVisibleAsync();
}
```
 
Do not use `WaitForResponseAsync` for UpdatePanel postbacks — `__doPostBack`
does not trigger a network response detectable by Playwright's response
interception. Instead, wait for element visibility.
 
---
 
## Method Naming Rules
 
Page object methods must be named after the **user action or outcome**,
not the underlying control:
 
| Correct | Incorrect |
|---|---|
| `FillAndSave(...)` | `ClickBtnSave()` |
| `DeleteWithConfirmation()` | `ClickLnkDelete()` |
| `SearchByKeyword(string)` | `SetTxtSearchAndClickBtnSearch(string)` |
| `SelectDateOfBirth(DateTime)` | `FillDpDob(DateTime)` |
| `NavigateToManageProfile()` | `ClickLnkManageProfile()` |
 
---
 
## Test Method Naming Rules
 
Test method names must read as a description of the scenario:
 
Format: `{Verb}_{context}[_{qualifier}]`
 
Examples:
- `Save_valid_record`
- `Submit_without_email_shows_validation_error`
- `Delete_with_confirmation_removes_row`
- `Cancel_delete_leaves_page_unchanged`
- `Reviewer_can_view_but_not_edit_profile`
- `Search_results_update_without_page_reload`
 
Use underscores as word separators (NUnit displays them as spaces in the
test results UI).
 