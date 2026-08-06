# Playwright PDF Pipeline
 
Reference for the `pdf-infrastructure` agent. Covers `IPlaywrightPdfService`
design, concurrency model, `PdfOptions`, Docker setup, and error handling.
 
---
 
## Service Interface
 
```csharp
public interface IPlaywrightPdfService
{
    /// <summary>
    /// Renders the given URL to a PDF using Playwright headless Chromium.
    /// </summary>
    /// <param name="url">Absolute URL of the Razor view to render.</param>
    /// <param name="options">Paper size, margins, and rendering options.</param>
    /// <returns>PDF bytes. Always starts with %PDF magic bytes.</returns>
    /// <exception cref="PdfRenderException">
    /// Thrown on any rendering failure. Never returns null or partial bytes.
    /// </exception>
    Task<byte[]> RenderAsync(string url, PdfRenderOptions options);
}
 
public class PdfRenderOptions
{
    public string PaperFormat { get; set; } = "A4";        // "A4" | "Letter"
    public bool Landscape { get; set; } = false;
    public float MarginTopMm { get; set; } = 15;
    public float MarginBottomMm { get; set; } = 15;
    public float MarginLeftMm { get; set; } = 15;
    public float MarginRightMm { get; set; } = 15;
    public bool PrintBackground { get; set; } = true;      // Required for CSS backgrounds
    public string? WaitForFunction { get; set; }           // e.g. "chartRendered"
    public int TimeoutMs { get; set; } = 30_000;
}
 
public class PdfRenderException : Exception
{
    public string ReportUrl { get; }
    public PdfRenderException(string url, string message, Exception? inner = null)
        : base($"PDF render failed for '{url}': {message}", inner)
        => ReportUrl = url;
}
```
 
---
 
## Implementation
 
Use the template at
`.github/Skills/pdf-html-migration/assets/PlaywrightPdfService.cs.template`
as the starting point. Key implementation requirements:
 
### Concurrency Limiting
 
PDF generation is memory-intensive. Use a `SemaphoreSlim` to cap concurrent
renders:
 
```csharp
private readonly SemaphoreSlim _semaphore;
 
public PlaywrightPdfService(IConfiguration config)
{
    var maxConcurrent = config.GetValue<int>("PdfGeneration:MaxConcurrentRenders", 3);
    _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
}
```
 
Configure in `appsettings.json`:
```json
{
  "PdfGeneration": {
    "MaxConcurrentRenders": 3,
    "TimeoutMs": 30000
  }
}
```
 
Size the semaphore based on available container memory:
- 512 MB container → max 1 concurrent render
- 1 GB container → max 2–3 concurrent renders
- 2 GB container → max 4–5 concurrent renders
 
### Browser Lifecycle
 
Use a single `IBrowser` instance per service lifetime (not per request) to
avoid cold-start overhead. Create a new `IBrowserContext` per render to ensure
isolation between concurrent requests:
 
```csharp
// In constructor / InitializeAsync
_browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
 
// In RenderAsync
await using var context = await _browser.NewContextAsync();
await using var page = await context.NewPageAsync();
```
 
### Chart Rendering Wait
 
For reports with Archetype D (chart dependency), the `WaitForFunction` option
instructs Playwright to wait until Chart.js has finished rendering before
capturing the PDF:
 
```csharp
if (options.WaitForFunction is not null)
{
    await page.WaitForFunctionAsync(options.WaitForFunction,
        new() { Timeout = options.TimeoutMs });
}
```
 
The Razor view must set `window.chartRendered = true` in Chart.js's `onComplete`
callback:
 
```javascript
new Chart(ctx, {
    // ...
    options: {
        animation: {
            onComplete: () => { window.chartRendered = true; }
        }
    }
});
```
 
### Error Handling
 
Never silently return partial or null bytes. Always throw `PdfRenderException`:
 
```csharp
var bytes = await page.PdfAsync(pdfOptions);
if (bytes is null || bytes.Length < 4 || bytes[0] != '%')
    throw new PdfRenderException(url, "Playwright returned invalid PDF bytes");
return bytes;
```
 
Wrap all Playwright calls in try/catch and rethrow as `PdfRenderException` with
the original exception as the inner exception.
 
---
 
## Registration in `Program.cs`
 
```csharp
// Install Playwright Chromium during app startup (dev only — Docker handles production)
if (app.Environment.IsDevelopment())
{
    var exitCode = Microsoft.Playwright.Program.Main(new[] { "install", "chromium" });
    if (exitCode != 0) throw new Exception("Playwright Chromium install failed");
}
 
builder.Services.AddSingleton<IPlaywrightPdfService, PlaywrightPdfService>();
```
 
Register as `Singleton` (not Scoped) because the `IBrowser` instance is
intentionally shared. The `IBrowserContext` created per-render provides the
required request isolation.
 
---
 
## Docker Setup
 
Add to the project `Dockerfile` after the .NET publish step and before
`ENTRYPOINT`:
 
```dockerfile
# Install Playwright Chromium and its Linux shared library dependencies
RUN apt-get update && apt-get install -y --no-install-recommends \
    libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0 libcups2 \
    libdrm2 libxkbcommon0 libxcomposite1 libxdamage1 libxfixes3 \
    libxrandr2 libgbm1 libasound2 libpango-1.0-0 libcairo2 \
    && rm -rf /var/lib/apt/lists/*
 
RUN dotnet tool install --global Microsoft.Playwright.CLI --version 1.44.0
ENV PATH="$PATH:/root/.dotnet/tools"
RUN playwright install chromium
```
 
The `mcr.microsoft.com/dotnet/aspnet:10.0` base image does not include these
shared libraries by default. All listed packages are required for Chromium
headless on Alpine/Debian slim images.
 
---
 
## Internal URL Construction
 
The PDF service renders Razor views by their URL within the same running
application. The URL must be an absolute URL accessible from within the container.
Two approaches:
 
**Option A — Self-call (recommended for simplicity)**
The application calls its own base URL (read from `PdfGeneration:BaseUrl` in
config or constructed from `IHttpContextAccessor`):
```csharp
var url = $"{_baseUrl}/reports/{reportName}?profileVersionId={id}";
```
 
**Option B — Named pipe / in-process rendering**
Use `RazorViewToStringRenderer` to render the Razor view to HTML in-process,
then pass the HTML string directly to `page.SetContentAsync(html)` instead of
navigating to a URL. Avoids the need for the app to call itself over HTTP.
Preferred for test environments where a live HTTP server may not be available.
 
For the smoke test, Option A is used (navigate to a real URL). For production
report generation, either option is valid — record the chosen approach in an ADR.
 
---
 
## `PdfOptions` Mapping
 
| `PdfRenderOptions` field | Playwright `PdfOptions` property |
|---|---|
| `PaperFormat` | `Format` |
| `Landscape` | `Landscape` |
| `MarginTopMm` | `Margin.Top` (convert mm → string e.g. `"15mm"`) |
| `MarginBottomMm` | `Margin.Bottom` |
| `MarginLeftMm` | `Margin.Left` |
| `MarginRightMm` | `Margin.Right` |
| `PrintBackground` | `PrintBackground` |
| `TimeoutMs` | Navigation timeout (set on `page.GotoAsync`) |
 
```csharp
var pdfOptions = new PdfOptions
{
    Format = options.PaperFormat,
    Landscape = options.Landscape,
    PrintBackground = options.PrintBackground,
    Margin = new Margin
    {
        Top = $"{options.MarginTopMm}mm",
        Bottom = $"{options.MarginBottomMm}mm",
        Left = $"{options.MarginLeftMm}mm",
        Right = $"{options.MarginRightMm}mm"
    }
};
```